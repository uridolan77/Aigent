using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LazyCache;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring.Logging;
using Aigent.Monitoring.Metrics;
using Aigent.Configuration.Builders;
using Aigent.Configuration.Core;
using Aigent.Configuration.Registry;
using Aigent.Orchestration;

namespace Aigent.Configuration.DI
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

            // Create and register Aigent configuration wrapper
            var aigentConfig = new ConfigurationAdapter(configuration);
            services.AddSingleton<Core.IConfiguration>(aigentConfig);

            // Add core services
            services.AddLogging();
            services.AddSingleton<ILogger, ConsoleLogger>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            services.AddSingleton<ISafetyValidator, EnhancedSafetyValidator>();
            services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();

            // Add LazyCache
            services.AddSingleton<IAppCache>(new CachingService());

            // Add memory services
            var memoryType = aigentConfig.GetValue("Aigent:MemoryType");
            switch (memoryType)
            {
                case "Redis":
                    services.AddSingleton<IMemoryService>(sp =>
                        new RedisMemoryService(
                            aigentConfig.GetValue("Aigent:Redis:ConnectionString"),
                            sp.GetService<ILogger>(),
                            sp.GetService<IMetricsCollector>()));
                    break;
                case "SQL":
                    services.AddSingleton<IMemoryService>(sp =>
                        new SqlMemoryService(
                            aigentConfig.GetValue("Aigent:SQL:ConnectionString"),
                            sp.GetService<ILogger>(),
                            sp.GetService<IMetricsCollector>()));
                    break;
                case "DocumentDb":
                    services.AddSingleton<IMemoryService>(sp =>
                        new DocumentDbMemoryService(
                            aigentConfig.GetValue("Aigent:DocumentDb:ConnectionString"),
                            sp.GetService<ILogger>(),
                            sp.GetService<IMetricsCollector>()));
                    break;
                default:
                    services.AddSingleton<IMemoryService, LazyCacheMemoryService>();
                    services.AddSingleton<IMemoryServiceFactory, LazyCacheMemoryServiceFactory>();
                    break;
            }

            // Add agent builder and registry
            services.AddSingleton<IAgentBuilder, EnhancedAgentBuilder>();
            services.AddSingleton<IAgentRegistry, AgentRegistry>();

            // Add orchestration
            services.AddSingleton<IOrchestrator, EnhancedOrchestrator>();

            return services;
        }

        /// <summary>
        /// Adds configuration services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigentConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<Core.IConfiguration>(new ConfigurationAdapter(configuration));
            return services;
        }
    }

    /// <summary>
    /// Adapter for Microsoft.Extensions.Configuration to Aigent.Configuration.Core.IConfiguration
    /// </summary>
    internal class ConfigurationAdapter : Core.IConfiguration
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the ConfigurationAdapter class
        /// </summary>
        /// <param name="configuration">Microsoft configuration</param>
        public ConfigurationAdapter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        public Core.IConfigurationSection GetSection(string key)
        {
            var section = _configuration.GetSection(key);
            
            // This is a simplified adapter - in a real implementation,
            // you would properly convert the section
            var dictionary = new Dictionary<string, object>
            {
                { "Value", section.Value }
            };
            
            foreach (var child in section.GetChildren())
            {
                dictionary[child.Key] = child.Value;
            }
            
            return new ConfigurationSection(dictionary)
            {
                Key = key
            };
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public string GetValue(string key)
        {
            return _configuration[key];
        }

        /// <summary>
        /// Gets a strongly typed configuration value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public T GetValue<T>(string key)
        {
            return _configuration.GetValue<T>(key);
        }

        /// <summary>
        /// Gets a strongly typed configuration value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key of the value</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>The configuration value or default</returns>
        public T GetValue<T>(string key, T defaultValue)
        {
            return _configuration.GetValue<T>(key, defaultValue);
        }
    }
}
