using System;
using System.Collections.Generic;
using Aigent.Core.Models;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Definition of a step in a workflow
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Gets or sets the unique identifier of the step
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the type of agent required for this step
        /// </summary>
        public string RequiredAgentType { get; set; }
        
        /// <summary>
        /// Gets or sets the agent capabilities required for this step
        /// </summary>
        public List<string> RequiredCapabilities { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the parameters for the step
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the dependencies on other steps (step IDs)
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the condition for executing this step
        /// </summary>
        public string Condition { get; set; }
        
        /// <summary>
        /// Gets or sets the timeout for this step in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets the number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the delay between retry attempts in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the step to execute if this step fails
        /// </summary>
        public string FallbackStepId { get; set; }
        
        /// <summary>
        /// Gets or sets whether this step is critical for the workflow
        /// </summary>
        public bool IsCritical { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to continue executing subsequent steps if this step fails
        /// </summary>
        public bool ContinueOnFailure { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the metadata associated with this step
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
