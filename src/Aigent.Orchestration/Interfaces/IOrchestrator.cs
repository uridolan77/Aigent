using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Orchestration.Models;

namespace Aigent.Orchestration.Interfaces
{
    /// <summary>
    /// Interface for orchestration services that coordinate multiple agents
    /// </summary>
    public interface IOrchestrator
    {
        /// <summary>
        /// Registers an agent with the orchestrator
        /// </summary>
        /// <param name="agent">The agent to register</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RegisterAgentAsync(IAgent agent);
        
        /// <summary>
        /// Unregisters an agent from the orchestrator
        /// </summary>
        /// <param name="agentId">ID of the agent to unregister</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task UnregisterAgentAsync(string agentId);
        
        /// <summary>
        /// Assigns a task to the most suitable agent
        /// </summary>
        /// <param name="task">Description of the task</param>
        /// <param name="requirements">Optional requirements for agent selection</param>
        /// <returns>The selected agent</returns>
        Task<IAgent> AssignTaskAsync(string task, AgentRequirements requirements = null);
        
        /// <summary>
        /// Executes a workflow involving multiple agents
        /// </summary>
        /// <param name="workflow">Definition of the workflow</param>
        /// <param name="context">Optional workflow context</param>
        /// <returns>Result of the workflow execution</returns>
        Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, WorkflowContext context = null);
        
        /// <summary>
        /// Gets all registered agents
        /// </summary>
        /// <returns>Collection of registered agents</returns>
        IReadOnlyCollection<IAgent> GetRegisteredAgents();
        
        /// <summary>
        /// Gets the agent with the specified ID
        /// </summary>
        /// <param name="agentId">ID of the agent to get</param>
        /// <returns>The agent with the specified ID, or null if not found</returns>
        IAgent GetAgent(string agentId);
        
        /// <summary>
        /// Gets the status of a workflow
        /// </summary>
        /// <param name="workflowId">ID of the workflow</param>
        /// <returns>Status of the workflow</returns>
        Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId);
        
        /// <summary>
        /// Cancels a running workflow
        /// </summary>
        /// <param name="workflowId">ID of the workflow to cancel</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CancelWorkflowAsync(string workflowId);
        
        /// <summary>
        /// Configures the orchestrator with the specified options
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        void Configure(OrchestratorConfiguration configuration);
    }
}
