using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Communication;
using Aigent.Monitoring;
using Aigent.Safety;

namespace Aigent.Orchestration
{
    /// <summary>
    /// Enhanced implementation of IOrchestrator with complete workflow support
    /// </summary>
    public class EnhancedOrchestrator : IOrchestrator
    {
        private readonly Dictionary<string, IAgent> _agents = new();
        private readonly ILogger _logger;
        private readonly ISafetyValidator _safetyValidator;
        private readonly IMessageBus _messageBus;
        private readonly IMetricsCollector _metrics;

        /// <summary>
        /// Initializes a new instance of the EnhancedOrchestrator class
        /// </summary>
        /// <param name="logger">Logger for recording orchestration activities</param>
        /// <param name="safetyValidator">Safety validator for ensuring actions are safe</param>
        /// <param name="messageBus">Message bus for inter-agent communication</param>
        /// <param name="metrics">Metrics collector for monitoring orchestration performance</param>
        public EnhancedOrchestrator(
            ILogger logger, 
            ISafetyValidator safetyValidator,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyValidator = safetyValidator ?? throw new ArgumentNullException(nameof(safetyValidator));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _metrics = metrics;
        }

        /// <summary>
        /// Registers an agent with the orchestrator
        /// </summary>
        /// <param name="agent">The agent to register</param>
        public Task RegisterAgent(IAgent agent)
        {
            _metrics?.StartOperation("orchestrator_register_agent");
            
            try
            {
                _agents[agent.Id] = agent;
                _logger.Log(LogLevel.Information, $"Registered agent: {agent.Name} ({agent.Id})");
                _metrics?.RecordMetric("orchestrator.registered_agents_count", _agents.Count);
                
                return Task.CompletedTask;
            }
            finally
            {
                _metrics?.EndOperation("orchestrator_register_agent");
            }
        }

        /// <summary>
        /// Unregisters an agent from the orchestrator
        /// </summary>
        /// <param name="agentId">ID of the agent to unregister</param>
        public Task UnregisterAgent(string agentId)
        {
            _metrics?.StartOperation("orchestrator_unregister_agent");
            
            try
            {
                if (_agents.Remove(agentId))
                {
                    _logger.Log(LogLevel.Information, $"Unregistered agent: {agentId}");
                    _metrics?.RecordMetric("orchestrator.registered_agents_count", _agents.Count);
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Attempted to unregister unknown agent: {agentId}");
                }
                
                return Task.CompletedTask;
            }
            finally
            {
                _metrics?.EndOperation("orchestrator_unregister_agent");
            }
        }

        /// <summary>
        /// Assigns a task to the most suitable agent
        /// </summary>
        /// <param name="task">Description of the task</param>
        /// <returns>The selected agent</returns>
        public async Task<IAgent> AssignTask(string task)
        {
            _metrics?.StartOperation("orchestrator_assign_task");
            
            try
            {
                var bestAgent = await SelectBestAgent(task, _agents.Values.ToList());
                _logger.Log(LogLevel.Information, $"Assigned task '{task}' to agent '{bestAgent.Name}'");
                _metrics?.RecordMetric("orchestrator.task_assignments_count", 1.0);
                
                return bestAgent;
            }
            finally
            {
                _metrics?.EndOperation("orchestrator_assign_task");
            }
        }

        /// <summary>
        /// Executes a workflow involving multiple agents
        /// </summary>
        /// <param name="workflow">Definition of the workflow</param>
        /// <returns>Result of the workflow execution</returns>
        public async Task<WorkflowResult> ExecuteWorkflow(WorkflowDefinition workflow)
        {
            _metrics?.StartOperation($"orchestrator_execute_workflow_{workflow.Name}");
            
            var result = new WorkflowResult();

            try
            {
                _logger.Log(LogLevel.Information, $"Starting workflow: {workflow.Name}");
                
                switch (workflow.Type)
                {
                    case WorkflowType.Sequential:
                        result = await ExecuteSequentialWorkflow(workflow);
                        break;
                    case WorkflowType.Parallel:
                        result = await ExecuteParallelWorkflow(workflow);
                        break;
                    case WorkflowType.Conditional:
                        result = await ExecuteConditionalWorkflow(workflow);
                        break;
                    case WorkflowType.Hierarchical:
                        result = await ExecuteHierarchicalWorkflow(workflow);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported workflow type: {workflow.Type}");
                }
                
                _logger.Log(LogLevel.Information, $"Completed workflow: {workflow.Name}, Success: {result.Success}");
                _metrics?.RecordMetric("orchestrator.workflow_success", result.Success ? 1.0 : 0.0);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Workflow execution error: {ex.Message}");
                _logger.LogError($"Workflow '{workflow.Name}' failed", ex);
                _metrics?.RecordMetric("orchestrator.workflow_error", 1.0);
            }
            finally
            {
                _metrics?.EndOperation($"orchestrator_execute_workflow_{workflow.Name}");
            }

            return result;
        }

