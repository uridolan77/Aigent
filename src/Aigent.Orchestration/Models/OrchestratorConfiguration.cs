using System;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Configuration options for orchestrators
    /// </summary>
    public class OrchestratorConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent workflows
        /// </summary>
        public int MaxConcurrentWorkflows { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the default timeout for workflows in seconds
        /// </summary>
        public int DefaultWorkflowTimeoutSeconds { get; set; } = 300;
        
        /// <summary>
        /// Gets or sets the default timeout for workflow steps in seconds
        /// </summary>
        public int DefaultStepTimeoutSeconds { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets whether to enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the workflow history retention period
        /// </summary>
        public TimeSpan WorkflowHistoryRetention { get; set; } = TimeSpan.FromDays(7);
        
        /// <summary>
        /// Gets or sets whether to enable metrics collection
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to validate workflows before execution
        /// </summary>
        public bool ValidateWorkflows { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to validate steps before execution
        /// </summary>
        public bool ValidateSteps { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to enable agent auto-scaling
        /// </summary>
        public bool EnableAgentAutoScaling { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to enable workflow persistence
        /// </summary>
        public bool EnableWorkflowPersistence { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the throttling threshold for API rate limits
        /// </summary>
        public int ApiRateLimitThreshold { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the default error handling mode for workflows
        /// </summary>
        public ErrorHandlingMode DefaultErrorHandlingMode { get; set; } = ErrorHandlingMode.StopWorkflow;
        
        /// <summary>
        /// Creates a default configuration
        /// </summary>
        /// <returns>Default configuration</returns>
        public static OrchestratorConfiguration Default() => new OrchestratorConfiguration();
    }
    
    /// <summary>
    /// Requirements for agent selection
    /// </summary>
    public class AgentRequirements
    {
        /// <summary>
        /// Gets or sets the required agent type
        /// </summary>
        public string AgentType { get; set; }
        
        /// <summary>
        /// Gets or sets the required agent capabilities
        /// </summary>
        public string[] RequiredCapabilities { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum skill level
        /// </summary>
        public double MinimumSkillLevel { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the maximum load factor
        /// </summary>
        public double MaxLoadFactor { get; set; } = 1.0;
    }
}
