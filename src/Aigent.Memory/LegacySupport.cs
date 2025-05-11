using System;
using Aigent.Memory.Compatibility;
using Aigent.Memory.Interfaces;
using Aigent.Memory.Providers;

namespace Aigent.Memory
{
    /// <summary>
    /// Provides support for legacy code using the old memory interfaces
    /// </summary>
    public static class LegacySupport
    {
        /// <summary>
        /// Creates a legacy memory service adapter
        /// </summary>
        /// <param name="service">The new memory service</param>
        /// <returns>A legacy memory service adapter</returns>
        public static IMemoryService CreateLegacyAdapter(Interfaces.IMemoryService service)
        {
            return new LegacyMemoryService(service);
        }
        
        /// <summary>
        /// Creates a memory service factory that produces legacy memory services
        /// </summary>
        /// <param name="factory">The new memory service factory</param>
        /// <returns>A memory service factory that produces legacy memory services</returns>
        public static IMemoryServiceFactory CreateLegacyFactoryAdapter(Interfaces.IMemoryServiceFactory factory)
        {
            return new LegacyMemoryServiceFactory(factory);
        }
        
        /// <summary>
        /// Creates a legacy concurrent memory service
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="logger">Optional logger</param>
        /// <param name="metrics">Optional metrics collector</param>
        /// <returns>A legacy concurrent memory service</returns>
        public static IMemoryService CreateLegacyConcurrentMemoryService(string agentId, Aigent.Monitoring.ILogger logger = null, Aigent.Monitoring.IMetricsCollector metrics = null)
        {
            var service = new ConcurrentMemoryService(agentId, null, logger, metrics);
            return CreateLegacyAdapter(service);
        }
        
        /// <summary>
        /// Legacy memory service factory adapter
        /// </summary>
        private class LegacyMemoryServiceFactory : IMemoryServiceFactory
        {
            private readonly Interfaces.IMemoryServiceFactory _factory;
            
            /// <summary>
            /// Initializes a new instance of the LegacyMemoryServiceFactory class
            /// </summary>
            /// <param name="factory">The new memory service factory</param>
            public LegacyMemoryServiceFactory(Interfaces.IMemoryServiceFactory factory)
            {
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            }
            
            /// <summary>
            /// Creates a memory service for an agent
            /// </summary>
            /// <param name="agentId">ID of the agent</param>
            /// <returns>Memory service for the agent</returns>
            public IMemoryService CreateMemoryService(string agentId)
            {
                var service = _factory.CreateMemoryService(agentId);
                return CreateLegacyAdapter(service);
            }
            
            /// <summary>
            /// Creates a short-term memory service for an agent
            /// </summary>
            /// <param name="agentId">ID of the agent</param>
            /// <returns>Short-term memory service for the agent</returns>
            public IShortTermMemory CreateShortTermMemory(string agentId)
            {
                var service = _factory.CreateShortTermMemory(agentId);
                return (IShortTermMemory)CreateLegacyAdapter(service);
            }
            
            /// <summary>
            /// Creates a long-term memory service for an agent
            /// </summary>
            /// <param name="agentId">ID of the agent</param>
            /// <returns>Long-term memory service for the agent</returns>
            public ILongTermMemory CreateLongTermMemory(string agentId)
            {
                var service = _factory.CreateLongTermMemory(agentId);
                return (ILongTermMemory)CreateLegacyAdapter(service);
            }
            
            /// <summary>
            /// Creates a memory service for an agent with specific options
            /// </summary>
            /// <param name="agentId">ID of the agent</param>
            /// <param name="options">Memory service options</param>
            /// <returns>Memory service for the agent</returns>
            public IMemoryService CreateMemoryService(string agentId, MemoryServiceOptions options)
            {
                var service = _factory.CreateMemoryService(agentId, options);
                return CreateLegacyAdapter(service);
            }
        }
    }
}
