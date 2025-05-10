using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for actions that an agent can perform
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        string ActionType { get; }

        /// <summary>
        /// Parameters of the action
        /// </summary>
        Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// Priority of the action (higher values indicate higher priority)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Estimated cost of performing the action
        /// </summary>
        double EstimatedCost { get; }

        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        Task<ActionResult> Execute();
    }

    /// <summary>
    /// Result of an action
    /// </summary>
    public class ActionResult
    {
        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Additional data from the action
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Time taken to execute the action
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Creates a successful action result
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="data">Additional data</param>
        /// <param name="executionTime">Time taken to execute the action</param>
        /// <returns>Successful action result</returns>
        public static ActionResult Successful(string message, Dictionary<string, object> data = null, TimeSpan? executionTime = null)
        {
            return new ActionResult
            {
                Success = true,
                Message = message,
                Data = data ?? new Dictionary<string, object>(),
                ExecutionTime = executionTime ?? TimeSpan.Zero
            };
        }

        /// <summary>
        /// Creates a failed action result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Additional data</param>
        /// <param name="executionTime">Time taken to execute the action</param>
        /// <returns>Failed action result</returns>
        public static ActionResult Failed(string message, Dictionary<string, object> data = null, TimeSpan? executionTime = null)
        {
            return new ActionResult
            {
                Success = false,
                Message = message,
                Data = data ?? new Dictionary<string, object>(),
                ExecutionTime = executionTime ?? TimeSpan.Zero
            };
        }
    }
}
