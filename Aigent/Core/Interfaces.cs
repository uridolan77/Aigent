using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Defines the type of agent based on its decision-making approach
    /// </summary>
    public enum AgentType
    {
        /// <summary>
        /// Reactive agents respond directly to stimuli without planning
        /// </summary>
        Reactive,
        
        /// <summary>
        /// Deliberative agents plan and reason before acting
        /// </summary>
        Deliberative,
        
        /// <summary>
        /// Hybrid agents combine reactive and deliberative approaches
        /// </summary>
        Hybrid,
        
        /// <summary>
        /// Learning agents improve over time through experience
        /// </summary>
        Learning,
        
        /// <summary>
        /// Utility-based agents select actions based on utility functions
        /// </summary>
        UtilityBased,
        
        /// <summary>
        /// BDI (Belief-Desire-Intention) agents model mental attitudes
        /// </summary>
        BDI
    }

    /// <summary>
    /// Represents the core agent interface with lifecycle management
    /// </summary>
    public interface IAgent : IDisposable
    {
        /// <summary>
        /// Unique identifier for the agent
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Human-readable name of the agent
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Type of agent based on its decision-making approach
        /// </summary>
        AgentType Type { get; }
        
        /// <summary>
        /// Agent capabilities including supported actions and skill levels
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
        /// Shuts down the agent and releases resources
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
        public List<string> SupportedActionTypes { get; set; } = new();
        
        /// <summary>
        /// Skill levels for different domains (0.0 to 1.0)
        /// </summary>
        public Dictionary<string, double> SkillLevels { get; set; } = new();
        
        /// <summary>
        /// Current load factor of the agent (0.0 to 1.0)
        /// </summary>
        public double LoadFactor { get; set; }
        
        /// <summary>
        /// Historical performance metric (0.0 to 1.0)
        /// </summary>
        public double HistoricalPerformance { get; set; }
    }

    /// <summary>
    /// Represents an action that an agent can perform
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        string ActionType { get; }
        
        /// <summary>
        /// Parameters for the action
        /// </summary>
        Dictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// Priority of the action (higher values indicate higher priority)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Estimated cost of performing the action
        /// </summary>
        double EstimatedCost { get; }
        
        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        Task<ActionResult> Execute();
    }

    /// <summary>
    /// Represents the result of an action
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
        /// Data produced by the action
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Time taken to execute the action
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Represents the state of the environment
    /// </summary>
    public class EnvironmentState
    {
        /// <summary>
        /// Properties of the environment
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
        
        /// <summary>
        /// Timestamp when the state was captured
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
