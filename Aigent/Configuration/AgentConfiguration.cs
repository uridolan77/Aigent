using System.Collections.Generic;
using Aigent.Core;

namespace Aigent.Configuration
{
    /// <summary>
    /// Configuration for an agent
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the agent
        /// </summary>
        public AgentType Type { get; set; }
        
        /// <summary>
        /// Additional settings for the agent
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}
