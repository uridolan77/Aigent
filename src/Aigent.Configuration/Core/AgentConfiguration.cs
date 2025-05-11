using System.Collections.Generic;
using Aigent.Core;

namespace Aigent.Configuration.Core
{
    /// <summary>
    /// Configuration for an agent instance
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the AgentConfiguration class
        /// </summary>
        public AgentConfiguration()
        {
            Settings = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Initializes a new instance of the AgentConfiguration class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="type">Type of the agent</param>
        public AgentConfiguration(string name, AgentType type) : this()
        {
            Name = name;
            Type = type;
        }
        
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
        public Dictionary<string, object> Settings { get; set; }
        
        /// <summary>
        /// Description of the agent
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Version of the agent configuration
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// Whether the agent is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Tags for the agent
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
}
