using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Memory.Interfaces;
using Aigent.Monitoring;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// Base implementation of IMemoryService that adapts a memory provider
    /// </summary>
    public abstract class BaseMemoryService : Interfaces.IMemoryService
    {
        private const string STATE_KEY = "_agent_state";
        
        /// <summary>
        /// Gets the memory provider used by this service
        /// </summary>
        public IMemoryProvider MemoryProvider { get; }
        
        /// <summary>
        /// Gets the agent ID associated with this memory service
        /// </summary>
        public string AgentId { get; }
        
        /// <summary>
        /// Gets the logger used by this memory service
        /// </summary>
        protected ILogger Logger { get; }
        
        /// <summary>
        /// Gets the metrics collector used by this memory service
        /// </summary>
        protected IMetricsCollector Metrics { get; }
        
        /// <summary>
        /// Initializes a new instance of the BaseMemoryService class
        /// </summary>
        /// <param name="provider">Memory provider</param>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        protected BaseMemoryService(
            IMemoryProvider provider, 
            string agentId, 
            ILogger logger = null, 
            IMetricsCollector metrics = null)
        {
            MemoryProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            Logger = logger;
            Metrics = metrics;
        }
        
        /// <summary>
        /// Initializes the memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task Initialize(string agentId)
        {
            // This is kept for compatibility with Core.Interfaces.IMemoryService
            // In this implementation, the agent ID is provided in the constructor
            Logger?.Debug($"Initializing memory service for agent {agentId} (note: preferred to use constructor initialization)");
            await MemoryProvider.InitializeAsync();
        }
        
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task StoreAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var prefixedKey = GetPrefixedKey(key);
            Logger?.Debug($"Storing value with key {prefixedKey}");
            Metrics?.Increment("memory.store.count");
            
            var startTime = DateTime.UtcNow;
            await MemoryProvider.StoreAsync(prefixedKey, value, expirationTime);
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            Metrics?.Histogram("memory.store.duration", elapsedMs);
            Logger?.Debug($"Stored value with key {prefixedKey} in {elapsedMs}ms");
        }
        
        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        public virtual async Task<T> RetrieveAsync<T>(string key)
        {
            var prefixedKey = GetPrefixedKey(key);
            Logger?.Debug($"Retrieving value with key {prefixedKey}");
            Metrics?.Increment("memory.retrieve.count");
            
            var startTime = DateTime.UtcNow;
            var result = await MemoryProvider.RetrieveAsync<T>(prefixedKey);
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            Metrics?.Histogram("memory.retrieve.duration", elapsedMs);
            Logger?.Debug($"Retrieved value with key {prefixedKey} in {elapsedMs}ms");
            
            return result;
        }
        
        /// <summary>
        /// Checks if a key exists in memory
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public virtual async Task<bool> ExistsAsync(string key)
        {
            var prefixedKey = GetPrefixedKey(key);
            return await MemoryProvider.ExistsAsync(prefixedKey);
        }
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key for the value to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task RemoveAsync(string key)
        {
            var prefixedKey = GetPrefixedKey(key);
            await MemoryProvider.RemoveAsync(prefixedKey);
        }
        
        /// <summary>
        /// Gets all keys in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public virtual async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
        {
            var prefixedPattern = GetPrefixedKey(pattern);
            var keys = await MemoryProvider.GetKeysAsync(prefixedPattern);
            return RemovePrefixFromKeys(keys);
        }
        
        /// <summary>
        /// Gets all key-value pairs in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>Dictionary of key-value pairs</returns>
        public virtual async Task<IDictionary<string, object>> GetAllAsync(string pattern = "*")
        {
            var prefixedPattern = GetPrefixedKey(pattern);
            var values = await MemoryProvider.GetAllAsync(prefixedPattern);
            return RemovePrefixFromDictionary(values);
        }
        
        /// <summary>
        /// Clears all memory for the agent
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task ClearAsync()
        {
            await MemoryProvider.ClearAsync();
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
        public virtual async Task StoreSectionAsync<T>(string section, string key, T value, TimeSpan? expirationTime = null)
        {
            var sectionKey = $"{section}:{key}";
            await StoreAsync(sectionKey, value, expirationTime);
        }
        
        /// <summary>
        /// Retrieves a value with a hierarchical key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="section">Section of the memory</param>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        public virtual async Task<T> RetrieveSectionAsync<T>(string section, string key)
        {
            var sectionKey = $"{section}:{key}";
            return await RetrieveAsync<T>(sectionKey);
        }
        
        /// <summary>
        /// Gets all keys in a memory section
        /// </summary>
        /// <param name="section">Section of the memory</param>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public virtual async Task<IEnumerable<string>> GetSectionKeysAsync(string section, string pattern = "*")
        {
            var sectionPattern = $"{section}:{pattern}";
            var keys = await GetKeysAsync(sectionPattern);
            return RemoveSectionPrefixFromKeys(keys, section);
        }
        
        /// <summary>
        /// Clears a specific section of memory
        /// </summary>
        /// <param name="section">Section of the memory to clear</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task ClearSectionAsync(string section)
        {
            var keys = await GetKeysAsync($"{section}:*");
            foreach (var key in keys)
            {
                await RemoveAsync($"{section}:{key}");
            }
        }
        
        /// <summary>
        /// Stores agent state in memory
        /// </summary>
        /// <param name="state">The agent state to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task StoreStateAsync(object state, TimeSpan? expirationTime = null)
        {
            await StoreAsync(STATE_KEY, state, expirationTime);
        }
        
        /// <summary>
        /// Retrieves agent state from memory
        /// </summary>
        /// <typeparam name="T">Type of the state</typeparam>
        /// <returns>The agent state, or default if not found</returns>
        public virtual async Task<T> RetrieveStateAsync<T>()
        {
            return await RetrieveAsync<T>(STATE_KEY);
        }
        
        // Core.Interfaces.IMemoryService compatibility methods
        
        /// <summary>
        /// Stores a value in the agent's context (legacy method)
        /// </summary>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="ttl">Optional time-to-live for the value</param>
        public virtual async Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            await StoreAsync(key, value, ttl);
        }
        
        /// <summary>
        /// Retrieves a value from the agent's context (legacy method)
        /// </summary>
        /// <typeparam name="T">Type of the value to retrieve</typeparam>
        /// <param name="key">Key the value is stored under</param>
        /// <returns>The retrieved value, or default if not found</returns>
        public virtual async Task<T> RetrieveContext<T>(string key)
        {
            return await RetrieveAsync<T>(key);
        }
        
        /// <summary>
        /// Clears all memory for the agent (legacy method)
        /// </summary>
        public virtual async Task ClearMemory()
        {
            await ClearAsync();
        }
        
        /// <summary>
        /// Flushes any pending changes to persistent storage (legacy method)
        /// </summary>
        public virtual Task Flush()
        {
            // Base implementation does nothing, override in persistent storage implementations
            return Task.CompletedTask;
        }
        
        // Helper methods
        
        /// <summary>
        /// Gets a key with the agent ID prefix
        /// </summary>
        /// <param name="key">Original key</param>
        /// <returns>Key with agent ID prefix</returns>
        protected virtual string GetPrefixedKey(string key)
        {
            return $"{AgentId}:{key}";
        }
        
        /// <summary>
        /// Removes the agent ID prefix from keys
        /// </summary>
        /// <param name="keys">Keys with prefix</param>
        /// <returns>Keys without prefix</returns>
        protected virtual IEnumerable<string> RemovePrefixFromKeys(IEnumerable<string> keys)
        {
            var prefix = $"{AgentId}:";
            var result = new List<string>();
            
            foreach (var key in keys)
            {
                if (key.StartsWith(prefix))
                {
                    result.Add(key.Substring(prefix.Length));
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Removes the agent ID prefix from a dictionary's keys
        /// </summary>
        /// <param name="dictionary">Dictionary with prefixed keys</param>
        /// <returns>Dictionary with unprefixed keys</returns>
        protected virtual IDictionary<string, object> RemovePrefixFromDictionary(IDictionary<string, object> dictionary)
        {
            var prefix = $"{AgentId}:";
            var result = new Dictionary<string, object>();
            
            foreach (var pair in dictionary)
            {
                if (pair.Key.StartsWith(prefix))
                {
                    result[pair.Key.Substring(prefix.Length)] = pair.Value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Removes the section prefix from keys
        /// </summary>
        /// <param name="keys">Keys with section prefix</param>
        /// <param name="section">Section name</param>
        /// <returns>Keys without section prefix</returns>
        protected virtual IEnumerable<string> RemoveSectionPrefixFromKeys(IEnumerable<string> keys, string section)
        {
            var prefix = $"{section}:";
            var result = new List<string>();
            
            foreach (var key in keys)
            {
                if (key.StartsWith(prefix))
                {
                    result.Add(key.Substring(prefix.Length));
                }
            }
            
            return result;
        }
    }
}
