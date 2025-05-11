using System;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Orchestration.Interfaces;
using Aigent.Orchestration.Models;
using Aigent.Orchestration.Orchestrators;
using Aigent.Orchestration.Engines;
using Aigent.Orchestration.Compatibility;

namespace Aigent.Orchestration
{
    /// <summary>
    /// Extensions for registering orchestration services with dependency injection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds orchestration services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddOrchestration(this IServiceCollection services)
        {
            return AddOrchestration(services, _ => { });
        }
        
        /// <summary>
        /// Adds orchestration services to the service collection with the specified configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">A delegate to configure the orchestration options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddOrchestration(
            this IServiceCollection services,
            Action<OrchestratorConfiguration> configureOptions)
        {
            // Register configuration
            var configuration = new OrchestratorConfiguration();
            configureOptions?.Invoke(configuration);
            services.AddSingleton(configuration);
            
            // Register workflow engine
            services.AddSingleton<IWorkflowEngine, StandardWorkflowEngine>();
            
            // Register orchestrator factory
            services.AddSingleton<IOrchestratorFactory, OrchestratorFactory>();
            
            // Register default orchestrator
            services.AddSingleton<IOrchestrator>(provider =>
            {
                var factory = provider.GetRequiredService<IOrchestratorFactory>();
                return factory.CreateOrchestrator(configuration);
            });
            
            // Register legacy compatibility services
            services.AddSingleton<global::Aigent.Orchestration.IOrchestrator>(provider =>
            {
                var modernOrchestrator = provider.GetRequiredService<IOrchestrator>();
                return LegacySupport.CreateLegacyOrchestrator(modernOrchestrator);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds backward compatibility services for legacy orchestration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddLegacyOrchestrationSupport(this IServiceCollection services)
        {
            // Register legacy orchestrator adapter
            services.AddSingleton<global::Aigent.Orchestration.IOrchestrator2>(provider =>
            {
                var modernOrchestrator = provider.GetRequiredService<IOrchestrator>();
                var logger = provider.GetRequiredService<Aigent.Monitoring.ILogger>();
                return new LegacyOrchestrator2Adapter(modernOrchestrator, logger);
            });
            
            return services;
        }
    }
}
