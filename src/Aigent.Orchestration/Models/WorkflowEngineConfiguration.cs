using System;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Configuration options for workflow engines
    /// </summary>
    public class WorkflowEngineConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent workflows
        /// </summary>
        public int MaxConcurrentWorkflows { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent steps per workflow
        /// </summary>
        public int MaxConcurrentStepsPerWorkflow { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the default timeout for workflows in seconds
        /// </summary>
        public int DefaultWorkflowTimeoutSeconds { get; set; } = 300;
        
        /// <summary>
        /// Gets or sets the default timeout for workflow steps in seconds
        /// </summary>
        public int DefaultStepTimeoutSeconds { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets whether to persist workflow state
        /// </summary>
        public bool PersistWorkflowState { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the workflow state persistence interval in seconds
        /// </summary>
        public int WorkflowStatePersistenceIntervalSeconds { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets whether to enable retry for failed steps
        /// </summary>
        public bool EnableStepRetry { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the default maximum retry count for steps
        /// </summary>
        public int DefaultMaxStepRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the default retry delay in seconds
        /// </summary>
        public int DefaultRetryDelaySeconds { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets whether to enable step timeouts
        /// </summary>
        public bool EnableStepTimeouts { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to enable workflow timeouts
        /// </summary>
        public bool EnableWorkflowTimeouts { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the default error handling mode for workflows
        /// </summary>
        public ErrorHandlingMode DefaultErrorHandlingMode { get; set; } = ErrorHandlingMode.StopWorkflow;
        
        /// <summary>
        /// Gets or sets whether to enable metrics collection
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
        
        /// <summary>
        /// Creates a default configuration
        /// </summary>
        /// <returns>Default configuration</returns>
        public static WorkflowEngineConfiguration Default() => new WorkflowEngineConfiguration();
    }
}
