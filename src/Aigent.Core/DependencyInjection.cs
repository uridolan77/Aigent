using System;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core.Configuration;
using Aigent.Core.Interfaces;

namespace Aigent.Core
{
    /// <summary>
    /// Dependency injection extensions for Core components
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Core services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Register Core interfaces and implementations
            services.AddTransient<IConfigurationSectionFactory, ConfigurationSectionFactory>();
            
            return services;
        }
    }
}
