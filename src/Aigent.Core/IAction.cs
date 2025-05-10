using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for actions
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
        /// Creates a successful action result
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="data">Additional data</param>
        /// <returns>Successful action result</returns>
        public static ActionResult Successful(string message, Dictionary<string, object> data = null)
        {
            return new ActionResult
            {
                Success = true,
                Message = message,
                Data = data ?? new Dictionary<string, object>()
            };
        }
        
        /// <summary>
        /// Creates a failed action result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Additional data</param>
        /// <returns>Failed action result</returns>
        public static ActionResult Failed(string message, Dictionary<string, object> data = null)
        {
            return new ActionResult
            {
                Success = false,
                Message = message,
                Data = data ?? new Dictionary<string, object>()
            };
        }
    }
}
