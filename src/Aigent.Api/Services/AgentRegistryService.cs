using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Configuration;
using Aigent.Memory;
using Aigent.Monitoring;
using Aigent.Api.Models;
using Aigent.Api.Interfaces;

namespace Aigent.Api.Services
{
    /// <summary>
    /// Implementation of IAgentRegistry
    /// </summary>
    public class AgentRegistryService : IAgentRegistry
    {
        private readonly ConcurrentDictionary<string, IAgent> _agents = new();
        private readonly IAgentBuilder _agentBuilder;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private readonly IAgentEventService _eventService;

        /// <summary>
        /// Initializes a new instance of the AgentRegistryService class
        /// </summary>
        /// <param name="agentBuilder">Agent builder</param>
        /// <param name="logger">Logger for recording registry activities</param>
        /// <param name="metrics">Metrics collector for monitoring registry performance</param>
        /// <param name="eventService">Agent event service</param>
        public AgentRegistryService(
            IAgentBuilder agentBuilder,
            ILogger logger,
            IMetricsCollector metrics = null,
            IAgentEventService eventService = null)
        {
            _agentBuilder = agentBuilder ?? throw new ArgumentNullException(nameof(agentBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics;
            _eventService = eventService;
        }

        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="request">Agent creation request</param>
        /// <returns>The created agent</returns>
        public async Task<AgentDto> CreateAgentAsync(CreateAgentRequest request)
        {
            _metrics?.StartOperation("agent_registry_create_agent");

            try
            {
                var config = new AgentConfiguration
                {
                    Name = request.Name,
                    Type = request.Type,
                    Settings = request.Settings ?? new Dictionary<string, object>()
                };

                var builder = _agentBuilder.WithConfiguration(config);

                // Set memory service based on request
                switch (request.MemoryServiceType?.ToLower())
                {
                    case "redis":
                        builder = builder.WithMemory<RedisMemoryService>();
                        break;
                    case "sql":
                        builder = builder.WithMemory<SqlMemoryService>();
                        break;
                    case "documentdb":
                        builder = builder.WithMemory<DocumentDbMemoryService>();
                        break;
                    default:
                        builder = builder.WithMemory<ConcurrentMemoryService>();
                        break;
                }

                var agent = builder.Build();
                await agent.Initialize();

                _agents[agent.Id] = agent;

                _logger.Log(LogLevel.Information, $"Created agent: {agent.Name} ({agent.Id})");
                _metrics?.RecordMetric("agent_registry.agent_count", _agents.Count);

                var agentDto = MapToDto(agent);

                // Send agent status update event
                _eventService?.SendAgentStatusUpdateAsync(agent.Id, agentDto.Status).ConfigureAwait(false);

                return agentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating agent: {ex.Message}", ex);
                _metrics?.RecordMetric("agent_registry.create_agent_error_count", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation("agent_registry_create_agent");
            }
        }

        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>The agent</returns>
        public Task<AgentDto> GetAgentAsync(string agentId)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                return Task.FromResult(MapToDto(agent));
            }

            return Task.FromResult<AgentDto>(null);
        }

        /// <summary>
        /// Gets all agents
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <returns>Paged list of agents</returns>
        public Task<PagedList<AgentDto>> GetAgentsAsync(AgentQueryParameters queryParameters)
        {
            var agents = _agents.Values.Select(MapToDto);

            // Apply filtering
            if (!string.IsNullOrEmpty(queryParameters.Name))
            {
                agents = agents.Where(a => a.Name.Contains(queryParameters.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(queryParameters.Type) && Enum.TryParse<AgentType>(queryParameters.Type, true, out var agentType))
            {
                agents = agents.Where(a => a.Type == agentType);
            }

            if (!string.IsNullOrEmpty(queryParameters.Status))
            {
                agents = agents.Where(a => a.Status.Equals(queryParameters.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(queryParameters.ActionType))
            {
                agents = agents.Where(a => a.Capabilities.SupportedActionTypes.Any(t =>
                    t.Equals(queryParameters.ActionType, StringComparison.OrdinalIgnoreCase)));
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(queryParameters.SortBy))
            {
                agents = ApplySorting(agents, queryParameters.SortBy, queryParameters.SortDirection);
            }

            // Apply pagination
            var pagedList = PagedList<AgentDto>.Create(
                agents,
                queryParameters.Page,
                queryParameters.PageSize);

            return Task.FromResult(pagedList);
        }

        private IEnumerable<AgentDto> ApplySorting(IEnumerable<AgentDto> agents, string sortBy, string sortDirection)
        {
            var isAscending = string.IsNullOrEmpty(sortDirection) || sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "name" => isAscending ? agents.OrderBy(a => a.Name) : agents.OrderByDescending(a => a.Name),
                "type" => isAscending ? agents.OrderBy(a => a.Type) : agents.OrderByDescending(a => a.Type),
                "status" => isAscending ? agents.OrderBy(a => a.Status) : agents.OrderByDescending(a => a.Status),
                "loadfactor" => isAscending ? agents.OrderBy(a => a.Capabilities.LoadFactor) : agents.OrderByDescending(a => a.Capabilities.LoadFactor),
                "performance" => isAscending ? agents.OrderBy(a => a.Capabilities.HistoricalPerformance) : agents.OrderByDescending(a => a.Capabilities.HistoricalPerformance),
                _ => isAscending ? agents.OrderBy(a => a.Id) : agents.OrderByDescending(a => a.Id)
            };
        }

        /// <summary>
        /// Deletes an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Whether the agent was deleted</returns>
        public async Task<bool> DeleteAgentAsync(string agentId)
        {
            _metrics?.StartOperation("agent_registry_delete_agent");

            try
            {
                if (_agents.TryRemove(agentId, out var agent))
                {
                    await agent.Shutdown();
                    agent.Dispose();

                    _logger.Log(LogLevel.Information, $"Deleted agent: {agent.Name} ({agent.Id})");
                    _metrics?.RecordMetric("agent_registry.agent_count", _agents.Count);

                    return true;
                }

                return false;
            }
            finally
            {
                _metrics?.EndOperation("agent_registry_delete_agent");
            }
        }

        /// <summary>
        /// Performs an action with an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="request">Action request</param>
        /// <returns>Result of the action</returns>
        public async Task<AgentActionResponse> PerformActionAsync(string agentId, AgentActionRequest request)
        {
            _metrics?.StartOperation("agent_registry_perform_action");

            try
            {
                if (!_agents.TryGetValue(agentId, out var agent))
                {
                    throw new KeyNotFoundException($"Agent with ID {agentId} not found");
                }

                var state = new EnvironmentState
                {
                    Properties = new Dictionary<string, object>
                    {
                        ["input"] = request.Input
                    }
                };

                // Add additional parameters
                if (request.Parameters != null)
                {
                    foreach (var param in request.Parameters)
                    {
                        state.Properties[param.Key] = param.Value;
                    }
                }

                var action = await agent.DecideAction(state);
                var result = await action.Execute();

                // Learn from the result
                await agent.Learn(state, action, result);

                _logger.Log(LogLevel.Information, $"Agent {agent.Name} performed action: {action.ActionType}");
                _metrics?.RecordMetric("agent_registry.action_count", 1.0);

                var actionResultDto = new ActionResultDto
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                };

                var response = new AgentActionResponse
                {
                    ActionType = action.ActionType,
                    Result = actionResultDto
                };

                // Send agent action event
                _eventService?.SendAgentActionEventAsync(agentId, action.ActionType, actionResultDto).ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error performing action: {ex.Message}", ex);
                _metrics?.RecordMetric("agent_registry.perform_action_error_count", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation("agent_registry_perform_action");
            }
        }

        private AgentDto MapToDto(IAgent agent)
        {
            return new AgentDto
            {
                Id = agent.Id,
                Name = agent.Name,
                Type = agent.Type,
                Capabilities = new AgentCapabilitiesDto
                {
                    SupportedActionTypes = agent.Capabilities.SupportedActionTypes,
                    SkillLevels = agent.Capabilities.SkillLevels,
                    LoadFactor = agent.Capabilities.LoadFactor,
                    HistoricalPerformance = agent.Capabilities.HistoricalPerformance
                },
                Status = "Active" // In a real implementation, this would be determined by the agent's state
            };
        }
    }
}