        private async Task<IAgent> SelectBestAgent(string task, List<IAgent> availableAgents)
        {
            // Score agents based on capabilities, load, and historical performance
            var scores = new Dictionary<IAgent, double>();

            foreach (var agent in availableAgents)
            {
                double score = 0;
                
                // Check if agent supports the required action types
                if (TaskRequiresActionType(task, out var requiredActionTypes))
                {
                    var supportedCount = requiredActionTypes
                        .Count(at => agent.Capabilities.SupportedActionTypes.Contains(at));
                    score += supportedCount * 10;
                }

                // Factor in skill levels
                var relevantSkill = GetRelevantSkill(task);
                if (agent.Capabilities.SkillLevels.TryGetValue(relevantSkill, out var skillLevel))
                {
                    score += skillLevel * 5;
                }

                // Consider load factor (lower is better)
                score -= agent.Capabilities.LoadFactor * 2;

                // Add historical performance
                score += agent.Capabilities.HistoricalPerformance * 3;

                scores[agent] = score;
            }

            return scores.OrderByDescending(kv => kv.Value).First().Key;
        }

        private bool TaskRequiresActionType(string task, out List<string> actionTypes)
        {
            // Analyze task to determine required action types
            actionTypes = new List<string>();
            
            if (task.Contains("weather", StringComparison.OrdinalIgnoreCase))
                actionTypes.Add("WeatherQuery");
            if (task.Contains("plan", StringComparison.OrdinalIgnoreCase))
                actionTypes.Add("Planning");
            if (task.Contains("urgent", StringComparison.OrdinalIgnoreCase))
                actionTypes.Add("ReactiveResponse");
            
            return actionTypes.Any();
        }

        private string GetRelevantSkill(string task)
        {
            // Determine relevant skill based on task
            if (task.Contains("weather", StringComparison.OrdinalIgnoreCase)) return "weather_analysis";
            if (task.Contains("plan", StringComparison.OrdinalIgnoreCase)) return "planning";
            if (task.Contains("urgent", StringComparison.OrdinalIgnoreCase)) return "quick_response";
            return "general";
        }

        private async Task<WorkflowResult> ExecuteSequentialWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();
            var context = new Dictionary<string, object>();

            foreach (var step in workflow.Steps)
            {
                try
                {
                    _logger.Log(LogLevel.Debug, $"Executing step: {step.Name}");
                    
                    var agent = await SelectAgentForStep(step);
                    var stepResult = await ExecuteWorkflowStep(agent, step, context);
                    
                    context[step.Name] = stepResult;
                    result.Results[step.Name] = stepResult;
                    
                    _logger.Log(LogLevel.Debug, $"Completed step: {step.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in step {step.Name}", ex);
                    result.Errors.Add($"Error in step {step.Name}: {ex.Message}");
                    break;
                }
            }

            result.Success = !result.Errors.Any();
            return result;
        }

        private async Task<WorkflowResult> ExecuteParallelWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();
            var context = new Dictionary<string, object>();
            var tasks = new List<Task>();
            var stepResults = new Dictionary<string, object>();
            var stepErrors = new Dictionary<string, string>();

            foreach (var step in workflow.Steps)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        _logger.Log(LogLevel.Debug, $"Executing step: {step.Name}");
                        
                        var agent = await SelectAgentForStep(step);
                        var stepResult = await ExecuteWorkflowStep(agent, step, context);
                        
                        lock (stepResults)
                        {
                            stepResults[step.Name] = stepResult;
                        }
                        
                        _logger.Log(LogLevel.Debug, $"Completed step: {step.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error in step {step.Name}", ex);
                        
                        lock (stepErrors)
                        {
                            stepErrors[step.Name] = ex.Message;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            result.Results = stepResults;
            result.Errors = stepErrors.Select(kv => $"Error in step {kv.Key}: {kv.Value}").ToList();
            result.Success = !result.Errors.Any();
            
            return result;
        }

