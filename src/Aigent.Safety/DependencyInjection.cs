using Microsoft.Extensions.DependencyInjection;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;
using Aigent.Safety.Validators;

namespace Aigent.Safety
{
    /// <summary>
    /// Extension methods for adding safety services to a service collection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds safety services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddAigentSafety(this IServiceCollection services)
        {
            return AddAigentSafety(services, new SafetyConfiguration());
        }
        
        /// <summary>
        /// Adds safety services to the service collection with a specific configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Safety configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddAigentSafety(this IServiceCollection services, SafetyConfiguration configuration)
        {
            // Register factory
            services.AddSingleton<ISafetyValidatorFactory, SafetyValidatorFactory>();
            
            // Register validator (singleton for consistency)
            services.AddSingleton<ISafetyValidator>(provider => {
                var factory = provider.GetRequiredService<ISafetyValidatorFactory>();
                return factory.CreateValidator(configuration);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds a scoped safety validator to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddScopedSafetyValidator(this IServiceCollection services)
        {
            services.AddScoped<ISafetyValidator>(provider => {
                var factory = provider.GetRequiredService<ISafetyValidatorFactory>();
                return factory.CreateValidator();
            });
            
            return services;
        }
    }
}
