using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Monitoring;

namespace Aigent.Configuration
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
            
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(a => a.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
            }
            
            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                query = sortBy.ToLowerInvariant() switch
                {
                    "name" => sortDirection.ToLowerInvariant() == "desc" 
                        ? query.OrderByDescending(a => a.Name) 
                        : query.OrderBy(a => a.Name),
                    "type" => sortDirection.ToLowerInvariant() == "desc" 
                        ? query.OrderByDescending(a => a.Type) 
                        : query.OrderBy(a => a.Type),
                    "status" => sortDirection.ToLowerInvariant() == "desc" 
                        ? query.OrderByDescending(a => a.Status) 
                        : query.OrderBy(a => a.Status),
                    _ => query.OrderBy(a => a.Name)
                };
            }
            else
            {
                query = query.OrderBy(a => a.Name);
            }
            
            // Apply pagination
            var result = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Gets the total count of agents
        /// </summary>
        /// <param name="name">Optional name filter</param>
        /// <param name="type">Optional type filter</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>Total count of agents</returns>
        public Task<int> GetAgentCount(string name = null, string type = null, string status = null)
        {
            var query = _agents.Values.AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(a => a.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(a => a.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
            }
            
            return Task.FromResult(query.Count());
        }
        
        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="id">ID of the agent</param>
        /// <returns>The agent, or null if not found</returns>
        public Task<IAgent> GetAgent(string id)
        {
            _agents.TryGetValue(id, out var agent);
            return Task.FromResult(agent);
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
            
            if (string.IsNullOrEmpty(agent.Id))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agent));
            }
            
            if (_agents.ContainsKey(agent.Id))
            {
                throw new InvalidOperationException($"Agent with ID {agent.Id} already exists");
            }
            
            _agents[agent.Id] = agent;
            _logger.Log(LogLevel.Information, $"Registered agent: {agent.Name} ({agent.Id})");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Unregisters an agent
        /// </summary>
        /// <param name="id">ID of the agent to unregister</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UnregisterAgent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(id));
            }
            
            if (_agents.TryGetValue(id, out var agent))
            {
                await agent.Shutdown();
                _agents.Remove(id);
                _logger.Log(LogLevel.Information, $"Unregistered agent: {agent.Name} ({agent.Id})");
            }
        }
        
        /// <summary>
        /// Creates an agent from a configuration
        /// </summary>
        /// <param name="configuration">Agent configuration</param>
        /// <returns>The created agent</returns>
        public async Task<IAgent> CreateAgent(AgentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            var agent = _agentBuilder
                .WithConfiguration(configuration)
                .Build();
            
            await agent.Initialize();
            await RegisterAgent(agent);
            
            return agent;
        }
    }
}
