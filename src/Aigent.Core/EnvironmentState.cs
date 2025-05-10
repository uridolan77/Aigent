using System;
using System.Collections.Generic;

namespace Aigent.Core
{
    /// <summary>
    /// Represents the state of the environment
    /// </summary>
    public class EnvironmentState
    {
        /// <summary>
        /// Properties of the state
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Timestamp of the state
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
