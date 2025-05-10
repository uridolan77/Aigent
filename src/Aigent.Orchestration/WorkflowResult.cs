using System.Collections.Generic;
using Aigent.Core;

namespace Aigent.Orchestration
{
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
        /// Message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Results of individual steps
        /// </summary>
        public Dictionary<string, ActionResult> Results { get; set; } = new Dictionary<string, ActionResult>();
        
        /// <summary>
        /// Creates a successful workflow result
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="results">Step results</param>
        /// <returns>Successful workflow result</returns>
        public static WorkflowResult Successful(string message, Dictionary<string, ActionResult> results = null)
        {
            return new WorkflowResult
            {
                Success = true,
                Message = message,
                Results = results ?? new Dictionary<string, ActionResult>()
            };
        }
        
        /// <summary>
        /// Creates a failed workflow result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="results">Step results</param>
        /// <returns>Failed workflow result</returns>
        public static WorkflowResult Failed(string message, Dictionary<string, ActionResult> results = null)
        {
            return new WorkflowResult
            {
                Success = false,
                Message = message,
                Results = results ?? new Dictionary<string, ActionResult>()
            };
        }
    }
}
