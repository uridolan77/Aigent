using System.Collections.Generic;

namespace Aigent.Core
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
        /// Rules for the agent
        /// </summary>
        public Dictionary<string, IConfigurationSection> Rules { get; set; }

        /// <summary>
        /// Settings for the agent
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }
}
