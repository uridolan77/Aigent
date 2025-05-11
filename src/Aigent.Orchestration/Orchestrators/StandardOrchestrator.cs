using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Monitoring;
using Aigent.Communication.Interfaces;
using Aigent.Safety.Interfaces;
using Aigent.Orchestration.Interfaces;
using Aigent.Orchestration.Models;

namespace Aigent.Orchestration.Orchestrators
{
    /// <summary>
    /// Standard implementation of IOrchestrator
    /// </summary>
    public class StandardOrchestrator : IOrchestrator
    {
        private readonly ConcurrentDictionary<string, IAgent> _agents = new();
        private readonly ConcurrentDictionary<string, WorkflowStatus> _workflowStatuses = new();
        private readonly ILogger _logger;
        private readonly ISafetyValidator _safetyValidator;
        private readonly IMessageBus _messageBus;
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IMetricsCollector _metrics;
        private OrchestratorConfiguration _configuration = OrchestratorConfiguration.Default();
        
        /// <summary>
        /// Initializes a new instance of the StandardOrchestrator class
        /// </summary>
        /// <param name="logger">Logger for orchestration activities</param>
        /// <param name="safetyValidator">Safety validator for ensuring actions are safe</param>
        /// <param name="messageBus">Message bus for agent communication</param>
        /// <param name="workflowEngine">Engine for executing workflows</param>
        /// <param name="metrics">Metrics collector for monitoring</param>
        public StandardOrchestrator(
            ILogger logger, 
            ISafetyValidator safetyValidator,
            IMessageBus messageBus,
            IWorkflowEngine workflowEngine,
            IMetricsCollector metrics = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyValidator = safetyValidator ?? throw new ArgumentNullException(nameof(safetyValidator));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
            _metrics = metrics;
            
            // Subscribe to workflow status updates
            _messageBus.SubscribeAsync<WorkflowStatus>("workflow.status.updated", OnWorkflowStatusUpdated);
        }
        
        /// <summary>
        /// Registers an agent with the orchestrator
        /// </summary>
        /// <param name="agent">The agent to register</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task RegisterAgentAsync(IAgent agent)
        {
            _metrics?.RecordMetric("orchestrator.register_agent.start", 1.0);
            
            try
            {
                if (agent == null)
                {
                    throw new ArgumentNullException(nameof(agent));
                }
                
                _agents[agent.Id] = agent;
                _logger.Log(LogLevel.Information, $"Registered agent: {agent.Name} ({agent.Id})");
                _metrics?.RecordMetric("orchestrator.agents.count", _agents.Count);
                
                // Notify other components about new agent
                _messageBus.PublishAsync("agent.registered", new { AgentId = agent.Id, AgentName = agent.Name });
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering agent: {ex.Message}", ex);
                throw;
            }
            finally
            {
                _metrics?.RecordMetric("orchestrator.register_agent.end", 1.0);
            }
        }
        
        /// <summary>
        /// Unregisters an agent from the orchestrator
        /// </summary>
        /// <param name="agentId">ID of the agent to unregister</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task UnregisterAgentAsync(string agentId)
        {
            _metrics?.RecordMetric("orchestrator.unregister_agent.start", 1.0);
            
            try
            {
                if (string.IsNullOrEmpty(agentId))
                {
                    throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
                }
                
                if (_agents.TryRemove(agentId, out var agent))
                {
                    _logger.Log(LogLevel.Information, $"Unregistered agent: {agent.Name} ({agentId})");
                    _metrics?.RecordMetric("orchestrator.agents.count", _agents.Count);
                    
                    // Notify other components about removed agent
                    _messageBus.PublishAsync("agent.unregistered", new { AgentId = agentId });
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Attempted to unregister unknown agent: {agentId}");
                }
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unregistering agent: {ex.Message}", ex);
                throw;
            }
            finally
            {
                _metrics?.RecordMetric("orchestrator.unregister_agent.end", 1.0);
            }
        }
        
