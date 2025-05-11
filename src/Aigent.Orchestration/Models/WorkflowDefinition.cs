using System;
using System.Collections.Generic;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Definition of a workflow
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// Gets or sets the unique identifier of the workflow
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the name of the workflow
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the workflow
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the workflow
        /// </summary>
        public WorkflowType Type { get; set; } = WorkflowType.Sequential;
        
        /// <summary>
        /// Gets or sets the steps in the workflow
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
        
        /// <summary>
        /// Gets or sets the maximum timeout for the workflow in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 600; // 10 minutes default
        
        /// <summary>
        /// Gets or sets the tags associated with the workflow
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the version of the workflow
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// Gets or sets the error handling mode
        /// </summary>
        public ErrorHandlingMode ErrorHandling { get; set; } = ErrorHandlingMode.StopOnError;
        
        /// <summary>
        /// Gets or sets whether the workflow is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the metadata associated with the workflow
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        Hierarchical,
        
        /// <summary>
        /// Steps are executed in a feedback loop
        /// </summary>
        FeedbackLoop
    }
    
    /// <summary>
    /// Error handling modes for workflows
    /// </summary>
    public enum ErrorHandlingMode
    {
        /// <summary>
        /// Stop workflow execution when an error occurs
        /// </summary>
        StopOnError,
        
        /// <summary>
        /// Continue workflow execution when an error occurs
        /// </summary>
        ContinueOnError,
        
        /// <summary>
        /// Retry the step when an error occurs
        /// </summary>
        RetryOnError,
        
        /// <summary>
        /// Use alternative steps when an error occurs
        /// </summary>
        UseAlternative
    }
}
