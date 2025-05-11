using System.Collections.Generic;

namespace Aigent.Core.Models
{
    /// <summary>
    /// Represents the capabilities of an agent
    /// </summary>
    public class AgentCapabilities
    {
        /// <summary>
        /// Initializes a new instance of the AgentCapabilities class
        /// </summary>
        public AgentCapabilities()
        {
            SupportedActionTypes = new List<string>();
            SkillLevels = new Dictionary<string, double>();
        }
        
        /// <summary>
        /// Types of actions this agent can perform
        /// </summary>
        public List<string> SupportedActionTypes { get; set; }

        /// <summary>
        /// Skill levels for different domains (0.0 to 1.0)
        /// </summary>
        public Dictionary<string, double> SkillLevels { get; set; }

        /// <summary>
        /// Current load factor of the agent (0.0 to 1.0)
        /// </summary>
        public double LoadFactor { get; set; }

        /// <summary>
        /// Historical performance metric (0.0 to 1.0)
        /// </summary>
        public double HistoricalPerformance { get; set; }
        
        /// <summary>
        /// Maximum concurrent tasks the agent can handle
        /// </summary>
        public int MaxConcurrentTasks { get; set; } = 1;
        
        /// <summary>
        /// Memory capacity in megabytes
        /// </summary>
        public int MemoryCapacity { get; set; } = 1024;
        
        /// <summary>
        /// Accuracy level of the agent (0.0 to 1.0)
        /// </summary>
        public double Accuracy { get; set; } = 0.8;
        
        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public int ResponseTime { get; set; } = 1000;
    }
}
