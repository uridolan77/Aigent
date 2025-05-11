using System;
using System.Threading.Tasks;
using LazyCache;
using Aigent.Memory.Interfaces;
using Aigent.Monitoring;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// LazyCache-based implementation of IShortTermMemory
    /// </summary>
    public class LazyCacheMemoryService : BaseMemoryService, IShortTermMemory
    {
        private TimeSpan _defaultExpirationTime = TimeSpan.FromHours(1);
        private readonly LazyCacheProvider _lazyCacheProvider;
        
        /// <summary>
        /// Initializes a new instance of the LazyCacheMemoryService class
        /// </summary>
        /// <param name="cache">LazyCache instance</param>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="logger">Logger for recording memory operations</param>
        /// <param name="metrics">Metrics collector for monitoring memory performance</param>
        public LazyCacheMemoryService(
            IAppCache cache, 
            string agentId, 
            ILogger logger = null, 
            IMetricsCollector metrics = null)
            : base(new LazyCacheProvider(cache), agentId, logger, metrics)
        {
            _lazyCacheProvider = (LazyCacheProvider)MemoryProvider;
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
            
            // LazyCache doesn't have a direct way to renew expiration, so we need to retrieve and store again
            var exists = await MemoryProvider.ExistsAsync(prefixedKey);
            
            if (exists)
            {
                var cache = (IAppCache)_lazyCacheProvider.GetStorageProvider();
                var value = cache.GetOrDefault<object>(prefixedKey);
                
                if (value != null)
                {
                    cache.Remove(prefixedKey);
                    cache.Add(prefixedKey, value, expirationTime);
                    Logger?.Debug($"Renewed expiration for key {prefixedKey}");
                }
            }
            else
            {
                Logger?.Warning($"Attempted to renew expiration for non-existent key {prefixedKey}");
            }
        }
        
        /// <summary>
        /// Gets the remaining time until a key expires
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>The remaining time, or null if the key doesn't exist or has no expiration</returns>
        public Task<TimeSpan?> GetExpirationTimeRemainingAsync(string key)
        {
            // LazyCache doesn't expose expiration information, so we can't implement this properly
            Logger?.Warning($"GetExpirationTimeRemainingAsync is not supported by LazyCache");
            return Task.FromResult<TimeSpan?>(null);
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
