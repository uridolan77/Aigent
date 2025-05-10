using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Monitoring;

namespace Aigent.Memory
{
    /// <summary>
    /// Thread-safe in-memory implementation of IShortTermMemory
    /// </summary>
    public class ConcurrentMemoryService : IShortTermMemory
    {
        private readonly ConcurrentDictionary<string, MemoryEntry> _memory = new();
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private string _agentId;

        /// <summary>
        /// Initializes a new instance of the ConcurrentMemoryService class
        /// </summary>
        /// <param name="logger">Logger for recording memory operations</param>
        /// <param name="metrics">Metrics collector for monitoring memory performance</param>
        public ConcurrentMemoryService(ILogger logger = null, IMetricsCollector metrics = null)
        {
            _logger = logger;
            _metrics = metrics;
        }

        /// <summary>
        /// Initializes the memory service for a specific agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        public Task Initialize(string agentId)
        {
            _agentId = agentId;
            _logger?.Log(LogLevel.Debug, $"Initialized memory service for agent {agentId}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stores a value in the agent's context
        /// </summary>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="ttl">Optional time-to-live for the value</param>
        public Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            _metrics?.StartOperation($"memory_{_agentId}_store");
            
            try
            {
                var fullKey = $"{_agentId}:{key}";
                var entry = new MemoryEntry
                {
                    Value = value,
                    Expiry = ttl.HasValue ? DateTime.UtcNow + ttl.Value : DateTime.MaxValue
                };
                
                _memory.AddOrUpdate(fullKey, entry, (k, v) => entry);
                
                _logger?.Log(LogLevel.Debug, $"Stored value for key {fullKey}");
                _metrics?.RecordMetric($"memory.{_agentId}.store_count", 1.0);
                
                return Task.CompletedTask;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_store");
            }
        }

        /// <summary>
        /// Retrieves a value from the agent's context
        /// </summary>
        /// <typeparam name="T">Type of the value to retrieve</typeparam>
        /// <param name="key">Key the value is stored under</param>
        /// <returns>The retrieved value, or default if not found</returns>
        public Task<T> RetrieveContext<T>(string key)
        {
            _metrics?.StartOperation($"memory_{_agentId}_retrieve");
            
            try
            {
                var fullKey = $"{_agentId}:{key}";
                
                if (_memory.TryGetValue(fullKey, out var entry) && entry.Expiry > DateTime.UtcNow)
                {
                    _logger?.Log(LogLevel.Debug, $"Retrieved value for key {fullKey}");
                    _metrics?.RecordMetric($"memory.{_agentId}.retrieve_hit_count", 1.0);
                    
                    return Task.FromResult((T)entry.Value);
                }
                
                _logger?.Log(LogLevel.Debug, $"No value found for key {fullKey}");
                _metrics?.RecordMetric($"memory.{_agentId}.retrieve_miss_count", 1.0);
                
                return Task.FromResult(default(T));
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_retrieve");
            }
        }

        /// <summary>
        /// Clears all memory for the agent
        /// </summary>
        public Task ClearMemory()
        {
            _metrics?.StartOperation($"memory_{_agentId}_clear");
            
            try
            {
                var keysToRemove = _memory.Keys.Where(k => k.StartsWith($"{_agentId}:")).ToList();
                
                foreach (var key in keysToRemove)
                {
                    _memory.TryRemove(key, out _);
                }
                
                _logger?.Log(LogLevel.Information, $"Cleared memory for agent {_agentId}");
                _metrics?.RecordMetric($"memory.{_agentId}.clear_count", 1.0);
                
                return Task.CompletedTask;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_clear");
            }
        }

        /// <summary>
        /// Flushes any pending changes and cleans up expired entries
        /// </summary>
        public Task Flush()
        {
            _metrics?.StartOperation($"memory_{_agentId}_flush");
            
            try
            {
                // Clean up expired entries
                var expiredKeys = _memory.Where(kv => kv.Value.Expiry <= DateTime.UtcNow)
                                        .Select(kv => kv.Key)
                                        .ToList();
                
                foreach (var key in expiredKeys)
                {
                    _memory.TryRemove(key, out _);
                }
                
                _logger?.Log(LogLevel.Debug, $"Flushed memory for agent {_agentId}, removed {expiredKeys.Count} expired entries");
                _metrics?.RecordMetric($"memory.{_agentId}.expired_entries_count", expiredKeys.Count);
                
                return Task.CompletedTask;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_flush");
            }
        }

        /// <summary>
        /// Represents an entry in the memory store
        /// </summary>
        private class MemoryEntry
        {
            /// <summary>
            /// The stored value
            /// </summary>
            public object Value { get; set; }
            
            /// <summary>
            /// When the entry expires
            /// </summary>
            public DateTime Expiry { get; set; }
        }
    }
}