        /// <summary>
        /// Assigns a task to the most suitable agent
        /// </summary>
        /// <param name="task">Description of the task</param>
        /// <param name="requirements">Optional requirements for agent selection</param>
        /// <returns>The selected agent</returns>
        public async Task<IAgent> AssignTaskAsync(string task, AgentRequirements requirements = null)
        {
            _metrics?.RecordMetric("orchestrator.assign_task.start", 1.0);
            
            try
            {
                if (string.IsNullOrEmpty(task))
                {
                    throw new ArgumentException("Task cannot be null or empty", nameof(task));
                }
                
                // Validate task with safety validator
                var safetyResult = await _safetyValidator.ValidateAsync(task);
                if (!safetyResult.IsValid)
                {
                    throw new InvalidOperationException($"Task failed safety validation: {safetyResult.Message}");
                }
                
                _logger.Log(LogLevel.Debug, $"Assigning task: {task}");
                
                // Find suitable agents
                var candidates = _agents.Values.ToList();
                
                // Apply requirements filter if provided
                if (requirements != null)
                {
                    candidates = FilterAgentsByRequirements(candidates, requirements);
                }
                
                if (candidates.Count == 0)
                {
                    throw new InvalidOperationException("No suitable agents found for the task");
                }
                
                // Select the best agent
                var selectedAgent = await SelectBestAgentForTaskAsync(task, candidates);
                
                _logger.Log(LogLevel.Information, $"Assigned task '{task}' to agent '{selectedAgent.Name}' ({selectedAgent.Id})");
                _metrics?.RecordMetric("orchestrator.task_assignments.count", 1.0);
                
                // Notify about task assignment
                await _messageBus.PublishAsync("task.assigned", new 
                { 
                    TaskDescription = task, 
                    AgentId = selectedAgent.Id,
                    AgentName = selectedAgent.Name,
                    Timestamp = DateTime.UtcNow
                });
                
                return selectedAgent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning task: {ex.Message}", ex);
                throw;
            }
            finally
            {
                _metrics?.RecordMetric("orchestrator.assign_task.end", 1.0);
            }
        }
        
        /// <summary>
        /// Executes a workflow involving multiple agents
        /// </summary>
        /// <param name="workflow">Definition of the workflow</param>
        /// <param name="context">Optional workflow context</param>
        /// <returns>Result of the workflow execution</returns>
        public async Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, WorkflowContext context = null)
        {
            _metrics?.RecordMetric($"orchestrator.execute_workflow.{workflow.Name}.start", 1.0);
            
            try
            {
                if (workflow == null)
                {
                    throw new ArgumentNullException(nameof(workflow));
                }
                
                // Validate workflow definition
                var validationErrors = workflow.Validate();
                if (validationErrors.Count > 0)
                {
                    throw new InvalidOperationException($"Invalid workflow definition: {string.Join(", ", validationErrors)}");
                }
                
                _logger.Log(LogLevel.Information, $"Executing workflow: {workflow.Name} (ID: {workflow.Id})");
                
                // Create context if not provided
                context ??= WorkflowContext.Default();
                
                // Execute workflow using engine
                var result = await _workflowEngine.ExecuteWorkflowAsync(workflow, context);
                
                _logger.Log(result.Success 
                    ? LogLevel.Information 
                    : LogLevel.Warning, 
                    $"Workflow execution {(result.Success ? "completed successfully" : "failed")}: {workflow.Name} (ID: {workflow.Id})");
                
                _metrics?.RecordMetric("orchestrator.workflow.success", result.Success ? 1.0 : 0.0);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing workflow '{workflow?.Name}': {ex.Message}", ex);
                
                return WorkflowResult.Failed(
                    workflow?.Id ?? Guid.NewGuid().ToString(), 
                    workflow?.Name ?? "Unknown", 
                    $"Workflow execution error: {ex.Message}", 
                    new List<WorkflowError> 
                    { 
                        new WorkflowError 
                        { 
                            Code = "EXECUTION_ERROR", 
                            Message = ex.Message,
                            Severity = ErrorSeverity.Critical,
                            Details = new Dictionary<string, object> 
                            { 
                                ["ExceptionType"] = ex.GetType().Name,
                                ["StackTrace"] = ex.StackTrace
                            }
                        } 
                    });
            }
            finally
            {
                _metrics?.RecordMetric($"orchestrator.execute_workflow.{workflow?.Name}.end", 1.0);
            }
        }
        
