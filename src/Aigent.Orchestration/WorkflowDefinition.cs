using System.Collections.Generic;
using Aigent.Core;

namespace Aigent.Orchestration
{
    /// <summary>
    /// Types of workflows
    /// </summary>
    public enum WorkflowType
    {
        /// <summary>
        /// Sequential workflow
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Parallel workflow
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Conditional workflow
        /// </summary>
        Conditional
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
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    }
    
    /// <summary>
    /// Step in a workflow
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Required agent type for the step
        /// </summary>
        public AgentType RequiredAgentType { get; set; }
        
        /// <summary>
        /// Parameters for the step
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Dependencies of the step
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        
        /// <summary>
        /// Condition for the step
        /// </summary>
        public string Condition { get; set; }
    }
}
