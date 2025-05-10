using System;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ILogger = Aigent.Monitoring.ILogger;
using LogLevel = Aigent.Monitoring.LogLevel;

namespace Aigent.Api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time agent communication
    /// </summary>
    [Authorize]
    public class AgentHub : Hub
    {
        private readonly IAgentRegistry _agentRegistry;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the AgentHub class
        /// </summary>
        /// <param name="agentRegistry">Agent registry</param>
        /// <param name="logger">Logger</param>
        public AgentHub(IAgentRegistry agentRegistry, ILogger logger)
        {
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends a message to an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="message">Message to send</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SendMessageToAgent(string agentId, string message)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message cannot be null or empty", nameof(message));
            }

            var agent = await _agentRegistry.GetAgent(agentId);

            if (agent == null)
            {
                _logger.Log(LogLevel.Warning, $"Agent not found: {agentId}");
                throw new InvalidOperationException($"Agent not found: {agentId}");
            }

            _logger.Log(LogLevel.Information, $"Message sent to agent {agentId}: {message}");

            // Create an environment state with the message
            var state = new EnvironmentState();
            state.Properties["input"] = message;
            state.Properties["source"] = "hub";
            state.Properties["connectionId"] = Context.ConnectionId;

            // Let the agent decide what to do with the message
            var action = await agent.DecideAction(state);
            var result = await action.Execute();

            // Send the result back to the client
            await Clients.Caller.SendAsync("ReceiveAgentResponse", agentId, result.Success, result.Message, result.Data);
        }

        /// <summary>
        /// Subscribes to agent events
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SubscribeToAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            }

            var agent = await _agentRegistry.GetAgent(agentId);

            if (agent == null)
            {
                _logger.Log(LogLevel.Warning, $"Agent not found: {agentId}");
                throw new InvalidOperationException($"Agent not found: {agentId}");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"agent-{agentId}");
            _logger.Log(LogLevel.Information, $"Client {Context.ConnectionId} subscribed to agent {agentId}");
        }

        /// <summary>
        /// Unsubscribes from agent events
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UnsubscribeFromAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent-{agentId}");
            _logger.Log(LogLevel.Information, $"Client {Context.ConnectionId} unsubscribed from agent {agentId}");
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public override Task OnConnectedAsync()
        {
            _logger.Log(LogLevel.Information, $"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        /// <param name="exception">Exception that caused the disconnect</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.Log(LogLevel.Information, $"Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
