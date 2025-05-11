using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Api.Interfaces;
using Aigent.Api.Services;
using Aigent.Monitoring;

namespace Aigent.Api.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Aigent.Api services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddAigentApi(this IServiceCollection services, IConfiguration configuration)
        {
            // Add interfaces and their implementations
            services.AddSingleton<IAgentRegistry, AgentRegistryService>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IApiAnalyticsService, ApiAnalyticsService>();
            
            // Configure logging
            services.AddSingleton<ILogger, ConsoleLogger>();
            
            return services;
        }
    }
}
