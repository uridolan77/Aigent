using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Agent status
    /// </summary>
    public enum AgentStatus
    {
        /// <summary>
        /// Agent is initializing
        /// </summary>
        Initializing,

        /// <summary>
        /// Agent is ready
        /// </summary>
        Ready,

        /// <summary>
        /// Agent is busy
        /// </summary>
        Busy,

        /// <summary>
        /// Agent is shutting down
        /// </summary>
        ShuttingDown,

        /// <summary>
        /// Agent is in error state
        /// </summary>
        Error
    }

    /// <summary>
    /// Represents the core agent interface with lifecycle management
    /// </summary>
    public interface IAgent : IDisposable
    {
        /// <summary>
        /// ID of the agent
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name of the agent
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Type of the agent
        /// </summary>
        AgentType Type { get; }

        /// <summary>
        /// Status of the agent
        /// </summary>
        AgentStatus Status { get; }

        /// <summary>
        /// Capabilities of the agent
        /// </summary>
        AgentCapabilities Capabilities { get; }

        /// <summary>
        /// Initializes the agent and its resources
        /// </summary>
        Task Initialize();

        /// <summary>
        /// Decides on an action based on the current environment state
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <returns>The action to be performed</returns>
        Task<IAction> DecideAction(EnvironmentState state);

        /// <summary>
        /// Learns from the result of an action
        /// </summary>
        /// <param name="state">State when the action was performed</param>
        /// <param name="action">The action that was performed</param>
        /// <param name="result">The result of the action</param>
        Task Learn(EnvironmentState state, IAction action, ActionResult result);

        /// <summary>
        /// Shuts down the agent and releases its resources
        /// </summary>
        Task Shutdown();
    }


    /// <summary>
    /// Represents the capabilities of an agent
    /// </summary>
    public class AgentCapabilities
    {
        /// <summary>
        /// Types of actions this agent can perform
        /// </summary>
        public List<string> SupportedActionTypes { get; set; } = new List<string>();

        /// <summary>
        /// Skill levels for different domains (0.0 to 1.0)
        /// </summary>
        public Dictionary<string, double> SkillLevels { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Current load factor of the agent (0.0 to 1.0)
        /// </summary>
        public double LoadFactor { get; set; }

        /// <summary>
        /// Historical performance metric (0.0 to 1.0)
        /// </summary>
        public double HistoricalPerformance { get; set; }
    }
}
