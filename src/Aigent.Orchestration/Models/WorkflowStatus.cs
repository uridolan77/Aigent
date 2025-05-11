using System;
using System.Collections.Generic;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Status of a workflow execution
    /// </summary>
    public class WorkflowStatus
    {
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }
        
        /// <summary>
        /// Gets or sets the instance ID of the workflow
        /// </summary>
        public string InstanceId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the workflow
        /// </summary>
        public string WorkflowName { get; set; }
        
        /// <summary>
        /// Gets or sets the current state of the workflow
        /// </summary>
        public WorkflowState State { get; set; } = WorkflowState.NotStarted;
        
        /// <summary>
        /// Gets or sets the start time of the workflow execution
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the end time of the workflow execution
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the current step being executed
        /// </summary>
        public string CurrentStepId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the current step being executed
        /// </summary>
        public string CurrentStepName { get; set; }
        
        /// <summary>
        /// Gets or sets the progress of the workflow as a percentage
        /// </summary>
        public int ProgressPercentage { get; set; }
        
        /// <summary>
        /// Gets or sets the number of completed steps
        /// </summary>
        public int CompletedSteps { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of steps
        /// </summary>
        public int TotalSteps { get; set; }
        
        /// <summary>
        /// Gets or sets the number of failed steps
        /// </summary>
        public int FailedSteps { get; set; }
        
        /// <summary>
        /// Gets or sets any error message associated with the workflow
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the status of individual steps
        /// </summary>
        public Dictionary<string, StepStatus> StepStatuses { get; set; } = new Dictionary<string, StepStatus>();
        
        /// <summary>
        /// Gets or sets the metadata associated with the workflow status
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Status of a workflow step
    /// </summary>
    public class StepStatus
    {
        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string StepId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string StepName { get; set; }
        
        /// <summary>
        /// Gets or sets the current state of the step
        /// </summary>
        public StepState State { get; set; } = StepState.NotStarted;
        
        /// <summary>
        /// Gets or sets the start time of the step execution
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the end time of the step execution
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the duration of the step execution in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }
        
        /// <summary>
        /// Gets or sets any error message associated with the step
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the attempt number for this step
        /// </summary>
        public int AttemptNumber { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the ID of the agent that executed this step
        /// </summary>
        public string AgentId { get; set; }
    }
    
    /// <summary>
    /// States of a workflow
    /// </summary>
    public enum WorkflowState
    {
        /// <summary>
        /// Not started
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// Running
        /// </summary>
        Running,
        
        /// <summary>
        /// Paused
        /// </summary>
        Paused,
        
        /// <summary>
        /// Completed successfully
        /// </summary>
        Completed,
        
        /// <summary>
        /// Failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// Timed out
        /// </summary>
        TimedOut
    }
    
    /// <summary>
    /// States of a workflow step
    /// </summary>
    public enum StepState
    {
        /// <summary>
        /// Not started
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// Waiting for dependencies
        /// </summary>
        Waiting,
        
        /// <summary>
        /// Running
        /// </summary>
        Running,
        
        /// <summary>
        /// Completed successfully
        /// </summary>
        Completed,
        
        /// <summary>
        /// Failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// Skipped
        /// </summary>
        Skipped,
        
        /// <summary>
        /// Timed out
        /// </summary>
        TimedOut
    }
}
