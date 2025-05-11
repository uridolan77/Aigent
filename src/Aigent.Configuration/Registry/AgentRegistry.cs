using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Configuration.Builders;
using Aigent.Core;
using Aigent.Monitoring.Logging;

namespace Aigent.Configuration.Registry
{
    /// <summary>
    /// Registry for managing agents
    /// </summary>
    public class AgentRegistry : IAgentRegistry
    {
        private readonly Dictionary<string, IAgent> _agents = new();
        private readonly IAgentBuilder _agentBuilder;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the AgentRegistry class
        /// </summary>
        /// <param name="agentBuilder">Agent builder</param>
        /// <param name="logger">Logger</param>
        public AgentRegistry(IAgentBuilder agentBuilder, ILogger logger)
        {
            _agentBuilder = agentBuilder ?? throw new ArgumentNullException(nameof(agentBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Registers an agent
        /// </summary>
        /// <param name="agent">Agent to register</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task RegisterAgent(IAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }
            
            var agentId = agent.Id;
            
            lock (_agents)
            {
                if (_agents.ContainsKey(agentId))
                {
                    throw new InvalidOperationException($"Agent with ID {agentId} is already registered");
                }
                
                _agents[agentId] = agent;
            }
            
            _logger.LogInformation($"Registered agent: {agent.Name} (ID: {agentId})");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Unregisters an agent
        /// </summary>
        /// <param name="agentId">ID of the agent to unregister</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task UnregisterAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            }
            
            IAgent agent = null;
            
            lock (_agents)
            {
                if (_agents.TryGetValue(agentId, out agent))
                {
                    _agents.Remove(agentId);
                }
            }
            
            if (agent != null)
            {
                _logger.LogInformation($"Unregistered agent: {agent.Name} (ID: {agentId})");
            }
            else
            {
                _logger.LogWarning($"Attempted to unregister non-existent agent: {agentId}");
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>The agent if found, null otherwise</returns>
        public Task<IAgent> GetAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            }
            
            lock (_agents)
            {
                _agents.TryGetValue(agentId, out var agent);
                return Task.FromResult(agent);
            }
        }
        
        /// <summary>
        /// Gets all registered agents
        /// </summary>
        /// <param name="name">Optional name filter</param>
        /// <param name="type">Optional type filter</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="sortBy">Sort by field</param>
        /// <param name="sortDirection">Sort direction</param>
        /// <returns>List of agents</returns>
        public Task<List<IAgent>> GetAgents(
            string name = null,
            string type = null,
            string status = null,
            int page = 1,
            int pageSize = 10,
            string sortBy = null,
            string sortDirection = "asc")
        {
            var query = _agents.Values.AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(a => a.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<AgentType>(type, true, out var agentType))
            {
                query = query.Where(a => a.Type == agentType);
            }
            
            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
                
                query = sortBy.ToLowerInvariant() switch
                {
                    "name" => isAscending 
                        ? query.OrderBy(a => a.Name) 
                        : query.OrderByDescending(a => a.Name),
                    "id" => isAscending 
                        ? query.OrderBy(a => a.Id) 
                        : query.OrderByDescending(a => a.Id),
                    "type" => isAscending 
                        ? query.OrderBy(a => a.Type) 
                        : query.OrderByDescending(a => a.Type),
                    _ => query
                };
            }
            
            // Apply pagination
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            page = Math.Max(1, Math.Min(page, totalPages));
            
            var pagedResults = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return Task.FromResult(pagedResults);
        }
    }
}