        private async Task<WorkflowResult> ExecuteConditionalWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();
            var context = new Dictionary<string, object>();

            foreach (var step in workflow.Steps)
            {
                // Evaluate condition for this step
                if (await EvaluateStepCondition(step, context))
                {
                    try
                    {
                        _logger.Log(LogLevel.Debug, $"Executing step: {step.Name}");
                        
                        var agent = await SelectAgentForStep(step);
                        var stepResult = await ExecuteWorkflowStep(agent, step, context);
                        
                        context[step.Name] = stepResult;
                        result.Results[step.Name] = stepResult;
                        
                        _logger.Log(LogLevel.Debug, $"Completed step: {step.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error in step {step.Name}", ex);
                        result.Errors.Add($"Error in step {step.Name}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log(LogLevel.Debug, $"Skipped step: {step.Name} (condition not met)");
                }
            }

            result.Success = !result.Errors.Any();
            return result;
        }

        private async Task<WorkflowResult> ExecuteHierarchicalWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();
            
            // Find root steps (no dependencies)
            var rootSteps = workflow.Steps.Where(s => !s.Dependencies.Any()).ToList();
            
            // Execute workflow hierarchically
            foreach (var rootStep in rootSteps)
            {
                try
                {
                    var subResult = await ExecuteStepHierarchy(rootStep, workflow.Steps, new Dictionary<string, object>());
                    result.Results[rootStep.Name] = subResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in hierarchical step {rootStep.Name}", ex);
                    result.Errors.Add($"Error in hierarchical step {rootStep.Name}: {ex.Message}");
                }
            }

            result.Success = !result.Errors.Any();
            return result;
        }

        private async Task<object> ExecuteStepHierarchy(WorkflowStep step, List<WorkflowStep> allSteps, Dictionary<string, object> context)
        {
            // Execute current step
            _logger.Log(LogLevel.Debug, $"Executing hierarchical step: {step.Name}");
            
            var agent = await SelectAgentForStep(step);
            var stepResult = await ExecuteWorkflowStep(agent, step, context);
            
            context[step.Name] = stepResult;
            
            // Find child steps
            var childSteps = allSteps.Where(s => s.Dependencies.Contains(step.Name)).ToList();
            
            // Execute child steps
            var childResults = new Dictionary<string, object>();
            foreach (var childStep in childSteps)
            {
                childResults[childStep.Name] = await ExecuteStepHierarchy(childStep, allSteps, context);
            }
            
            return new { StepResult = stepResult, ChildResults = childResults };
        }

        private async Task<bool> EvaluateStepCondition(WorkflowStep step, Dictionary<string, object> context)
        {
            // Evaluate step condition based on context
            if (step.Parameters.TryGetValue("condition", out var condition))
            {
                var conditionStr = condition.ToString();
                
                // Check for dependency conditions
                foreach (var dependency in step.Dependencies)
                {
                    if (context.TryGetValue(dependency, out var depResult))
                    {
                        // Simple condition evaluation
                        if (conditionStr.Contains($"{dependency}.Success") && depResult is ActionResult actionResult)
                        {
                            return actionResult.Success;
                        }
                    }
                    else
                    {
                        // Dependency not satisfied
                        return false;
                    }
                }
            }
            
            // Default to true if no condition specified
            return true;
        }

        private async Task<IAgent> SelectAgentForStep(WorkflowStep step)
        {
            var candidates = _agents.Values.Where(a => a.Type == step.RequiredAgentType).ToList();
            
            if (!candidates.Any())
            {
                throw new InvalidOperationException($"No agents of type {step.RequiredAgentType} available for step {step.Name}");
            }
            
            return await SelectBestAgent(step.Name, candidates);
        }

        private async Task<object> ExecuteWorkflowStep(IAgent agent, WorkflowStep step, Dictionary<string, object> context)
        {
            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>(step.Parameters)
            };

            // Add dependencies from context
            foreach (var dependency in step.Dependencies)
            {
                if (context.TryGetValue(dependency, out var dependencyResult))
                {
                    state.Properties[$"dep_{dependency}"] = dependencyResult;
                }
            }

            var action = await agent.DecideAction(state);
            var result = await action.Execute();
            
            // Publish step completion event
            await _messageBus.PublishAsync($"workflow.step.completed", new
            {
                StepName = step.Name,
                AgentId = agent.Id,
                Action = action,
                Result = result
            });
            
            return result;
        }
    }
}
