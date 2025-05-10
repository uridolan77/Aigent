using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Monitoring;

namespace Aigent.Orchestration
{
    /// <summary>
    /// Basic orchestrator implementation
    /// </summary>
    public class BasicOrchestrator : IOrchestrator
    {
        private readonly IAgentRegistry _agentRegistry;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the BasicOrchestrator class
        /// </summary>
        /// <param name="agentRegistry">Agent registry</param>
        /// <param name="logger">Logger</param>
        public BasicOrchestrator(IAgentRegistry agentRegistry, ILogger logger)
        {
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Executes a workflow
        /// </summary>
        /// <param name="workflow">Workflow to execute</param>
        /// <returns>Result of the workflow execution</returns>
        public async Task<WorkflowResult> ExecuteWorkflow(WorkflowDefinition workflow)
        {
            if (workflow == null)
            {
                throw new ArgumentNullException(nameof(workflow));
            }
            
            _logger.Log(LogLevel.Information, $"Executing workflow: {workflow.Name}");
            
            var results = new Dictionary<string, ActionResult>();
            
            try
            {
                switch (workflow.Type)
                {
                    case WorkflowType.Sequential:
                        await ExecuteSequentialWorkflow(workflow, results);
                        break;
                    case WorkflowType.Parallel:
                        await ExecuteParallelWorkflow(workflow, results);
                        break;
                    case WorkflowType.Conditional:
                        await ExecuteConditionalWorkflow(workflow, results);
                        break;
                    default:
                        throw new ArgumentException($"Unknown workflow type: {workflow.Type}");
                }
                
                var success = results.Values.All(r => r.Success);
                var message = success ? "Workflow executed successfully" : "Workflow execution failed";
                
                _logger.Log(success ? LogLevel.Information : LogLevel.Warning, $"{message}: {workflow.Name}");
                
                return success
                    ? WorkflowResult.Successful(message, results)
                    : WorkflowResult.Failed(message, results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing workflow {workflow.Name}: {ex.Message}", ex);
                return WorkflowResult.Failed($"Error executing workflow: {ex.Message}", results);
            }
        }
        
        private async Task ExecuteSequentialWorkflow(WorkflowDefinition workflow, Dictionary<string, ActionResult> results)
        {
            foreach (var step in workflow.Steps)
            {
                var stepResult = await ExecuteStep(step, results);
                results[step.Name] = stepResult;
                
                if (!stepResult.Success)
                {
                    _logger.Log(LogLevel.Warning, $"Sequential workflow step failed: {step.Name}");
                    break;
                }
            }
        }
        
        private async Task ExecuteParallelWorkflow(WorkflowDefinition workflow, Dictionary<string, ActionResult> results)
        {
            var tasks = new List<Task>();
            
            foreach (var step in workflow.Steps)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var stepResult = await ExecuteStep(step, results);
                    lock (results)
                    {
                        results[step.Name] = stepResult;
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
        }
        
        private async Task ExecuteConditionalWorkflow(WorkflowDefinition workflow, Dictionary<string, ActionResult> results)
        {
            foreach (var step in workflow.Steps)
            {
                if (ShouldExecuteStep(step, results))
                {
                    var stepResult = await ExecuteStep(step, results);
                    results[step.Name] = stepResult;
                }
                else
                {
                    _logger.Log(LogLevel.Debug, $"Skipping conditional workflow step: {step.Name}");
                }
            }
        }
        
        private async Task<ActionResult> ExecuteStep(WorkflowStep step, Dictionary<string, ActionResult> results)
        {
            _logger.Log(LogLevel.Debug, $"Executing workflow step: {step.Name}");
            
            // Check dependencies
            foreach (var dependency in step.Dependencies)
            {
                if (!results.TryGetValue(dependency, out var dependencyResult) || !dependencyResult.Success)
                {
                    _logger.Log(LogLevel.Warning, $"Dependency not satisfied: {dependency} for step: {step.Name}");
                    return ActionResult.Failed($"Dependency not satisfied: {dependency}");
                }
            }
            
            // Get an agent of the required type
            var agents = await _agentRegistry.GetAgents(type: step.RequiredAgentType.ToString());
            
            if (agents.Count == 0)
            {
                _logger.Log(LogLevel.Warning, $"No agent of type {step.RequiredAgentType} available for step: {step.Name}");
                return ActionResult.Failed($"No agent of type {step.RequiredAgentType} available");
            }
            
            var agent = agents[0];
            
            // Create environment state
            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>(step.Parameters)
            };
            
            // Execute the action
            var action = await agent.DecideAction(state);
            var result = await action.Execute();
            
            // Learn from the result
            await agent.Learn(state, action, result);
            
            _logger.Log(result.Success ? LogLevel.Debug : LogLevel.Warning, 
                $"Workflow step {step.Name} {(result.Success ? "succeeded" : "failed")}: {result.Message}");
            
            return result;
        }
        
        private bool ShouldExecuteStep(WorkflowStep step, Dictionary<string, ActionResult> results)
        {
            // If no condition, always execute
            if (string.IsNullOrEmpty(step.Condition))
            {
                return true;
            }
            
            // Check dependencies first
            foreach (var dependency in step.Dependencies)
            {
                if (!results.ContainsKey(dependency))
                {
                    _logger.Log(LogLevel.Warning, $"Dependency not found: {dependency} for step: {step.Name}");
                    return false;
                }
            }
            
            // Parse and evaluate the condition (simplified)
            try
            {
                if (step.Condition.Contains("=="))
                {
                    var parts = step.Condition.Split("==", StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        var leftPart = parts[0];
                        var rightPart = parts[1];
                        
                        if (leftPart.Contains("."))
                        {
                            var subParts = leftPart.Split(".", StringSplitOptions.TrimEntries);
                            if (subParts.Length == 2 && results.TryGetValue(subParts[0], out var result))
                            {
                                if (subParts[1] == "Success")
                                {
                                    return result.Success == bool.Parse(rightPart);
                                }
                            }
                        }
                    }
                }
                
                _logger.Log(LogLevel.Warning, $"Could not evaluate condition: {step.Condition} for step: {step.Name}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error evaluating condition {step.Condition} for step {step.Name}: {ex.Message}", ex);
                return false;
            }
        }
    }
}
