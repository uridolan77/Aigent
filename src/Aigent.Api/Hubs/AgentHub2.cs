using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Aigent.Api.Models;
using Aigent.Monitoring;

namespace Aigent.Api.Hubs
{
    /// <summary>
    /// SignalR hub for agent events
    /// </summary>
    [Authorize]
    public class AgentHub : Hub
    {
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private static readonly Dictionary<string, HashSet<string>> _agentSubscriptions = new();

        /// <summary>
        /// Initializes a new instance of the AgentHub class
        /// </summary>
        /// <param name="logger">Logger for recording hub activities</param>
        /// <param name="metrics">Metrics collector for monitoring hub performance</param>
        public AgentHub(ILogger logger, IMetricsCollector metrics = null)
        {
            _logger = logger;
            _metrics = metrics;
        }

        /// <summary>
        /// Subscribes to agent events
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        public async Task SubscribeToAgent(string agentId)
        {
            var connectionId = Context.ConnectionId;
            var userId = Context.UserIdentifier;

            _logger.Log(LogLevel.Information, $"User {userId} subscribing to agent {agentId}");
            
            lock (_agentSubscriptions)
            {
                if (!_agentSubscriptions.TryGetValue(agentId, out var connections))
                {
                    connections = new HashSet<string>();
                    _agentSubscriptions[agentId] = connections;
                }
                
                connections.Add(connectionId);
            }
            
            await Groups.AddToGroupAsync(connectionId, $"agent-{agentId}");
            _metrics?.RecordMetric("signalr.agent_subscriptions", 1.0);
            
            await Clients.Caller.SendAsync("SubscriptionConfirmed", agentId);
        }

        /// <summary>
        /// Unsubscribes from agent events
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        public async Task UnsubscribeFromAgent(string agentId)
        {
            var connectionId = Context.ConnectionId;
            var userId = Context.UserIdentifier;

            _logger.Log(LogLevel.Information, $"User {userId} unsubscribing from agent {agentId}");
            
            lock (_agentSubscriptions)
            {
                if (_agentSubscriptions.TryGetValue(agentId, out var connections))
                {
                    connections.Remove(connectionId);
                    
                    if (connections.Count == 0)
                    {
                        _agentSubscriptions.Remove(agentId);
                    }
                }
            }
            
            await Groups.RemoveFromGroupAsync(connectionId, $"agent-{agentId}");
            _metrics?.RecordMetric("signalr.agent_unsubscriptions", 1.0);
            
            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", agentId);
        }

        /// <summary>
        /// Handles client disconnection
        /// </summary>
        /// <param name="exception">Exception that caused the disconnection</param>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            
            // Remove connection from all agent subscriptions
            lock (_agentSubscriptions)
            {
                foreach (var agentId in _agentSubscriptions.Keys)
                {
                    var connections = _agentSubscriptions[agentId];
                    connections.Remove(connectionId);
                    
                    if (connections.Count == 0)
                    {
                        _agentSubscriptions.Remove(agentId);
                    }
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Agent event service for sending events to clients
    /// </summary>
    public interface IAgentEventService
    {
        /// <summary>
        /// Sends an agent status update
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="status">New status</param>
        Task SendAgentStatusUpdateAsync(string agentId, string status);
        
        /// <summary>
        /// Sends an agent action event
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="action">Action performed</param>
        /// <param name="result">Result of the action</param>
        Task SendAgentActionEventAsync(string agentId, string action, ActionResultDto result);
    }

    /// <summary>
    /// Implementation of IAgentEventService
    /// </summary>
    public class AgentEventService : IAgentEventService
    {
        private readonly IHubContext<AgentHub> _hubContext;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the AgentEventService class
        /// </summary>
        /// <param name="hubContext">Hub context</param>
        /// <param name="logger">Logger for recording event activities</param>
        public AgentEventService(IHubContext<AgentHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Sends an agent status update
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="status">New status</param>
        public async Task SendAgentStatusUpdateAsync(string agentId, string status)
        {
            _logger.Log(LogLevel.Debug, $"Sending status update for agent {agentId}: {status}");
            
            await _hubContext.Clients.Group($"agent-{agentId}").SendAsync("AgentStatusUpdate", agentId, status);
        }

        /// <summary>
        /// Sends an agent action event
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="action">Action performed</param>
        /// <param name="result">Result of the action</param>
        public async Task SendAgentActionEventAsync(string agentId, string action, ActionResultDto result)
        {
            _logger.Log(LogLevel.Debug, $"Sending action event for agent {agentId}: {action}");
            
            await _hubContext.Clients.Group($"agent-{agentId}").SendAsync("AgentActionEvent", agentId, action, result);
        }
    }
}
