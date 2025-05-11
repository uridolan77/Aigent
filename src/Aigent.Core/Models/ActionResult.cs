using System;
using System.Collections.Generic;

namespace Aigent.Core.Models
{
    /// <summary>
    /// Represents the result of an action performed by an agent
    /// </summary>
    public class ActionResult
    {
        /// <summary>
        /// Gets or sets whether the action was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets a message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the new state after the action
        /// </summary>
        public EnvironmentState NewState { get; set; }
        
        /// <summary>
        /// Gets or sets the reward for the action
        /// </summary>
        public double Reward { get; set; }
        
        /// <summary>
        /// Gets or sets additional data from the action
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the timestamp when the action was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
