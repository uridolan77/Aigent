using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring.Logging;

namespace Aigent.Configuration.Builders
{
    /// <summary>
    /// Enhanced implementation of IAgentBuilder with ML integration and rule-based behavior
    /// </summary>
    public class EnhancedAgentBuilder : IAgentBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly Core.IConfiguration _configuration;
        private AgentConfiguration _agentConfiguration;
        private IMemoryService _memoryService;
        private ISafetyValidator _safetyValidator;
        private IMLModel _mlModel;
        private readonly Dictionary<string, Func<EnvironmentState, IAction>> _rules = new();
        private readonly List<IGuardrail> _guardrails = new();
        private readonly Dictionary<string, object> _options = new();

        /// <summary>
        /// Initializes a new instance of the EnhancedAgentBuilder class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        /// <param name="logger">Logger for recording builder activities</param>
        /// <param name="configuration">Configuration for the builder</param>
        public EnhancedAgentBuilder(
            IServiceProvider serviceProvider,
            ILogger logger,
            Core.IConfiguration configuration)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Set default safety validator
            _safetyValidator = new EnhancedSafetyValidator(
                _logger,
                serviceProvider.GetService<IEthicsEngine>()
            );
        }

        /// <summary>
        /// Sets the configuration for the agent
        /// </summary>
        /// <param name="configuration">Agent configuration</param>
        /// <returns>The builder</returns>
        public IAgentBuilder WithConfiguration(AgentConfiguration configuration)
        {
            _agentConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger.LogInformation($"Configured agent: {configuration.Name}, Type: {configuration.Type}");
            return this;
        }

        /// <summary>
        /// Sets the memory service for the agent
        /// </summary>
        /// <typeparam name="T">Type of memory service</typeparam>
        /// <returns>The builder</returns>
        public IAgentBuilder WithMemory<T>() where T : IMemoryService
        {
            _memoryService = _serviceProvider.GetService<T>() ?? 
                ActivatorUtilities.CreateInstance<T>(_serviceProvider);
            
            _logger.LogInformation($"Using memory service: {typeof(T).Name}");
            return this;
        }

        /// <summary>
        /// Sets a specific memory service instance for the agent
        /// </summary>
        /// <param name="memoryService">Memory service to use</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithMemory(IMemoryService memoryService)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _logger.LogInformation($"Using memory service: {memoryService.GetType().Name}");
            return this;
        }

        /// <summary>
        /// Adds a guardrail to the agent
        /// </summary>
        /// <param name="guardrail">Guardrail to add</param>
        /// <returns>The builder</returns>
        public IAgentBuilder WithGuardrail(IGuardrail guardrail)
        {
            if (guardrail == null)
            {
                throw new ArgumentNullException(nameof(guardrail));
            }
            
            _guardrails.Add(guardrail);
            _logger.LogInformation($"Added guardrail: {guardrail.GetType().Name}");
            return this;
        }

        /// <summary>
        /// Sets the machine learning model for the agent
        /// </summary>
        /// <typeparam name="T">Type of ML model</typeparam>
        /// <returns>The builder</returns>
        public IAgentBuilder WithMLModel<T>() where T : IMLModel
        {
            _mlModel = _serviceProvider.GetService<T>() ?? 
                ActivatorUtilities.CreateInstance<T>(_serviceProvider);
            
            _logger.LogInformation($"Using ML model: {typeof(T).Name}");
            return this;
        }

        /// <summary>
        /// Sets a specific ML model instance for the agent
        /// </summary>
        /// <param name="mlModel">ML model to use</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithMLModel(IMLModel mlModel)
        {
            _mlModel = mlModel ?? throw new ArgumentNullException(nameof(mlModel));
            _logger.LogInformation($"Using ML model: {mlModel.GetType().Name}");
            return this;
        }

        /// <summary>
        /// Loads rules for the agent from a file
        /// </summary>
        /// <param name="filePath">Path to the rules file</param>
        /// <returns>The builder</returns>
        public IAgentBuilder WithRulesFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Rules file not found: {filePath}");
            }

            try
            {
                var rulesJson = File.ReadAllText(filePath);
                var rulesDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(rulesJson);

                foreach (var rule in rulesDictionary)
                {
                    // Simplified for the example - in reality you'd need a rule parser
                    _rules[rule.Key] = (state) => 
                    {
                        _logger.LogDebug($"Executing rule: {rule.Key}");
                        // Process the rule.Value which would be a rule definition
                        return new NoopAction(); // This would be replaced with actual rule processing
                    };
                }

                _logger.LogInformation($"Loaded {rulesDictionary.Count} rules from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load rules from {filePath}", ex);
                throw;
            }

            return this;
        }

        /// <summary>
        /// Adds a specific rule to the agent
        /// </summary>
        /// <param name="ruleName">Name of the rule</param>
        /// <param name="ruleAction">Action function that implements the rule</param>
        /// <returns>The builder</returns>
        public IAgentBuilder WithRule(string ruleName, Func<EnvironmentState, IAction> ruleAction)
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                throw new ArgumentException("Rule name cannot be null or empty", nameof(ruleName));
            }

            if (ruleAction == null)
            {
                throw new ArgumentNullException(nameof(ruleAction));
            }

            _rules[ruleName] = ruleAction;
            _logger.LogInformation($"Added rule: {ruleName}");
            return this;
        }

        /// <summary>
        /// Sets the safety validator for the agent
        /// </summary>
        /// <param name="safetyValidator">Safety validator to use</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithSafetyValidator(ISafetyValidator safetyValidator)
        {
            _safetyValidator = safetyValidator ?? throw new ArgumentNullException(nameof(safetyValidator));
            _logger.LogInformation($"Using safety validator: {safetyValidator.GetType().Name}");
            return this;
        }

        /// <summary>
        /// Sets additional configuration options for the agent
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithOption(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Option key cannot be null or empty", nameof(key));
            }

            _options[key] = value;
            _logger.LogInformation($"Set option: {key}");
            return this;
        }

        /// <summary>
        /// Builds the agent
        /// </summary>
        /// <returns>The built agent</returns>
        public IAgent Build()
        {
            if (_agentConfiguration == null)
            {
                throw new InvalidOperationException("Agent configuration is required");
            }

            _logger.LogInformation($"Building agent: {_agentConfiguration.Name}");

            // Create default memory service if none specified
            if (_memoryService == null)
            {
                _memoryService = _serviceProvider.GetService<IMemoryService>() ?? 
                    new LazyCacheMemoryService(_logger);
                
                _logger.LogInformation($"Using default memory service: {_memoryService.GetType().Name}");
            }

            // Create agent based on type
            IAgent agent = _agentConfiguration.Type switch
            {
                AgentType.Basic => new BasicAgent(
                    _agentConfiguration.Name,
                    _memoryService,
                    _safetyValidator,
                    _logger),
                
                AgentType.Advanced => new AdvancedAgent(
                    _agentConfiguration.Name,
                    _memoryService,
                    _safetyValidator,
                    _serviceProvider.GetService<IMessageBus>(),
                    _logger),
                
                AgentType.BDI => new BDIAgent(
                    _agentConfiguration.Name,
                    _memoryService,
                    _safetyValidator,
                    _mlModel,
                    _logger),
                
                _ => throw new NotSupportedException($"Agent type not supported: {_agentConfiguration.Type}")
            };

            // Apply guardrails
            foreach (var guardrail in _guardrails)
            {
                agent.AddGuardrail(guardrail);
            }

            // Apply rules
            foreach (var rule in _rules)
            {
                agent.AddRule(rule.Key, rule.Value);
            }

            // Apply additional options
            foreach (var option in _options)
            {
                agent.SetOption(option.Key, option.Value);
            }

            // Apply settings from configuration
            foreach (var setting in _agentConfiguration.Settings)
            {
                agent.SetOption(setting.Key, setting.Value);
            }

            _logger.LogInformation($"Successfully built agent: {_agentConfiguration.Name}");
            return agent;
        }
    }

    // Placeholder action class for example purposes
    internal class NoopAction : IAction
    {
        public string Name => "NoOp";

        public void Execute(EnvironmentState state)
        {
            // Do nothing
        }
    }
}