        /// <summary>
        /// Gets all registered agents
        /// </summary>
        /// <returns>Collection of registered agents</returns>
        public IReadOnlyCollection<IAgent> GetRegisteredAgents()
        {
            return _agents.Values.ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Gets the agent with the specified ID
        /// </summary>
        /// <param name="agentId">ID of the agent to get</param>
        /// <returns>The agent with the specified ID, or null if not found</returns>
        public IAgent GetAgent(string agentId)
        {
            _agents.TryGetValue(agentId, out var agent);
            return agent;
        }
        
        /// <summary>
        /// Gets the status of a workflow
        /// </summary>
        /// <param name="workflowId">ID of the workflow</param>
        /// <returns>Status of the workflow</returns>
        public async Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId)
        {
            if (string.IsNullOrEmpty(workflowId))
            {
                throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));
            }
            
            // Check local cache first
            if (_workflowStatuses.TryGetValue(workflowId, out var cachedStatus))
            {
                return cachedStatus;
            }
            
            // If not in cache, get from workflow engine
            var status = await _workflowEngine.GetWorkflowStatusAsync(workflowId);
            
            // Cache the status
            if (status != null)
            {
                _workflowStatuses[workflowId] = status;
            }
            
            return status;
        }
        
        /// <summary>
        /// Cancels a running workflow
        /// </summary>
        /// <param name="workflowId">ID of the workflow to cancel</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CancelWorkflowAsync(string workflowId)
        {
            if (string.IsNullOrEmpty(workflowId))
            {
                throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));
            }
            
            _logger.Log(LogLevel.Information, $"Cancelling workflow: {workflowId}");
            
            await _workflowEngine.CancelWorkflowAsync(workflowId);
            
            _logger.Log(LogLevel.Information, $"Workflow cancelled: {workflowId}");
        }
        
        /// <summary>
        /// Configures the orchestrator with the specified options
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        public void Configure(OrchestratorConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _logger.Log(LogLevel.Information, "Orchestrator configuration updated");
            
            // Configure the workflow engine
            var engineConfig = new WorkflowEngineConfiguration
            {
                MaxConcurrentWorkflows = _configuration.MaxConcurrentWorkflows,
                DefaultWorkflowTimeoutSeconds = _configuration.DefaultWorkflowTimeoutSeconds,
                DefaultStepTimeoutSeconds = _configuration.DefaultStepTimeoutSeconds,
                EnableDetailedLogging = _configuration.EnableDetailedLogging,
                EnableMetrics = _configuration.EnableMetrics,
                DefaultErrorHandlingMode = _configuration.DefaultErrorHandlingMode
            };
            
            _workflowEngine.Configure(engineConfig);
        }
        
        private List<IAgent> FilterAgentsByRequirements(List<IAgent> agents, AgentRequirements requirements)
        {
            var filtered = agents;
            
            // Filter by agent type if specified
            if (!string.IsNullOrEmpty(requirements.AgentType))
            {
                filtered = filtered.Where(a => a.Type.ToString() == requirements.AgentType).ToList();
            }
            
            // Filter by capabilities if specified
            if (requirements.RequiredCapabilities?.Length > 0)
            {
                filtered = filtered.Where(a => 
                    requirements.RequiredCapabilities.All(c => 
                        a.Capabilities.SupportedActionTypes.Contains(c)))
                    .ToList();
            }
            
            // Filter by skill level
            if (requirements.MinimumSkillLevel > 0)
            {
                filtered = filtered.Where(a => 
                    a.Capabilities.SkillLevels.Values.Average() >= requirements.MinimumSkillLevel)
                    .ToList();
            }
            
            // Filter by load factor
            if (requirements.MaxLoadFactor < 1.0)
            {
                filtered = filtered.Where(a => 
                    a.Capabilities.LoadFactor <= requirements.MaxLoadFactor)
                    .ToList();
            }
            
            return filtered;
        }
        
        private async Task<IAgent> SelectBestAgentForTaskAsync(string task, List<IAgent> candidates)
        {
            // Simple scoring mechanism for agent selection
            var scores = new ConcurrentDictionary<string, double>();
            var taskLower = task.ToLowerInvariant();
            
            // Perform parallel scoring
            await Task.WhenAll(candidates.Select(async agent => 
            {
                var score = 0.0;
                
                // Base score from historical performance
                score += agent.Capabilities.HistoricalPerformance * 3;
                
                // Consider load factor (lower is better)
                score -= agent.Capabilities.LoadFactor * 2;
                
                // Check if agent handles relevant areas based on task keywords
                foreach (var entry in agent.Capabilities.SkillLevels)
                {
                    if (taskLower.Contains(entry.Key.ToLowerInvariant()))
                    {
                        score += entry.Value * 5;
                    }
                }
                
                // Additional task-specific scoring could be implemented here
                
                scores[agent.Id] = score;
                return Task.CompletedTask;
            }));
            
            // Select the agent with the highest score
            var bestAgentId = scores.OrderByDescending(x => x.Value).First().Key;
            return candidates.First(a => a.Id == bestAgentId);
        }
        
        private Task OnWorkflowStatusUpdated(WorkflowStatus status)
        {
            // Update the cached status
            _workflowStatuses[status.WorkflowId] = status;
            
            // Log state changes
            _logger.Log(LogLevel.Debug, $"Workflow {status.WorkflowId} status updated: {status.State}");
            
            return Task.CompletedTask;
        }
    }
}
