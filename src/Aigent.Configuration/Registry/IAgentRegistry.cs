using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core;

namespace Aigent.Configuration.Registry
{
    /// <summary>
    /// Interface for agent registry services
    /// </summary>
    public interface IAgentRegistry
    {
        /// <summary>
        /// Registers an agent
        /// </summary>
        /// <param name="agent">Agent to register</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RegisterAgent(IAgent agent);
        
        /// <summary>
        /// Unregisters an agent
        /// </summary>
        /// <param name="agentId">ID of the agent to unregister</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task UnregisterAgent(string agentId);
        
        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>The agent if found, null otherwise</returns>
        Task<IAgent> GetAgent(string agentId);
        
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
        Task<List<IAgent>> GetAgents(
            string name = null,
            string type = null,
            string status = null,
            int page = 1,
            int pageSize = 10,
            string sortBy = null,
            string sortDirection = "asc");
    }
}
