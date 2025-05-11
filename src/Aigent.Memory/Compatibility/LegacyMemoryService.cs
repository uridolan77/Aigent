using System;
using System.Threading.Tasks;
using Aigent.Memory.Interfaces;

namespace Aigent.Memory.Compatibility
{
    /// <summary>
    /// Adapter for the legacy IMemoryService interface
    /// </summary>
    public class LegacyMemoryService : IMemoryService
    {
        private readonly Aigent.Memory.IMemoryService _originalService;
        
        /// <summary>
        /// Initializes a new instance of the LegacyMemoryService class
        /// </summary>
        /// <param name="originalService">The original memory service to adapt</param>
        public LegacyMemoryService(Aigent.Memory.IMemoryService originalService)
        {
            _originalService = originalService ?? throw new ArgumentNullException(nameof(originalService));
        }
        
        /// <summary>
        /// Gets the memory provider used by this service
        /// </summary>
        public IMemoryProvider MemoryProvider => _originalService.MemoryProvider;
        
        /// <summary>
        /// Gets the agent ID associated with this memory service
        /// </summary>
        public string AgentId => _originalService.AgentId;
        
        /// <summary>
        /// Initializes the memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task Initialize(string agentId)
        {
            return _originalService.Initialize(agentId);
        }
        
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task StoreAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            return _originalService.StoreAsync(key, value, expirationTime);
        }
        
        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        public Task<T> RetrieveAsync<T>(string key)
        {
            return _originalService.RetrieveAsync<T>(key);
        }
        
        /// <summary>
        /// Checks if a key exists in memory
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public Task<bool> ExistsAsync(string key)
        {
            return _originalService.ExistsAsync(key);
        }
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key for the value to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task RemoveAsync(string key)
        {
            return _originalService.RemoveAsync(key);
        }
        
        /// <summary>
        /// Gets all keys in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public Task<System.Collections.Generic.IEnumerable<string>> GetKeysAsync(string pattern = "*")
        {
            return _originalService.GetKeysAsync(pattern);
        }
        
        /// <summary>
        /// Gets all key-value pairs in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>Dictionary of key-value pairs</returns>
        public Task<System.Collections.Generic.IDictionary<string, object>> GetAllAsync(string pattern = "*")
        {
            return _originalService.GetAllAsync(pattern);
        }
        
        /// <summary>
        /// Clears all memory for the agent
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task ClearAsync()
        {
            return _originalService.ClearAsync();
        }
        
        /// <summary>
        /// Stores a value with a hierarchical key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="section">Section of the memory</param>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task StoreSectionAsync<T>(string section, string key, T value, TimeSpan? expirationTime = null)
        {
            return _originalService.StoreSectionAsync(section, key, value, expirationTime);
        }
        
        /// <summary>
        /// Retrieves a value with a hierarchical key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="section">Section of the memory</param>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        public Task<T> RetrieveSectionAsync<T>(string section, string key)
        {
            return _originalService.RetrieveSectionAsync<T>(section, key);
        }
        
        /// <summary>
        /// Gets all keys in a memory section
        /// </summary>
        /// <param name="section">Section of the memory</param>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public Task<System.Collections.Generic.IEnumerable<string>> GetSectionKeysAsync(string section, string pattern = "*")
        {
            return _originalService.GetSectionKeysAsync(section, pattern);
        }
        
        /// <summary>
        /// Clears a specific section of memory
        /// </summary>
        /// <param name="section">Section of the memory to clear</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task ClearSectionAsync(string section)
        {
            return _originalService.ClearSectionAsync(section);
        }
        
        /// <summary>
        /// Stores agent state in memory
        /// </summary>
        /// <param name="state">The agent state to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task StoreStateAsync(object state, TimeSpan? expirationTime = null)
        {
            return _originalService.StoreStateAsync(state, expirationTime);
        }
        
        /// <summary>
        /// Retrieves agent state from memory
        /// </summary>
        /// <typeparam name="T">Type of the state</typeparam>
        /// <returns>The agent state, or default if not found</returns>
        public Task<T> RetrieveStateAsync<T>()
        {
            return _originalService.RetrieveStateAsync<T>();
        }
        
        /// <summary>
        /// Stores a value in the agent's context (legacy method)
        /// </summary>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="ttl">Optional time-to-live for the value</param>
        public Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            return _originalService.StoreContext(key, value, ttl);
        }
        
        /// <summary>
        /// Retrieves a value from the agent's context (legacy method)
        /// </summary>
        /// <typeparam name="T">Type of the value to retrieve</typeparam>
        /// <param name="key">Key the value is stored under</param>
        /// <returns>The retrieved value, or default if not found</returns>
        public Task<T> RetrieveContext<T>(string key)
        {
            return _originalService.RetrieveContext<T>(key);
        }
        
        /// <summary>
        /// Clears all memory for the agent (legacy method)
        /// </summary>
        public Task ClearMemory()
        {
            return _originalService.ClearMemory();
        }
        
        /// <summary>
        /// Flushes any pending changes to persistent storage (legacy method)
        /// </summary>
        public Task Flush()
        {
            return _originalService.Flush();
        }
    }
}
