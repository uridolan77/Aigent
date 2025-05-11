using System.Threading.Tasks;
using Aigent.Api.Models;

namespace Aigent.Api.Interfaces
{
    /// <summary>
    /// Interface for agent registries
    /// </summary>
    public interface IAgentRegistry
    {
        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="request">Agent creation request</param>
        /// <returns>The created agent</returns>
        Task<AgentDto> CreateAgentAsync(CreateAgentRequest request);

        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>The agent</returns>
        Task<AgentDto> GetAgentAsync(string agentId);

        /// <summary>
        /// Gets all agents
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <returns>Paged list of agents</returns>
        Task<PagedList<AgentDto>> GetAgentsAsync(AgentQueryParameters queryParameters);

        /// <summary>
        /// Deletes an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Whether the agent was deleted</returns>
        Task<bool> DeleteAgentAsync(string agentId);

        /// <summary>
        /// Performs an action with an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="request">Action request</param>
        /// <returns>Result of the action</returns>
        Task<AgentActionResponse> PerformActionAsync(string agentId, AgentActionRequest request);
    }
}
