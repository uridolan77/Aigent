using System.Threading.Tasks;

namespace Aigent.Orchestration
{
    /// <summary>
    /// Interface for orchestrators
    /// </summary>
    public interface IOrchestrator
    {
        /// <summary>
        /// Executes a workflow
        /// </summary>
        /// <param name="workflow">Workflow to execute</param>
        /// <returns>Result of the workflow execution</returns>
        Task<WorkflowResult> ExecuteWorkflow(WorkflowDefinition workflow);
    }
}
