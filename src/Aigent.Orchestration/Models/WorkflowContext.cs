using System;
using System.Collections.Generic;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Context for workflow execution
    /// </summary>
    public class WorkflowContext
    {
        /// <summary>
        /// Gets or sets the ID of the workflow instance
        /// </summary>
        public string InstanceId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the parent workflow instance ID if this is a sub-workflow
        /// </summary>
        public string ParentInstanceId { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation ID for tracking related workflows
        /// </summary>
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the user ID associated with the workflow
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Gets or sets the tenant ID associated with the workflow
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// Gets or sets input data for the workflow
        /// </summary>
        public Dictionary<string, object> InputData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets variables that can be shared across steps
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets a value indicating whether to validate steps before execution
        /// </summary>
        public bool ValidateSteps { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether to execute the workflow asynchronously
        /// </summary>
        public bool ExecuteAsync { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether to log detailed information about the workflow execution
        /// </summary>
        public bool DetailedLogging { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the start time of the workflow execution
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the metadata associated with the workflow context
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a default workflow context
        /// </summary>
        /// <returns>A default workflow context</returns>
        public static WorkflowContext Default()
        {
            return new WorkflowContext();
        }
        
        /// <summary>
        /// Creates a workflow context with specific input data
        /// </summary>
        /// <param name="inputData">Input data for the workflow</param>
        /// <returns>A workflow context with the specified input data</returns>
        public static WorkflowContext WithInputData(Dictionary<string, object> inputData)
        {
            return new WorkflowContext { InputData = inputData ?? new Dictionary<string, object>() };
        }
    }
}
