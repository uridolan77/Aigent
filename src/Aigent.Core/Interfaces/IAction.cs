using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Interface for actions that can be performed by agents
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Gets the unique identifier for the action
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Gets the type of the action
        /// </summary>
        string Type { get; }
        
        /// <summary>
        /// Gets the parameters for the action
        /// </summary>
        Dictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// Gets the description of the action
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Gets the timestamp when the action was created
        /// </summary>
        DateTime CreatedAt { get; }
        
        /// <summary>
        /// Gets the priority of the action
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Executes the action
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <returns>Result of the action</returns>
        Task<Models.ActionResult> Execute(EnvironmentState state);
        
        /// <summary>
        /// Validates the action
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <returns>True if the action is valid, false otherwise</returns>
        bool Validate(EnvironmentState state);
    }
}
