using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Configuration
{
    /// <summary>
    /// Enhanced agent builder implementation
    /// </summary>
    public class EnhancedAgentBuilder : IAgentBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly ISafetyValidator _safetyValidator;

        private AgentConfiguration _agentConfiguration;
        private IMemoryService _memoryService;

        /// <summary>
        /// Initializes a new instance of the EnhancedAgentBuilder class
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <param name="logger">Logger</param>
        /// <param name="safetyValidator">Safety validator</param>
        public EnhancedAgentBuilder(
            IServiceProvider serviceProvider,
            ILogger logger,
            ISafetyValidator safetyValidator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyValidator = safetyValidator ?? throw new ArgumentNullException(nameof(safetyValidator));
        }

        /// <summary>
        /// Sets the configuration for the agent
        /// </summary>
        /// <param name="configuration">Agent configuration</param>
        /// <returns>The builder</returns>
        public IAgentBuilder WithConfiguration(AgentConfiguration configuration)
        {
            _agentConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            return this;
        }

        /// <summary>
        /// Sets the memory service for the agent
        /// </summary>
        /// <typeparam name="T">Type of the memory service</typeparam>
        /// <returns>The builder</returns>
        public IAgentBuilder WithMemory<T>() where T : IMemoryService
        {
            if (_agentConfiguration == null)
            {
                throw new InvalidOperationException("Agent configuration must be set before memory service");
            }

            var factories = _serviceProvider.GetServices<IMemoryServiceFactory>();

            foreach (var factory in factories)
            {
                if (factory.GetType().Name.Contains(typeof(T).Name.Replace("MemoryService", "")))
                {
                    _memoryService = factory.CreateMemoryService(_agentConfiguration.Name);
                    _logger.Log(LogLevel.Debug, $"Using memory service: {typeof(T).Name}");
                    return this;
                }
            }

            throw new InvalidOperationException($"Memory service factory for {typeof(T).Name} not found");
        }

        /// <summary>
        /// Builds the agent
        /// </summary>
        /// <returns>The built agent</returns>
        public IAgent Build()
        {
            if (_agentConfiguration == null)
            {
                throw new InvalidOperationException("Agent configuration must be set");
            }

            if (_memoryService == null)
            {
                // Default to LazyCache memory service
                var factory = _serviceProvider.GetServices<IMemoryServiceFactory>()
                    .FirstOrDefault(f => f is LazyCacheMemoryServiceFactory);

                if (factory == null)
                {
                    throw new InvalidOperationException("No memory service factory found");
                }

                _memoryService = factory.CreateMemoryService(_agentConfiguration.Name);
                _logger.Log(LogLevel.Debug, "Using default LazyCache memory service");
            }

            var messageBus = _serviceProvider.GetRequiredService<IMessageBus>();
            var metrics = _serviceProvider.GetService<IMetricsCollector>();

            // Placeholder for agent creation
            _logger.Log(LogLevel.Information, $"Creating agent of type: {_agentConfiguration.Type}");

            // Return a placeholder agent
            return new PlaceholderAgent(
                _agentConfiguration.Name,
                _agentConfiguration.Type,
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus,
                metrics);
        }

        // Placeholder agent implementation
        private class PlaceholderAgent : IAgent
        {
            public string Id { get; } = Guid.NewGuid().ToString();
            public string Name { get; }
            public AgentType Type { get; }
            public AgentStatus Status { get; private set; } = AgentStatus.Initializing;
            public AgentCapabilities Capabilities { get; } = new AgentCapabilities();

            private readonly IMemoryService _memory;
            private readonly ISafetyValidator _safetyValidator;
            private readonly ILogger _logger;
            private readonly IMessageBus _messageBus;
            private readonly IMetricsCollector _metrics;

            public PlaceholderAgent(
                string name,
                AgentType type,
                IMemoryService memory,
                ISafetyValidator safetyValidator,
                ILogger logger,
                IMessageBus messageBus,
                IMetricsCollector metrics)
            {
                Name = name;
                Type = type;
                _memory = memory;
                _safetyValidator = safetyValidator;
                _logger = logger;
                _messageBus = messageBus;
                _metrics = metrics;
            }

            public Task Initialize()
            {
                Status = AgentStatus.Ready;
                _logger.Log(LogLevel.Information, $"Initialized placeholder agent: {Name}");
                return Task.CompletedTask;
            }

            public Task<IAction> DecideAction(EnvironmentState state)
            {
                var text = $"I'm a placeholder agent of type {Type}. I received input: {state.Properties.GetValueOrDefault("input", "No input")}";
                return Task.FromResult<IAction>(new TextOutputAction(text));
            }

            public Task Learn(EnvironmentState state, IAction action, ActionResult result)
            {
                _logger.Log(LogLevel.Information, $"Placeholder agent {Name} learning from action {action.ActionType}");
                return Task.CompletedTask;
            }

            public Task Shutdown()
            {
                Status = AgentStatus.ShuttingDown;
                _logger.Log(LogLevel.Information, $"Shutting down placeholder agent: {Name}");
                return Task.CompletedTask;
            }

            private bool _disposed = false;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        Shutdown().GetAwaiter().GetResult();
                    }
                    _disposed = true;
                }
            }
        }
    }
}
