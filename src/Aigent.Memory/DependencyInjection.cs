using System;
using Microsoft.Extensions.DependencyInjection;
using LazyCache;
using LazyCache.Providers;
using Aigent.Memory.Interfaces;
using Aigent.Memory.Providers;
using Aigent.Monitoring;

namespace Aigent.Memory
{
    /// <summary>
    /// Extension methods for setting up memory services in an IServiceCollection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds memory services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigentMemory(this IServiceCollection services)
        {
            return AddAigentMemory(services, new MemoryServiceOptions());
        }
        
        /// <summary>
        /// Adds memory services to the service collection with specified options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="options">Memory service options</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigentMemory(this IServiceCollection services, MemoryServiceOptions options)
        {
            // Register LazyCache
            services.AddSingleton<IAppCache>(provider => 
            {
                return new CachingService
                {
                    CacheProvider = new MemoryCacheProvider()
                };
            });
            
            // Register memory service factory
            services.AddSingleton<IMemoryServiceFactory>(provider => 
            {
                var cache = provider.GetRequiredService<IAppCache>();
                var logger = provider.GetService<ILogger>();
                var metrics = provider.GetService<IMetricsCollector>();
                
                return new MemoryServiceFactory(cache, options, logger, metrics);
            });
            
            // Also register the Core.Interfaces version
            services.AddSingleton<Core.Interfaces.IMemoryServiceFactory>(provider => 
            {
                return provider.GetRequiredService<IMemoryServiceFactory>();
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds MongoDB memory services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="collectionName">Name of the collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddMongoDbMemory(this IServiceCollection services, string connectionString, string databaseName = "AigentMemory", string collectionName = "AgentMemory")
        {
            var options = new MemoryServiceOptions
            {
                ServiceType = MemoryServiceType.MongoDB,
                ConnectionString = connectionString,
                DatabaseName = databaseName,
                CollectionName = collectionName
            };
            
            return AddAigentMemory(services, options);
        }
        
        /// <summary>
        /// Adds in-memory services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="maxCapacity">Optional maximum capacity for the memory cache</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddInMemoryMemory(this IServiceCollection services, int? maxCapacity = null)
        {
            var options = new MemoryServiceOptions
            {
                ServiceType = MemoryServiceType.InMemory,
                MaxCapacity = maxCapacity
            };
            
            return AddAigentMemory(services, options);
        }
        
        /// <summary>
        /// Adds LazyCache memory services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="defaultExpirationTime">Optional default expiration time</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddLazyCacheMemory(this IServiceCollection services, TimeSpan? defaultExpirationTime = null)
        {
            var options = new MemoryServiceOptions
            {
                ServiceType = MemoryServiceType.LazyCache,
                DefaultExpirationTime = defaultExpirationTime
            };
            
            return AddAigentMemory(services, options);
        }
    }
}
