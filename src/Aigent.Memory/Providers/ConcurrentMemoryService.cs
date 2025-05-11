using System;
using System.Threading.Tasks;
using Aigent.Memory.Interfaces;
using Aigent.Monitoring;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// Thread-safe in-memory implementation of IShortTermMemory
    /// </summary>
    public class ConcurrentMemoryService : BaseMemoryService, IShortTermMemory
    {
        private TimeSpan _defaultExpirationTime = TimeSpan.FromHours(1);
        
        /// <summary>
        /// Initializes a new instance of the ConcurrentMemoryService class
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="maxCapacity">Optional maximum capacity</param>
        /// <param name="logger">Logger for recording memory operations</param>
        /// <param name="metrics">Metrics collector for monitoring memory performance</param>
        public ConcurrentMemoryService(
            string agentId, 
            int? maxCapacity = null, 
            ILogger logger = null, 
            IMetricsCollector metrics = null)
            : base(new ConcurrentMemoryProvider(maxCapacity), agentId, logger, metrics)
        {
        }
        
        /// <summary>
        /// Sets the default expiration time for items stored in short-term memory
        /// </summary>
        /// <param name="expirationTime">The default expiration time</param>
        public void SetDefaultExpirationTime(TimeSpan expirationTime)
        {
            _defaultExpirationTime = expirationTime;
        }
        
        /// <summary>
        /// Renews the expiration time for a key
        /// </summary>
        /// <param name="key">Key to renew</param>
        /// <param name="expirationTime">New expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task RenewExpirationAsync(string key, TimeSpan expirationTime)
        {
            var prefixedKey = GetPrefixedKey(key);
            var provider = (ConcurrentMemoryProvider)MemoryProvider;
            var memory = (System.Collections.Concurrent.ConcurrentDictionary<string, Models.MemoryEntry>)provider.GetStorageProvider();
            
            if (memory.TryGetValue(prefixedKey, out var entry))
            {
                entry.SetExpiration(expirationTime);
                Logger?.Debug($"Renewed expiration for key {prefixedKey}");
            }
            else
            {
                Logger?.Warning($"Attempted to renew expiration for non-existent key {prefixedKey}");
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets the remaining time until a key expires
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>The remaining time, or null if the key doesn't exist or has no expiration</returns>
        public async Task<TimeSpan?> GetExpirationTimeRemainingAsync(string key)
        {
            var prefixedKey = GetPrefixedKey(key);
            var provider = (ConcurrentMemoryProvider)MemoryProvider;
            var memory = (System.Collections.Concurrent.ConcurrentDictionary<string, Models.MemoryEntry>)provider.GetStorageProvider();
            
            if (memory.TryGetValue(prefixedKey, out var entry) && entry.ExpiresAt.HasValue)
            {
                var remaining = entry.ExpiresAt.Value - DateTimeOffset.UtcNow;
                return remaining.TotalMilliseconds > 0 ? remaining : TimeSpan.Zero;
            }
            
            return null;
        }
        
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public override async Task StoreAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            // Use default expiration time if not specified
            var actualExpiration = expirationTime ?? _defaultExpirationTime;
            await base.StoreAsync(key, value, actualExpiration);
        }
    }
}
