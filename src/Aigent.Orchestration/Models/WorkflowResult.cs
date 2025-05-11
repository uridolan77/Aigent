using System;
using System.Collections.Generic;
using Aigent.Core.Models;

namespace Aigent.Orchestration.Models
{
    /// <summary>
    /// Result of a workflow execution
    /// </summary>
    public class WorkflowResult
    {
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the workflow
        /// </summary>
        public string WorkflowName { get; set; }
        
        /// <summary>
        /// Gets or sets whether the workflow execution was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets the message associated with the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the results of individual steps
        /// </summary>
        public Dictionary<string, ActionResult> StepResults { get; set; } = new Dictionary<string, ActionResult>();
        
        /// <summary>
        /// Gets or sets the errors that occurred during workflow execution
        /// </summary>
        public List<WorkflowError> Errors { get; set; } = new List<WorkflowError>();
        
        /// <summary>
        /// Gets or sets the start time of the workflow execution
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the end time of the workflow execution
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the duration of the workflow execution in milliseconds
        /// </summary>
        public long DurationMs { get; set; }
        
        /// <summary>
        /// Gets or sets the output data of the workflow
        /// </summary>
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the metadata associated with the workflow result
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a successful workflow result
        /// </summary>
        /// <param name="workflowId">ID of the workflow</param>
        /// <param name="workflowName">Name of the workflow</param>
        /// <param name="message">Success message</param>
        /// <param name="stepResults">Results of individual steps</param>
        /// <returns>A successful workflow result</returns>
        public static WorkflowResult Successful(string workflowId, string workflowName, string message, Dictionary<string, ActionResult> stepResults = null)
        {
            return new WorkflowResult
            {
                WorkflowId = workflowId,
                WorkflowName = workflowName,
                Success = true,
                Message = message,
                StepResults = stepResults ?? new Dictionary<string, ActionResult>(),
                StartTime = DateTime.UtcNow.AddSeconds(-1), // Placeholder, should be set externally
                EndTime = DateTime.UtcNow // Placeholder, should be set externally
            };
        }
        
        /// <summary>
        /// Creates a failed workflow result
        /// </summary>
        /// <param name="workflowId">ID of the workflow</param>
        /// <param name="workflowName">Name of the workflow</param>
        /// <param name="message">Error message</param>
        /// <param name="errors">List of errors</param>
        /// <param name="stepResults">Results of individual steps</param>
        /// <returns>A failed workflow result</returns>
        public static WorkflowResult Failed(string workflowId, string workflowName, string message, List<WorkflowError> errors = null, Dictionary<string, ActionResult> stepResults = null)
        {
            return new WorkflowResult
            {
                WorkflowId = workflowId,
                WorkflowName = workflowName,
                Success = false,
                Message = message,
                Errors = errors ?? new List<WorkflowError>(),
                StepResults = stepResults ?? new Dictionary<string, ActionResult>(),
                StartTime = DateTime.UtcNow.AddSeconds(-1), // Placeholder, should be set externally
                EndTime = DateTime.UtcNow // Placeholder, should be set externally
            };
        }
    }
    
    /// <summary>
    /// Error that occurred during workflow execution
    /// </summary>
    public class WorkflowError
    {
        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the step where the error occurred
        /// </summary>
        public string StepId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the step where the error occurred
        /// </summary>
        public string StepName { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the severity of the error
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
        
        /// <summary>
        /// Gets or sets additional details about the error
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Severity levels for workflow errors
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Warning, does not cause workflow failure
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error, causes step failure but may not cause workflow failure
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical error, causes workflow failure
        /// </summary>
        Critical
    }
}
