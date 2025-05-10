using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Configuration
{
    /// <summary>
    /// Enhanced implementation of IAgentBuilder with ML integration
    /// </summary>
    public class EnhancedAgentBuilder : IAgentBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private AgentConfiguration _agentConfiguration;
        private IMemoryService _memoryService;
        private ISafetyValidator _safetyValidator;
        private IMLModel _mlModel;
        private Dictionary<string, Func<EnvironmentState, IAction>> _rules = new();

        /// <summary>
        /// Initializes a new instance of the EnhancedAgentBuilder class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        /// <param name="logger">Logger for recording builder activities</param>
        /// <param name="configuration">Configuration for the builder</param>
        public EnhancedAgentBuilder(
            IServiceProvider serviceProvider,
            ILogger logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _safetyValidator = new EnhancedSafetyValidator(
                _logger,
                serviceProvider.GetService<IEthicsEngine>()
            );
        }

        /// <summary>
        /// Sets the configuration for the agent
        /// </summary>
        /// <param name="configuration">Configuration for the agent</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithConfiguration(AgentConfiguration configuration)
        {
            _agentConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            return this;
        }

        /// <summary>
        /// Sets the memory service for the agent
        /// </summary>
        /// <typeparam name="T">Type of memory service</typeparam>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithMemory<T>() where T : IMemoryService
        {
            _memoryService = _serviceProvider.GetService<T>();
            return this;
        }

        /// <summary>
        /// Adds a guardrail to the agent
        /// </summary>
        /// <param name="guardrail">Guardrail to add</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithGuardrail(IGuardrail guardrail)
        {
            _safetyValidator.AddGuardrail(guardrail);
            return this;
        }

        /// <summary>
        /// Sets the machine learning model for the agent
        /// </summary>
        /// <typeparam name="T">Type of ML model</typeparam>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithMLModel<T>() where T : IMLModel
        {
            _mlModel = _serviceProvider.GetService<T>();
            return this;
        }

        /// <summary>
        /// Loads rules for the agent from a file
        /// </summary>
        /// <param name="filePath">Path to the rules file</param>
        /// <returns>The builder for method chaining</returns>
        public IAgentBuilder WithRulesFromFile(string filePath)
        {
            try
            {
                // Load rules from JSON file
                var rulesJson = File.ReadAllText(filePath);
                var rulesData = JsonSerializer.Deserialize<Dictionary<string, RuleDefinition>>(rulesJson);

                foreach (var rule in rulesData)
                {
                    _rules[rule.Key] = CreateRuleFunction(rule.Value);
                }

                _logger.Log(LogLevel.Information, $"Loaded {rulesData.Count} rules from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading rules from {filePath}", ex);
                throw;
            }

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

            _memoryService ??= new ConcurrentMemoryService(_logger);
            var messageBus = _serviceProvider.GetService<IMessageBus>();
            var metrics = _serviceProvider.GetService<IMetricsCollector>();

            return _agentConfiguration.Type switch
            {
                AgentType.Reactive => BuildReactiveAgent(messageBus, metrics),
                AgentType.Deliberative => BuildDeliberativeAgent(messageBus, metrics),
                AgentType.Hybrid => BuildHybridAgent(messageBus, metrics),
                AgentType.BDI => BuildBDIAgent(messageBus, metrics),
                AgentType.UtilityBased => BuildUtilityBasedAgent(messageBus, metrics),
                _ => throw new ArgumentException($"Unknown agent type: {_agentConfiguration.Type}")
            };
        }

        private Func<EnvironmentState, IAction> CreateRuleFunction(RuleDefinition ruleDef)
        {
            return (state) =>
            {
                // Dynamic rule evaluation (simplified)
                if (EvaluateCondition(ruleDef.Condition, state))
                {
                    return CreateAction(ruleDef.Action);
                }
                return null;
            };
        }

        private bool EvaluateCondition(string condition, EnvironmentState state)
        {
            // In production, use a proper expression evaluator
            // This is a simplified version
            if (condition.Contains("input.Contains"))
            {
                var searchTerm = ExtractSearchTerm(condition);
                return state.Properties.TryGetValue("input", out var input) &&
                       input.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private string ExtractSearchTerm(string condition)
        {
            // Extract search term from condition string
            var start = condition.IndexOf("'") + 1;
            var end = condition.LastIndexOf("'");
            return condition.Substring(start, end - start);
        }

        private IAction CreateAction(ActionDefinition actionDef)
        {
            return new GenericAction(actionDef.Type, actionDef.Parameters);
        }

        private ReactiveAgent BuildReactiveAgent(IMessageBus messageBus, IMetricsCollector metrics)
        {
            // Load rules from configuration if not already loaded
            if (!_rules.Any())
            {
                var rulesConfig = _configuration.GetSection($"Agents:{_agentConfiguration.Name}:Rules");
                foreach (var rule in rulesConfig.GetChildren())
                {
                    var ruleDef = rule.Get<RuleDefinition>();
                    _rules[rule.Key] = CreateRuleFunction(ruleDef);
                }
            }

            return new ReactiveAgent(
                _agentConfiguration.Name,
                _rules,
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus,
                metrics);
        }

        private DeliberativeAgent BuildDeliberativeAgent(IMessageBus messageBus, IMetricsCollector metrics)
        {
            var planner = new SimpleRulePlanner(new List<PlanningRule>());
            var learner = new SimpleReinforcementLearner();

            if (_mlModel != null)
            {
                return new NeuralNetworkAgent(
                    _agentConfiguration.Name,
                    _mlModel,
                    new SimpleFeatureExtractor(),
                    _memoryService,
                    _safetyValidator,
                    _logger,
                    messageBus,
                    metrics);
            }

            return new DeliberativeAgent(
                _agentConfiguration.Name,
                planner,
                learner,
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus,
                metrics);
        }

        private HybridAgent BuildHybridAgent(IMessageBus messageBus, IMetricsCollector metrics)
        {
            var reactiveComponent = BuildReactiveAgent(messageBus, metrics);
            var deliberativeComponent = BuildDeliberativeAgent(messageBus, metrics);
            var reactiveThreshold = _agentConfiguration.Settings.GetValueOrDefault("reactiveThreshold", 0.7);

            return new HybridAgent(
                _agentConfiguration.Name,
                reactiveComponent,
                deliberativeComponent,
                Convert.ToDouble(reactiveThreshold),
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus,
                metrics);
        }

        private BDIAgent BuildBDIAgent(IMessageBus messageBus, IMetricsCollector metrics)
        {
            // Use the factory to create a BDI agent with default components
            return BDIAgentFactory.CreateDefaultBDIAgent(
                _agentConfiguration.Name,
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus,
                metrics);
        }

        private UtilityBasedAgent BuildUtilityBasedAgent(IMessageBus messageBus, IMetricsCollector metrics)
        {
            // Use the factory to create a utility-based agent with default components
            return UtilityBasedAgentFactory.CreateDefaultUtilityBasedAgent(
                _agentConfiguration.Name,
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus,
                metrics);
        }
    }

    /// <summary>
    /// Definition of a rule
    /// </summary>
    public class RuleDefinition
    {
        /// <summary>
        /// Condition for the rule
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Action to take when the condition is met
        /// </summary>
        public ActionDefinition Action { get; set; }
    }

    /// <summary>
    /// Definition of an action
    /// </summary>
    public class ActionDefinition
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Parameters for the action
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
    }
}
