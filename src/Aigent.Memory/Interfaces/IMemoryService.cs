using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;

namespace Aigent.Memory.Interfaces
{
    /// <summary>
    /// Interface for memory services that provide agent memory capabilities
    /// </summary>
    public interface IMemoryService : Core.Interfaces.IMemoryService
    {
        /// <summary>
        /// Gets the memory provider used by this service
        /// </summary>
        IMemoryProvider MemoryProvider { get; }
        
        /// <summary>
        /// Gets the agent ID associated with this memory service
        /// </summary>
        string AgentId { get; }
        
        /// <summary>
        /// Stores a value with a hierarchical key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="section">Section of the memory</param>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreSectionAsync<T>(string section, string key, T value, TimeSpan? expirationTime = null);
        
        /// <summary>
        /// Retrieves a value with a hierarchical key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="section">Section of the memory</param>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        Task<T> RetrieveSectionAsync<T>(string section, string key);
        
        /// <summary>
        /// Gets all keys in a memory section
        /// </summary>
        /// <param name="section">Section of the memory</param>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        Task<IEnumerable<string>> GetSectionKeysAsync(string section, string pattern = "*");
        
        /// <summary>
        /// Clears a specific section of memory
        /// </summary>
        /// <param name="section">Section of the memory to clear</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ClearSectionAsync(string section);
        
        /// <summary>
        /// Stores agent state in memory
        /// </summary>
        /// <param name="state">The agent state to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreStateAsync(object state, TimeSpan? expirationTime = null);
        
        /// <summary>
        /// Retrieves agent state from memory
        /// </summary>
        /// <typeparam name="T">Type of the state</typeparam>
        /// <returns>The agent state, or default if not found</returns>
        Task<T> RetrieveStateAsync<T>();
    }
}
