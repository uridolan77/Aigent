using System;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Monitoring;
using Aigent.Communication.Interfaces;
using Aigent.Safety.Interfaces;
using Aigent.Orchestration.Interfaces;
using Aigent.Orchestration.Models;
using Aigent.Orchestration.Engines;

namespace Aigent.Orchestration.Orchestrators
{
    /// <summary>
    /// Factory for creating orchestrators
    /// </summary>
    public class OrchestratorFactory : IOrchestratorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        
        /// <summary>
        /// Initializes a new instance of the OrchestratorFactory class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        public OrchestratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        
        /// <summary>
        /// Creates a new orchestrator
        /// </summary>
        /// <returns>A new orchestrator</returns>
        public IOrchestrator CreateOrchestrator()
        {
            return CreateOrchestrator(OrchestratorConfiguration.Default());
        }
        
        /// <summary>
        /// Creates a new orchestrator with the specified configuration
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        /// <returns>A new orchestrator</returns>
        public IOrchestrator CreateOrchestrator(OrchestratorConfiguration configuration)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger>();
            var safetyValidator = _serviceProvider.GetRequiredService<ISafetyValidator>();
            var messageBus = _serviceProvider.GetRequiredService<IMessageBus>();
            var metrics = _serviceProvider.GetService<IMetricsCollector>(); // Optional
            
            // Create workflow engine based on configuration
            var workflowEngine = CreateWorkflowEngine(configuration);
            
            // Create and configure orchestrator
            var orchestrator = new StandardOrchestrator(
                logger,
                safetyValidator,
                messageBus,
                workflowEngine,
                metrics);
            
            orchestrator.Configure(configuration);
            
            return orchestrator;
        }
        
        private IWorkflowEngine CreateWorkflowEngine(OrchestratorConfiguration config)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger>();
            var messageBus = _serviceProvider.GetRequiredService<IMessageBus>();
            var metrics = _serviceProvider.GetService<IMetricsCollector>(); // Optional
            
            // Create engine configuration from orchestrator configuration
            var engineConfig = new WorkflowEngineConfiguration
            {
                MaxConcurrentWorkflows = config.MaxConcurrentWorkflows,
                DefaultWorkflowTimeoutSeconds = config.DefaultWorkflowTimeoutSeconds,
                DefaultStepTimeoutSeconds = config.DefaultStepTimeoutSeconds,
                EnableDetailedLogging = config.EnableDetailedLogging,
                EnableMetrics = config.EnableMetrics,
                DefaultErrorHandlingMode = config.DefaultErrorHandlingMode,
                PersistWorkflowState = config.EnableWorkflowPersistence
            };
            
            // Determine engine type based on configuration or other factors
            // For now, always use StandardWorkflowEngine
            var engine = new StandardWorkflowEngine(logger, messageBus, metrics);
            engine.Configure(engineConfig);
            
            return engine;
        }
    }
}
