using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Configuration.Builders;
using Aigent.Configuration.Core;
using Aigent.Configuration.Registry;

namespace Aigent.Configuration
{
    /// <summary>
    /// Type aliases for backward compatibility
    /// </summary>
    /// 
    /// <summary>
    /// Interface for agent builder services (Legacy)
    /// </summary>
    public interface IAgentBuilder : Builders.IAgentBuilder
    {
    }

    /// <summary>
    /// Interface for configuration (Legacy)
    /// </summary>
    public interface IConfiguration : Core.IConfiguration
    {
    }

    /// <summary>
    /// Interface for configuration sections (Legacy)
    /// </summary>
    public interface IConfigurationSection : Core.IConfigurationSection
    {
    }

    /// <summary>
    /// Interface for agent registry (Legacy)
    /// </summary>
    public interface IAgentRegistry : Registry.IAgentRegistry
    {
    }

    /// <summary>
    /// Agent configuration class (Legacy)
    /// </summary>
    public class AgentConfiguration : Core.AgentConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the AgentConfiguration class
        /// </summary>
        public AgentConfiguration() : base()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the AgentConfiguration class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="type">Type of the agent</param>
        public AgentConfiguration(string name, AgentType type) : base(name, type)
        {
        }
    }

    /// <summary>
    /// Enhanced agent builder implementation (Legacy)
    /// </summary>
    public class EnhancedAgentBuilder : Builders.EnhancedAgentBuilder, IAgentBuilder
    {
        /// <summary>
        /// Initializes a new instance of the EnhancedAgentBuilder class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        /// <param name="logger">Logger for recording builder activities</param>
        /// <param name="configuration">Configuration for the builder</param>
        public EnhancedAgentBuilder(
            IServiceProvider serviceProvider,
            Aigent.Monitoring.Logging.ILogger logger,
            Core.IConfiguration configuration) 
            : base(serviceProvider, logger, configuration)
        {
        }
    }

    /// <summary>
    /// Agent registry implementation (Legacy)
    /// </summary>
    public class AgentRegistry : Registry.AgentRegistry, IAgentRegistry
    {
        /// <summary>
        /// Initializes a new instance of the AgentRegistry class
        /// </summary>
        /// <param name="agentBuilder">Agent builder</param>
        /// <param name="logger">Logger</param>
        public AgentRegistry(
            Builders.IAgentBuilder agentBuilder, 
            Aigent.Monitoring.Logging.ILogger logger) 
            : base(agentBuilder, logger)
        {
        }
    }

    /// <summary>
    /// Legacy static extension methods for service collection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Aigent services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigent(
            this IServiceCollection services, 
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return DI.ServiceCollectionExtensions.AddAigent(services, configuration);
        }

        /// <summary>
        /// Adds configuration services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigentConfiguration(
            this IServiceCollection services, 
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return DI.ServiceCollectionExtensions.AddAigentConfiguration(services, configuration);
        }
    }
}
