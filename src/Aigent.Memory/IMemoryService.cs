using System;
using System.Threading.Tasks;

namespace Aigent.Memory
{
    /// <summary>
    /// Interface for agent memory services
    /// </summary>
    public interface IMemoryService
    {
        /// <summary>
        /// Initializes the memory service for a specific agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        Task Initialize(string agentId);
        
        /// <summary>
        /// Stores a value in the agent's context
        /// </summary>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="ttl">Optional time-to-live for the value</param>
        Task StoreContext(string key, object value, TimeSpan? ttl = null);
        
        /// <summary>
        /// Retrieves a value from the agent's context
        /// </summary>
        /// <typeparam name="T">Type of the value to retrieve</typeparam>
        /// <param name="key">Key the value is stored under</param>
        /// <returns>The retrieved value, or default if not found</returns>
        Task<T> RetrieveContext<T>(string key);
        
        /// <summary>
        /// Clears all memory for the agent
        /// </summary>
        Task ClearMemory();
        
        /// <summary>
        /// Flushes any pending changes to persistent storage
        /// </summary>
        Task Flush();
    }

    /// <summary>
    /// Interface for short-term memory services
    /// </summary>
    public interface IShortTermMemory : IMemoryService { }

    /// <summary>
    /// Interface for long-term memory services
    /// </summary>
    public interface ILongTermMemory : IMemoryService { }
}
