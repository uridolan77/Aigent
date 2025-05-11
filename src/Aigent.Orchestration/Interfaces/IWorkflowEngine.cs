using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Orchestration.Models;

namespace Aigent.Orchestration.Interfaces
{
    /// <summary>
    /// Interface for workflow execution engines
    /// </summary>
    public interface IWorkflowEngine
    {
        /// <summary>
        /// Executes a workflow
        /// </summary>
        /// <param name="workflow">Workflow to execute</param>
        /// <param name="context">Context for workflow execution</param>
        /// <returns>Result of the workflow execution</returns>
        Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, WorkflowContext context);
        
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
        /// Gets all running workflows
        /// </summary>
        /// <returns>Collection of running workflow statuses</returns>
        IReadOnlyCollection<WorkflowStatus> GetRunningWorkflows();
        
        /// <summary>
        /// Configures the workflow engine
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        void Configure(WorkflowEngineConfiguration configuration);
    }
}
