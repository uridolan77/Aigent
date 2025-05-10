using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core;

namespace Aigent.Orchestration
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
        Task RegisterAgent(IAgent agent);
        
        /// <summary>
        /// Unregisters an agent from the orchestrator
        /// </summary>
        /// <param name="agentId">ID of the agent to unregister</param>
        Task UnregisterAgent(string agentId);
        
        /// <summary>
        /// Assigns a task to the most suitable agent
        /// </summary>
        /// <param name="task">Description of the task</param>
        /// <returns>The selected agent</returns>
        Task<IAgent> AssignTask(string task);
        
        /// <summary>
        /// Executes a workflow involving multiple agents
        /// </summary>
        /// <param name="workflow">Definition of the workflow</param>
        /// <returns>Result of the workflow execution</returns>
        Task<WorkflowResult> ExecuteWorkflow(WorkflowDefinition workflow);
    }

    /// <summary>
    /// Types of workflows
    /// </summary>
    public enum WorkflowType
    {
        /// <summary>
        /// Steps are executed in sequence
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Steps are executed in parallel
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Steps are executed based on conditions
        /// </summary>
        Conditional,
        
        /// <summary>
        /// Steps are executed in a hierarchical structure
        /// </summary>
        Hierarchical
    }

    /// <summary>
    /// Definition of a workflow
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// Name of the workflow
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the workflow
        /// </summary>
        public WorkflowType Type { get; set; }
        
        /// <summary>
        /// Steps in the workflow
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = new();
    }

    /// <summary>
    /// Definition of a step in a workflow
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of agent required for this step
        /// </summary>
        public AgentType RequiredAgentType { get; set; }
        
        /// <summary>
        /// Parameters for the step
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// Dependencies on other steps
        /// </summary>
        public List<string> Dependencies { get; set; } = new();
    }

    /// <summary>
    /// Result of a workflow execution
    /// </summary>
    public class WorkflowResult
    {
        /// <summary>
        /// Whether the workflow was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Results of individual steps
        /// </summary>
        public Dictionary<string, object> Results { get; set; } = new();
        
        /// <summary>
        /// Errors that occurred during execution
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}
