using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core.Interfaces;
using Aigent.Monitoring;
using Aigent.Orchestration.Interfaces;
using Aigent.Orchestration.Models;

namespace Aigent.Orchestration.Compatibility
{
    /// <summary>
    /// Provides backward compatibility with older orchestration implementations
    /// </summary>
    public static class LegacySupport
    {
        /// <summary>
        /// Creates an adapter for the old IOrchestrator interface
        /// </summary>
        /// <param name="modernOrchestrator">Modern orchestrator implementation</param>
        /// <returns>Adapter implementing the old IOrchestrator interface</returns>
        public static global::Aigent.Orchestration.IOrchestrator CreateLegacyOrchestrator(
            Interfaces.IOrchestrator modernOrchestrator)
        {
            return new LegacyOrchestratorAdapter(modernOrchestrator);
        }
        
        /// <summary>
        /// Converts a legacy workflow definition to the modern format
        /// </summary>
        /// <param name="legacyWorkflow">Legacy workflow definition</param>
        /// <returns>Modern workflow definition</returns>
        public static WorkflowDefinition ConvertToModernWorkflow(global::Aigent.Orchestration.WorkflowDefinition legacyWorkflow)
        {
            var modernWorkflow = new WorkflowDefinition
            {
                Name = legacyWorkflow.Name,
                Type = ConvertWorkflowType(legacyWorkflow.Type)
            };
            
            foreach (var legacyStep in legacyWorkflow.Steps)
            {
                modernWorkflow.Steps.Add(new WorkflowStep
                {
                    Name = legacyStep.Name,
                    RequiredAgentType = legacyStep.RequiredAgentType,
                    Parameters = legacyStep.Parameters,
                    Dependencies = legacyStep.Dependencies,
                    Condition = legacyStep.Condition
                });
            }
            
            return modernWorkflow;
        }
        
        /// <summary>
        /// Converts a modern workflow result to the legacy format
        /// </summary>
        /// <param name="modernResult">Modern workflow result</param>
        /// <returns>Legacy workflow result</returns>
        public static global::Aigent.Orchestration.WorkflowResult ConvertToLegacyResult(WorkflowResult modernResult)
        {
            var legacyResult = new global::Aigent.Orchestration.WorkflowResult
            {
                Success = modernResult.Success,
                Message = modernResult.Message
            };
            
            foreach (var kvp in modernResult.StepResults)
            {
                legacyResult.Results[kvp.Key] = kvp.Value;
            }
            
            return legacyResult;
        }
        
        private static WorkflowType ConvertWorkflowType(global::Aigent.Orchestration.WorkflowType legacyType)
        {
            return legacyType switch
            {
                global::Aigent.Orchestration.WorkflowType.Sequential => WorkflowType.Sequential,
                global::Aigent.Orchestration.WorkflowType.Parallel => WorkflowType.Parallel,
                global::Aigent.Orchestration.WorkflowType.Conditional => WorkflowType.Conditional,
                _ => WorkflowType.Sequential
            };
        }
        
        /// <summary>
        /// Adapter that implements the old IOrchestrator interface using the new implementation
        /// </summary>
        private class LegacyOrchestratorAdapter : global::Aigent.Orchestration.IOrchestrator
        {
            private readonly Interfaces.IOrchestrator _modernOrchestrator;
            
            public LegacyOrchestratorAdapter(Interfaces.IOrchestrator modernOrchestrator)
            {
                _modernOrchestrator = modernOrchestrator ?? throw new ArgumentNullException(nameof(modernOrchestrator));
            }
            
            public Task<global::Aigent.Orchestration.WorkflowResult> ExecuteWorkflow(global::Aigent.Orchestration.WorkflowDefinition workflow)
            {
                // Convert legacy workflow to modern format
                var modernWorkflow = ConvertToModernWorkflow(workflow);
                
                // Execute using modern orchestrator
                return _modernOrchestrator.ExecuteWorkflowAsync(modernWorkflow)
                    .ContinueWith(t => ConvertToLegacyResult(t.Result));
            }
        }
    }
    
    /// <summary>
    /// Adapter that implements the old IOrchestrator2 interface using the new implementation
    /// </summary>
    public class LegacyOrchestrator2Adapter : global::Aigent.Orchestration.IOrchestrator
    {
        private readonly Interfaces.IOrchestrator _modernOrchestrator;
        private readonly ILogger _logger;
        
        public LegacyOrchestrator2Adapter(Interfaces.IOrchestrator modernOrchestrator, ILogger logger)
        {
            _modernOrchestrator = modernOrchestrator ?? throw new ArgumentNullException(nameof(modernOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task RegisterAgent(IAgent agent)
        {
            await _modernOrchestrator.RegisterAgentAsync(agent);
        }
        
        public async Task UnregisterAgent(string agentId)
        {
            await _modernOrchestrator.UnregisterAgentAsync(agentId);
        }
        
        public async Task<IAgent> AssignTask(string task)
        {
            return await _modernOrchestrator.AssignTaskAsync(task);
        }
        
        public async Task<global::Aigent.Orchestration.WorkflowResult> ExecuteWorkflow(global::Aigent.Orchestration.WorkflowDefinition workflow)
        {
            try
            {
                // Convert legacy workflow to modern format
                var modernWorkflow = LegacySupport.ConvertToModernWorkflow(workflow);
                
                // Execute using modern orchestrator
                var result = await _modernOrchestrator.ExecuteWorkflowAsync(modernWorkflow);
                
                // Convert result back to legacy format
                return LegacySupport.ConvertToLegacyResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing workflow '{workflow?.Name}': {ex.Message}", ex);
                
                return new global::Aigent.Orchestration.WorkflowResult
                {
                    Success = false,
                    Message = $"Error executing workflow: {ex.Message}"
                };
            }
        }
    }
}
