using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Generic action implementation
    /// </summary>
    public class GenericAction : IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string ActionType { get; }
        
        /// <summary>
        /// Parameters of the action
        /// </summary>
        public Dictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// Initializes a new instance of the GenericAction class
        /// </summary>
        /// <param name="actionType">Type of the action</param>
        /// <param name="parameters">Parameters of the action</param>
        public GenericAction(string actionType, Dictionary<string, object> parameters)
        {
            ActionType = actionType;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        public Task<ActionResult> Execute()
        {
            return Task.FromResult(ActionResult.Successful($"Generic action {ActionType} executed", Parameters));
        }
    }
}
