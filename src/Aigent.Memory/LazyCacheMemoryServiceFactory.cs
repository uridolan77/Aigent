using System;
using LazyCache;
using Aigent.Core;
using Aigent.Monitoring;

namespace Aigent.Memory
{
    /// <summary>
    /// Factory for creating LazyCache memory services
    /// </summary>
    public class LazyCacheMemoryServiceFactory : IMemoryServiceFactory
    {
        private readonly IAppCache _cache;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the LazyCacheMemoryServiceFactory class
        /// </summary>
        /// <param name="cache">LazyCache instance</param>
        /// <param name="logger">Logger</param>
        public LazyCacheMemoryServiceFactory(IAppCache cache, ILogger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Memory service for the agent</returns>
        public IMemoryService CreateMemoryService(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            }
            
            _logger.Log(LogLevel.Debug, $"Creating LazyCache memory service for agent {agentId}");
            
            return new LazyCacheMemoryService(_cache, _logger, agentId);
        }
    }
}
