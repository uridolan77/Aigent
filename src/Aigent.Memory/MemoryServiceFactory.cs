using System;
using LazyCache;
using Aigent.Memory.Interfaces;
using Aigent.Memory.Providers;
using Aigent.Monitoring;

namespace Aigent.Memory
{
    /// <summary>
    /// Factory for creating memory services
    /// </summary>
    public class MemoryServiceFactory : IMemoryServiceFactory
    {
        private readonly IAppCache _cache;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private readonly MemoryServiceOptions _defaultOptions;
        
        /// <summary>
        /// Initializes a new instance of the MemoryServiceFactory class with default options
        /// </summary>
        /// <param name="cache">LazyCache instance</param>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        public MemoryServiceFactory(IAppCache cache, ILogger logger = null, IMetricsCollector metrics = null)
            : this(cache, new MemoryServiceOptions(), logger, metrics)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the MemoryServiceFactory class with specified options
        /// </summary>
        /// <param name="cache">LazyCache instance</param>
        /// <param name="defaultOptions">Default options for memory services</param>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        public MemoryServiceFactory(IAppCache cache, MemoryServiceOptions defaultOptions, ILogger logger = null, IMetricsCollector metrics = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _defaultOptions = defaultOptions ?? new MemoryServiceOptions();
            _logger = logger;
            _metrics = metrics;
        }
        
        /// <summary>
        /// Creates a memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Memory service for the agent</returns>
        public Core.Interfaces.IMemoryService CreateMemoryService(string agentId)
        {
            return CreateMemoryService(agentId, _defaultOptions);
        }
        
        /// <summary>
        /// Creates a memory service for an agent with specific options
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="options">Memory service options</param>
        /// <returns>Memory service for the agent</returns>
        public IMemoryService CreateMemoryService(string agentId, MemoryServiceOptions options)
        {
            options ??= _defaultOptions;
            
            switch (options.ServiceType)
            {
                case MemoryServiceType.LazyCache:
                    return new LazyCacheMemoryService(_cache, agentId, _logger, _metrics);
                
                case MemoryServiceType.MongoDB:
                    ValidateDatabaseOptions(options);
                    return new MongoDbMemoryService(
                        options.ConnectionString,
                        options.DatabaseName,
                        options.CollectionName,
                        agentId,
                        _logger,
                        _metrics);
                
                case MemoryServiceType.InMemory:
                default:
                    return new ConcurrentMemoryService(agentId, options.MaxCapacity, _logger, _metrics);
            }
        }
        
        /// <summary>
        /// Creates a short-term memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Short-term memory service for the agent</returns>
        public IShortTermMemory CreateShortTermMemory(string agentId)
        {
            var options = new MemoryServiceOptions
            {
                ServiceType = MemoryServiceType.LazyCache,
                DefaultExpirationTime = _defaultOptions.DefaultExpirationTime ?? TimeSpan.FromHours(1)
            };
            
            var service = CreateMemoryService(agentId, options) as IShortTermMemory;
            
            if (service == null)
            {
                throw new InvalidOperationException("Failed to create short-term memory service");
            }
            
            return service;
        }
        
        /// <summary>
        /// Creates a long-term memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Long-term memory service for the agent</returns>
        public ILongTermMemory CreateLongTermMemory(string agentId)
        {
            var options = new MemoryServiceOptions
            {
                ServiceType = MemoryServiceType.MongoDB,
                ConnectionString = _defaultOptions.ConnectionString,
                DatabaseName = _defaultOptions.DatabaseName ?? "AigentMemory",
                CollectionName = _defaultOptions.CollectionName ?? "AgentMemory",
                DefaultExpirationTime = null  // Long-term memory doesn't expire by default
            };
            
            var service = CreateMemoryService(agentId, options) as ILongTermMemory;
            
            if (service == null)
            {
                // Fall back to concurrent memory if MongoDB is not configured
                _logger?.Warning("MongoDB not configured for long-term memory, falling back to in-memory storage");
                service = new ConcurrentMemoryService(agentId, null, _logger, _metrics) as ILongTermMemory;
            }
            
            if (service == null)
            {
                throw new InvalidOperationException("Failed to create long-term memory service");
            }
            
            return service;
        }
        
        // Helper methods
        
        private void ValidateDatabaseOptions(MemoryServiceOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new ArgumentException("ConnectionString is required for database providers", nameof(options));
            }
            
            if (string.IsNullOrEmpty(options.DatabaseName))
            {
                options.DatabaseName = "AigentMemory";
            }
            
            if (string.IsNullOrEmpty(options.CollectionName))
            {
                options.CollectionName = "AgentMemory";
            }
        }
    }
}
