using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Represents the core agent interface with lifecycle management and extended functionality
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
        /// Configuration of the agent
        /// </summary>
        Models.AgentConfiguration Configuration { get; }
        
        /// <summary>
        /// Gets the agent's metadata
        /// </summary>
        Dictionary<string, object> Metadata { get; }

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
        /// Processes a command sent to the agent
        /// </summary>
        /// <param name="command">Command to process</param>
        /// <returns>Result of the command</returns>
        Task<CommandResult> ProcessCommand(AgentCommand command);

        /// <summary>
        /// Shuts down the agent and releases its resources
        /// </summary>
        Task Shutdown();
    }

    /// <summary>
    /// Result of a command processed by an agent
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Whether the command was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Additional data returned by the command
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
    
    /// <summary>
    /// Command to be processed by an agent
    /// </summary>
    public class AgentCommand
    {
        /// <summary>
        /// Name of the command
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Parameters for the command
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
