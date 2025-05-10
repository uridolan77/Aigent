using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using Aigent.Core;
using Aigent.Monitoring;

namespace Aigent.Memory
{
    /// <summary>
    /// Memory service implementation using LazyCache
    /// </summary>
    public class LazyCacheMemoryService : IMemoryService
    {
        private readonly IAppCache _cache;
        private readonly ILogger _logger;
        private readonly string _agentId;
        private readonly Dictionary<string, TimeSpan> _expirationTimes;

        /// <summary>
        /// Initializes a new instance of the LazyCacheMemoryService class
        /// </summary>
        /// <param name="cache">LazyCache instance</param>
        /// <param name="logger">Logger</param>
        /// <param name="agentId">ID of the agent using this memory service</param>
        public LazyCacheMemoryService(IAppCache cache, ILogger logger, string agentId)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            
            _expirationTimes = new Dictionary<string, TimeSpan>
            {
                ["short"] = TimeSpan.FromMinutes(5),
                ["medium"] = TimeSpan.FromHours(1),
                ["long"] = TimeSpan.FromDays(1),
                ["permanent"] = TimeSpan.MaxValue
            };
        }

        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="durationType">Duration type (short, medium, long, permanent)</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task Store<T>(string key, T value, string durationType = "medium")
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
            
            var fullKey = $"{_agentId}:{key}";
            
            if (!_expirationTimes.TryGetValue(durationType, out var expiration))
            {
                _logger.Log(LogLevel.Warning, $"Unknown duration type: {durationType}, using medium");
                expiration = _expirationTimes["medium"];
            }
            
            if (expiration == TimeSpan.MaxValue)
            {
                _cache.Add(fullKey, value);
            }
            else
            {
                _cache.Add(fullKey, value, expiration);
            }
            
            _logger.Log(LogLevel.Debug, $"Stored value for key {fullKey} with duration {durationType}");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key to retrieve the value for</param>
        /// <returns>The retrieved value, or default if not found</returns>
        public Task<T> Retrieve<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
            
            var fullKey = $"{_agentId}:{key}";
            var value = _cache.Get<T>(fullKey);
            
            _logger.Log(LogLevel.Debug, $"Retrieved value for key {fullKey}: {(value != null ? "found" : "not found")}");
            
            return Task.FromResult(value);
        }

        /// <summary>
        /// Stores context information in memory
        /// </summary>
        /// <typeparam name="T">Type of the context</typeparam>
        /// <param name="contextType">Type of the context</param>
        /// <param name="context">Context to store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task StoreContext<T>(string contextType, T context)
        {
            if (string.IsNullOrEmpty(contextType))
            {
                throw new ArgumentException("Context type cannot be null or empty", nameof(contextType));
            }
            
            var key = $"context:{contextType}";
            return Store(key, context, "long");
        }

        /// <summary>
        /// Retrieves context information from memory
        /// </summary>
        /// <typeparam name="T">Type of the context</typeparam>
        /// <param name="contextType">Type of the context</param>
        /// <returns>The retrieved context, or default if not found</returns>
        public Task<T> RetrieveContext<T>(string contextType)
        {
            if (string.IsNullOrEmpty(contextType))
            {
                throw new ArgumentException("Context type cannot be null or empty", nameof(contextType));
            }
            
            var key = $"context:{contextType}";
            return Retrieve<T>(key);
        }

        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
            
            var fullKey = $"{_agentId}:{key}";
            _cache.Remove(fullKey);
            
            _logger.Log(LogLevel.Debug, $"Removed value for key {fullKey}");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears all values from memory for this agent
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task Clear()
        {
            // LazyCache doesn't support partial clearing, so we can't easily clear just this agent's data
            // In a real implementation, we would need to track the keys used by this agent
            _logger.Log(LogLevel.Warning, "Clear operation not fully supported by LazyCache implementation");
            
            return Task.CompletedTask;
        }
    }
}
