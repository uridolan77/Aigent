using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LazyCache;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Configuration
{
    /// <summary>
    /// Extension methods for IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Aigent services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigent(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Add core services
            services.AddSingleton<ILogger, ConsoleLogger>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            services.AddSingleton<ISafetyValidator, BasicSafetyValidator>();
            services.AddSingleton<IMetricsCollector, BasicMetricsCollector>();

            // Add LazyCache
            services.AddSingleton<IAppCache>(new CachingService());

            // Add memory services
            services.AddSingleton<IMemoryServiceFactory, LazyCacheMemoryServiceFactory>();

            // Add agent builder
            services.AddSingleton<IAgentBuilder, EnhancedAgentBuilder>();

            // Add orchestration
            // services.AddSingleton<IOrchestrator, BasicOrchestrator>();

            // Add agent registry
            services.AddSingleton<IAgentRegistry, AgentRegistry>();

            return services;
        }
    }
}
