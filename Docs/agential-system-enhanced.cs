// Enhanced Core Interfaces with Lifecycle Management
namespace AgentSystem.Core
{
    // Enhanced Agent Interface with Lifecycle Management
    public interface IAgent : IDisposable
    {
        string Id { get; }
        string Name { get; }
        AgentType Type { get; }
        AgentCapabilities Capabilities { get; }
        Task Initialize();
        Task<IAction> DecideAction(EnvironmentState state);
        Task Learn(EnvironmentState state, IAction action, ActionResult result);
        Task Shutdown();
    }

    public class AgentCapabilities
    {
        public List<string> SupportedActionTypes { get; set; } = new();
        public Dictionary<string, double> SkillLevels { get; set; } = new();
        public double LoadFactor { get; set; }
        public double HistoricalPerformance { get; set; }
    }

    public abstract class BaseAgent : IAgent
    {
        public string Id { get; protected set; } = Guid.NewGuid().ToString();
        public string Name { get; protected set; }
        public abstract AgentType Type { get; }
        public AgentCapabilities Capabilities { get; protected set; } = new();

        protected readonly IMemoryService _memory;
        protected readonly ISafetyValidator _safetyValidator;
        protected readonly ILogger _logger;
        protected readonly IMessageBus _messageBus;
        private bool _disposed;

        protected BaseAgent(
            IMemoryService memory, 
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus)
        {
            _memory = memory;
            _safetyValidator = safetyValidator;
            _logger = logger;
            _messageBus = messageBus;
        }

        public virtual async Task Initialize()
        {
            await _memory.Initialize(Id);
            _messageBus.Subscribe($"agent.{Id}.command", HandleMessage);
            _logger.Log($"Agent {Name} initialized");
        }

        public abstract Task<IAction> DecideAction(EnvironmentState state);
        
        public virtual async Task Learn(EnvironmentState state, IAction action, ActionResult result)
        {
            await Task.CompletedTask;
        }

        public virtual async Task Shutdown()
        {
            _logger.Log($"Shutting down agent {Name}");
            await _memory.Flush();
            _messageBus.Unsubscribe($"agent.{Id}.command", HandleMessage);
        }

        protected virtual void HandleMessage(object message)
        {
            _logger.Log($"Agent {Name} received message: {message}");
        }

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

// Enhanced Memory Services with Persistence Options
namespace AgentSystem.Memory
{
    public interface IMemoryService
    {
        Task Initialize(string agentId);
        Task StoreContext(string key, object value, TimeSpan? ttl = null);
        Task<T> RetrieveContext<T>(string key);
        Task ClearMemory();
        Task Flush();
    }

    public interface IShortTermMemory : IMemoryService { }
    public interface ILongTermMemory : IMemoryService { }

    // Redis-based Memory Service
    public class RedisMemoryService : ILongTermMemory
    {
        private readonly string _connectionString;
        private string _agentId;
        // In production, would use StackExchange.Redis

        public RedisMemoryService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Task Initialize(string agentId)
        {
            _agentId = agentId;
            // Connect to Redis
            return Task.CompletedTask;
        }

        public async Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            var fullKey = $"{_agentId}:{key}";
            var serialized = JsonSerializer.Serialize(value);
            // Store in Redis with optional TTL
            await Task.CompletedTask;
        }

        public async Task<T> RetrieveContext<T>(string key)
        {
            var fullKey = $"{_agentId}:{key}";
            // Retrieve from Redis
            return default;
        }

        public Task ClearMemory()
        {
            // Clear all keys for agent
            return Task.CompletedTask;
        }

        public Task Flush()
        {
            // Ensure all writes are persisted
            return Task.CompletedTask;
        }
    }

    // SQL-based Memory Service
    public class SqlMemoryService : ILongTermMemory
    {
        private readonly string _connectionString;
        private string _agentId;

        public SqlMemoryService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task Initialize(string agentId)
        {
            _agentId = agentId;
            // Initialize database connection
            await CreateTablesIfNotExist();
        }

        private async Task CreateTablesIfNotExist()
        {
            // Create memory tables if they don't exist
            await Task.CompletedTask;
        }

        public async Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            var fullKey = $"{_agentId}:{key}";
            var serialized = JsonSerializer.Serialize(value);
            // Store in SQL database
            await Task.CompletedTask;
        }

        public async Task<T> RetrieveContext<T>(string key)
        {
            var fullKey = $"{_agentId}:{key}";
            // Retrieve from SQL database
            return default;
        }

        public async Task ClearMemory()
        {
            // Clear all records for agent
            await Task.CompletedTask;
        }

        public async Task Flush()
        {
            // Commit any pending transactions
            await Task.CompletedTask;
        }
    }

    // Thread-safe in-memory implementation
    public class ConcurrentMemoryService : IShortTermMemory
    {
        private readonly ConcurrentDictionary<string, MemoryEntry> _memory = new();
        private string _agentId;

        public Task Initialize(string agentId)
        {
            _agentId = agentId;
            return Task.CompletedTask;
        }

        public async Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            var fullKey = $"{_agentId}:{key}";
            var entry = new MemoryEntry
            {
                Value = value,
                Expiry = ttl.HasValue ? DateTime.UtcNow + ttl.Value : DateTime.MaxValue
            };
            _memory.AddOrUpdate(fullKey, entry, (k, v) => entry);
            await Task.CompletedTask;
        }

        public async Task<T> RetrieveContext<T>(string key)
        {
            var fullKey = $"{_agentId}:{key}";
            if (_memory.TryGetValue(fullKey, out var entry) && entry.Expiry > DateTime.UtcNow)
            {
                return (T)entry.Value;
            }
            return default;
        }

        public Task ClearMemory()
        {
            _memory.Clear();
            return Task.CompletedTask;
        }

        public Task Flush()
        {
            // Clean up expired entries
            var expiredKeys = _memory.Where(kv => kv.Value.Expiry <= DateTime.UtcNow)
                                    .Select(kv => kv.Key)
                                    .ToList();
            foreach (var key in expiredKeys)
            {
                _memory.TryRemove(key, out _);
            }
            return Task.CompletedTask;
        }

        private class MemoryEntry
        {
            public object Value { get; set; }
            public DateTime Expiry { get; set; }
        }
    }
}

// Enhanced Safety Framework with Context-Aware Ethics
namespace AgentSystem.Safety
{
    public interface ISafetyValidator
    {
        Task<ValidationResult> ValidateAction(IAction action);
        void AddGuardrail(IGuardrail guardrail);
        void AddActionTypeRestriction(string actionType);
    }

    public class EnhancedSafetyValidator : ISafetyValidator
    {
        private readonly List<IGuardrail> _guardrails = new();
        private readonly HashSet<string> _restrictedActionTypes = new();
        private readonly ILogger _logger;
        private readonly IEthicsEngine _ethicsEngine;

        public EnhancedSafetyValidator(ILogger logger, IEthicsEngine ethicsEngine)
        {
            _logger = logger;
            _ethicsEngine = ethicsEngine;
        }

        public void AddGuardrail(IGuardrail guardrail)
        {
            _guardrails.Add(guardrail);
            _logger.Log($"Added guardrail: {guardrail.Name}");
        }

        public void AddActionTypeRestriction(string actionType)
        {
            _restrictedActionTypes.Add(actionType);
            _logger.Log($"Restricted action type: {actionType}");
        }

        public async Task<ValidationResult> ValidateAction(IAction action)
        {
            // Check restricted action types
            if (_restrictedActionTypes.Contains(action.ActionType))
            {
                return ValidationResult.Failure($"Action type '{action.ActionType}' is restricted");
            }

            var violations = new List<string>();

            // Run through all guardrails
            foreach (var guardrail in _guardrails)
            {
                var result = await guardrail.Validate(action);
                if (!result.IsValid)
                {
                    violations.AddRange(result.Violations);
                    _logger.LogWarning($"Guardrail {guardrail.Name} validation failed: {result.Message}");
                }
            }

            if (violations.Any())
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Action validation failed",
                    Violations = violations
                };
            }

            return ValidationResult.Success();
        }
    }

    // Enhanced Ethical Guardrail
    public interface IEthicsEngine
    {
        Task<bool> ValidateAsync(Dictionary<string, object> parameters);
    }

    public class EthicalConstraintGuardrail : IGuardrail
    {
        public string Name => "Ethical Constraints";
        private readonly IEthicsEngine _ethicsEngine;
        private readonly List<string> _ethicalGuidelines;

        public EthicalConstraintGuardrail(IEthicsEngine ethicsEngine, List<string> ethicalGuidelines)
        {
            _ethicsEngine = ethicsEngine;
            _ethicalGuidelines = ethicalGuidelines;
        }

        public async Task<ValidationResult> Validate(IAction action)
        {
            var isValid = await _ethicsEngine.ValidateAsync(action.Parameters);
            
            if (!isValid)
            {
                return ValidationResult.Failure("Action violates ethical guidelines");
            }

            return ValidationResult.Success();
        }
    }

    // NLP-based Context-Aware Ethics Engine
    public class NlpEthicsEngine : IEthicsEngine
    {
        private readonly INlpService _nlpService;

        public NlpEthicsEngine(INlpService nlpService)
        {
            _nlpService = nlpService;
        }

        public async Task<bool> ValidateAsync(Dictionary<string, object> parameters)
        {
            // Extract text content from parameters
            var content = ExtractTextContent(parameters);
            
            // Analyze intent using NLP
            var intent = await _nlpService.AnalyzeIntent(content);
            
            // Check if intent is ethical
            return IsEthicalIntent(intent);
        }

        private string ExtractTextContent(Dictionary<string, object> parameters)
        {
            return string.Join(" ", parameters.Values
                .Where(v => v is string)
                .Cast<string>());
        }

        private bool IsEthicalIntent(IntentAnalysisResult intent)
        {
            // Check against ethical guidelines
            return !intent.IsHarmful && intent.EthicalScore > 0.7;
        }
    }

    public interface INlpService
    {
        Task<IntentAnalysisResult> AnalyzeIntent(string text);
    }

    public class IntentAnalysisResult
    {
        public bool IsHarmful { get; set; }
        public double EthicalScore { get; set; }
        public List<string> DetectedIntents { get; set; } = new();
    }
}

// Enhanced Agent Builder with ML Integration
namespace AgentSystem.Configuration
{
    public interface IAgentBuilder
    {
        IAgentBuilder WithConfiguration(AgentConfiguration configuration);
        IAgentBuilder WithMemory<T>() where T : IMemoryService;
        IAgentBuilder WithGuardrail(IGuardrail guardrail);
        IAgentBuilder WithMLModel<T>() where T : IMLModel;
        IAgentBuilder WithRulesFromFile(string filePath);
        IAgent Build();
    }

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

        public EnhancedAgentBuilder(
            IServiceProvider serviceProvider, 
            ILogger logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _safetyValidator = new EnhancedSafetyValidator(
                _logger, 
                serviceProvider.GetService<IEthicsEngine>()
            );
        }

        public IAgentBuilder WithConfiguration(AgentConfiguration configuration)
        {
            _agentConfiguration = configuration;
            return this;
        }

        public IAgentBuilder WithMemory<T>() where T : IMemoryService
        {
            _memoryService = _serviceProvider.GetService<T>();
            return this;
        }

        public IAgentBuilder WithGuardrail(IGuardrail guardrail)
        {
            _safetyValidator.AddGuardrail(guardrail);
            return this;
        }

        public IAgentBuilder WithMLModel<T>() where T : IMLModel
        {
            _mlModel = _serviceProvider.GetService<T>();
            return this;
        }

        public IAgentBuilder WithRulesFromFile(string filePath)
        {
            // Load rules from JSON/YAML file
            var rulesJson = File.ReadAllText(filePath);
            var rulesData = JsonSerializer.Deserialize<Dictionary<string, RuleDefinition>>(rulesJson);
            
            foreach (var rule in rulesData)
            {
                _rules[rule.Key] = CreateRuleFunction(rule.Value);
            }
            
            return this;
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
                       input.ToString().Contains(searchTerm);
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

        public IAgent Build()
        {
            _memoryService ??= new ConcurrentMemoryService();
            var messageBus = _serviceProvider.GetService<IMessageBus>();

            return _agentConfiguration.Type switch
            {
                AgentType.Reactive => BuildReactiveAgent(messageBus),
                AgentType.Deliberative => BuildDeliberativeAgent(messageBus),
                AgentType.Hybrid => BuildHybridAgent(messageBus),
                _ => throw new ArgumentException($"Unknown agent type: {_agentConfiguration.Type}")
            };
        }

        private ReactiveAgent BuildReactiveAgent(IMessageBus messageBus)
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
                messageBus);
        }

        private DeliberativeAgent BuildDeliberativeAgent(IMessageBus messageBus)
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
                    messageBus);
            }

            return new DeliberativeAgent(
                _agentConfiguration.Name,
                planner,
                learner,
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus);
        }

        private HybridAgent BuildHybridAgent(IMessageBus messageBus)
        {
            var reactiveComponent = BuildReactiveAgent(messageBus);
            var deliberativeComponent = BuildDeliberativeAgent(messageBus);
            var reactiveThreshold = _agentConfiguration.Settings.GetValueOrDefault("reactiveThreshold", 0.7);

            return new HybridAgent(
                _agentConfiguration.Name,
                reactiveComponent,
                deliberativeComponent,
                Convert.ToDouble(reactiveThreshold),
                _memoryService,
                _safetyValidator,
                _logger,
                messageBus);
        }
    }

    public class RuleDefinition
    {
        public string Condition { get; set; }
        public ActionDefinition Action { get; set; }
    }

    public class ActionDefinition
    {
        public string Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}

// Enhanced Orchestration with Message Bus
namespace AgentSystem.Communication
{
    public interface IMessageBus
    {
        void Subscribe(string topic, Action<object> handler);
        void Unsubscribe(string topic, Action<object> handler);
        Task PublishAsync(string topic, object message);
    }

    public class InMemoryMessageBus : IMessageBus
    {
        private readonly ConcurrentDictionary<string, List<Action<object>>> _subscriptions = new();
        private readonly ILogger _logger;

        public InMemoryMessageBus(ILogger logger)
        {
            _logger = logger;
        }

        public void Subscribe(string topic, Action<object> handler)
        {
            _subscriptions.AddOrUpdate(
                topic,
                new List<Action<object>> { handler },
                (_, handlers) => 
                {
                    handlers.Add(handler);
                    return handlers;
                });
            
            _logger.Log($"Subscribed to topic: {topic}");
        }

        public void Unsubscribe(string topic, Action<object> handler)
        {
            if (_subscriptions.TryGetValue(topic, out var handlers))
            {
                handlers.Remove(handler);
                if (!handlers.Any())
                {
                    _subscriptions.TryRemove(topic, out _);
                }
            }
            
            _logger.Log($"Unsubscribed from topic: {topic}");
        }

        public async Task PublishAsync(string topic, object message)
        {
            if (_subscriptions.TryGetValue(topic, out var handlers))
            {
                var tasks = handlers.Select(handler => 
                    Task.Run(() => 
                    {
                        try
                        {
                            handler(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error in message handler for topic '{topic}'", ex);
                        }
                    }));
                
                await Task.WhenAll(tasks);
                _logger.Log($"Published message to topic: {topic}");
            }
        }
    }
}

// Enhanced Orchestration with Complete Workflow Implementation
namespace AgentSystem.Orchestration
{
    public interface IOrchestrator
    {
        Task RegisterAgent(IAgent agent);
        Task UnregisterAgent(string agentId);
        Task<IAgent> AssignTask(string task);
        Task<WorkflowResult> ExecuteWorkflow(WorkflowDefinition workflow);
    }

    public class EnhancedOrchestrator : IOrchestrator
    {
        private readonly Dictionary<string, IAgent> _agents = new();
        private readonly ILogger _logger;
        private readonly ISafetyValidator _safetyValidator;
        private readonly IMessageBus _messageBus;

        public EnhancedOrchestrator(
            ILogger logger, 
            ISafetyValidator safetyValidator,
            IMessageBus messageBus)
        {
            _logger = logger;
            _safetyValidator = safetyValidator;
            _messageBus = messageBus;
        }

        public Task RegisterAgent(IAgent agent)
        {
            _agents[agent.Id] = agent;
            _logger.Log($"Registered agent: {agent.Name} ({agent.Id})");
            return Task.CompletedTask;
        }

        public Task UnregisterAgent(string agentId)
        {
            if (_agents.Remove(agentId))
            {
                _logger.Log($"Unregistered agent: {agentId}");
            }
            return Task.CompletedTask;
        }

        public async Task<IAgent> AssignTask(string task)
        {
            var bestAgent = await SelectBestAgent(task, _agents.Values.ToList());
            _logger.Log($"Assigned task '{task}' to agent '{bestAgent.Name}'");
            return bestAgent;
        }

        public async Task<WorkflowResult> ExecuteWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();

            try
            {
                switch (workflow.Type)
                {
                    case WorkflowType.Sequential:
                        result = await ExecuteSequentialWorkflow(workflow);
                        break;
                    case WorkflowType.Parallel:
                        result = await ExecuteParallelWorkflow(workflow);
                        break;
                    case WorkflowType.Conditional:
                        result = await ExecuteConditionalWorkflow(workflow);
                        break;
                    case WorkflowType.Hierarchical:
                        result = await ExecuteHierarchicalWorkflow(workflow);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Workflow execution error: {ex.Message}");
                _logger.LogError($"Workflow '{workflow.Name}' failed", ex);
            }

            return result;
        }

        private async Task<IAgent> SelectBestAgent(string task, List<IAgent> availableAgents)
        {
            // Score agents based on capabilities, load, and historical performance
            var scores = new Dictionary<IAgent, double>();

            foreach (var agent in availableAgents)
            {
                double score = 0;
                
                // Check if agent supports the required action types
                if (TaskRequiresActionType(task, out var requiredActionTypes))
                {
                    var supportedCount = requiredActionTypes
                        .Count(at => agent.Capabilities.SupportedActionTypes.Contains(at));
                    score += supportedCount * 10;
                }

                // Factor in skill levels
                var relevantSkill = GetRelevantSkill(task);
                if (agent.Capabilities.SkillLevels.TryGetValue(relevantSkill, out var skillLevel))
                {
                    score += skillLevel * 5;
                }

                // Consider load factor (lower is better)
                score -= agent.Capabilities.LoadFactor * 2;

                // Add historical performance
                score += agent.Capabilities.HistoricalPerformance * 3;

                scores[agent] = score;
            }

            return scores.OrderByDescending(kv => kv.Value).First().Key;
        }

        private bool TaskRequiresActionType(string task, out List<string> actionTypes)
        {
            // Analyze task to determine required action types
            actionTypes = new List<string>();
            
            if (task.Contains("weather"))
                actionTypes.Add("WeatherQuery");
            if (task.Contains("plan"))
                actionTypes.Add("Planning");
            if (task.Contains("urgent"))
                actionTypes.Add("ReactiveResponse");
            
            return actionTypes.Any();
        }

        private string GetRelevantSkill(string task)
        {
            // Determine relevant skill based on task
            if (task.Contains("weather")) return "weather_analysis";
            if (task.Contains("plan")) return "planning";
            if (task.Contains("urgent")) return "quick_response";
            return "general";
        }

        private async Task<WorkflowResult> ExecuteConditionalWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();
            var context = new WorkflowContext();

            foreach (var step in workflow.Steps)
            {
                // Evaluate condition for this step
                if (await EvaluateStepCondition(step, context))
                {
                    var agent = await SelectAgentForStep(step);
                    var stepResult = await ExecuteWorkflowStep(agent, step, context.Results);
                    context.Results[step.Name] = stepResult;
                }
            }

            result.Success = true;
            result.Results = context.Results;
            return result;
        }

        private async Task<WorkflowResult> ExecuteHierarchicalWorkflow(WorkflowDefinition workflow)
        {
            var result = new WorkflowResult();
            
            // Find root steps (no dependencies)
            var rootSteps = workflow.Steps.Where(s => !s.Dependencies.Any()).ToList();
            
            // Execute workflow hierarchically
            foreach (var rootStep in rootSteps)
            {
                var subResult = await ExecuteStepHierarchy(rootStep, workflow.Steps);
                result.Results[rootStep.Name] = subResult;
            }

            result.Success = true;
            return result;
        }

        private async Task<object> ExecuteStepHierarchy(WorkflowStep step, List<WorkflowStep> allSteps)
        {
            // Execute current step
            var agent = await SelectAgentForStep(step);
            var stepResult = await ExecuteWorkflowStep(agent, step, new Dictionary<string, object>());
            
            // Find child steps
            var childSteps = allSteps.Where(s => s.Dependencies.Contains(step.Name)).ToList();
            
            // Execute child steps
            var childResults = new Dictionary<string, object>();
            foreach (var childStep in childSteps)
            {
                childResults[childStep.Name] = await ExecuteStepHierarchy(childStep, allSteps);
            }
            
            return new { StepResult = stepResult, ChildResults = childResults };
        }

        private async Task<bool> EvaluateStepCondition(WorkflowStep step, WorkflowContext context)
        {
            // Evaluate step condition based on context
            if (step.Parameters.TryGetValue("condition", out var condition))
            {
                // In production, use a proper expression evaluator
                return true; // Simplified for demo
            }
            return true;
        }

        private async Task<IAgent> SelectAgentForStep(WorkflowStep step)
        {
            var candidates = _agents.Values.Where(a => a.Type == step.RequiredAgentType).ToList();
            return await SelectBestAgent(step.Name, candidates);
        }

        private async Task<object> ExecuteWorkflowStep(IAgent agent, WorkflowStep step, Dictionary<string, object> previousResults)
        {
            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>(step.Parameters)
            };

            // Add dependencies from previous results
            foreach (var dependency in step.Dependencies)
            {
                if (previousResults.TryGetValue(dependency, out var dependencyResult))
                {
                    state.Properties[$"dep_{dependency}"] = dependencyResult;
                }
            }

            var action = await agent.DecideAction(state);
            
            // Publish step completion event
            await _messageBus.PublishAsync($"workflow.step.completed", new
            {
                StepName = step.Name,
                AgentId = agent.Id,
                Action = action
            });
            
            return action;
        }

        private class WorkflowContext
        {
            public Dictionary<string, object> Results { get; } = new();
        }
    }
}

// Enhanced Testing Framework with Parallel Execution
namespace AgentSystem.Testing
{
    public class ParallelTestRunner
    {
        private readonly ILogger _logger;
        private readonly List<IAgentTest> _tests = new();
        private readonly int _maxDegreeOfParallelism;

        public ParallelTestRunner(ILogger logger, int maxDegreeOfParallelism = -1)
        {
            _logger = logger;
            _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? 
                maxDegreeOfParallelism : Environment.ProcessorCount;
        }

        public void AddTest(IAgentTest test)
        {
            _tests.Add(test);
        }

        public async Task<TestSuiteResult> RunTestsAsync(IAgent agent)
        {
            var suiteResult = new TestSuiteResult
            {
                AgentName = agent.Name,
                StartTime = DateTime.UtcNow
            };

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism
            };

            var results = new ConcurrentDictionary<string, TestResult>();

            await Parallel.ForEachAsync(_tests, options, async (test, cancellationToken) =>
            {
                _logger.Log($"Running test: {test.Name}");
                var result = await test.Run(agent);
                results[test.Name] = result;
                _logger.Log($"Test {test.Name} {(result.Passed ? "PASSED" : "FAILED")}: {result.Message}");
            });

            suiteResult.TestResults = results.ToDictionary(kv => kv.Key, kv => kv.Value);
            suiteResult.EndTime = DateTime.UtcNow;
            suiteResult.TotalDuration = suiteResult.EndTime - suiteResult.StartTime;
            suiteResult.PassedCount = suiteResult.TestResults.Count(r => r.Value.Passed);
            suiteResult.FailedCount = suiteResult.TestResults.Count(r => !r.Value.Passed);

            return suiteResult;
        }
    }

    // Integration Test
    public class MultiAgentIntegrationTest : IAgentTest
    {
        public string Name => "Multi-Agent Integration Test";
        private readonly IOrchestrator _orchestrator;
        private readonly List<IAgent> _agents;

        public MultiAgentIntegrationTest(IOrchestrator orchestrator, List<IAgent> agents)
        {
            _orchestrator = orchestrator;
            _agents = agents;
        }

        public async Task<TestResult> Run(IAgent agent)
        {
            var result = new TestResult();

            try
            {
                // Register all agents
                foreach (var a in _agents)
                {
                    await _orchestrator.RegisterAgent(a);
                }

                // Create a workflow that requires multiple agents
                var workflow = new WorkflowDefinition
                {
                    Name = "Integration Test Workflow",
                    Type = WorkflowType.Sequential,
                    Steps = new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Name = "Step1",
                            RequiredAgentType = AgentType.Reactive,
                            Parameters = new Dictionary<string, object> { ["input"] = "test" }
                        },
                        new WorkflowStep
                        {
                            Name = "Step2",
                            RequiredAgentType = AgentType.Deliberative,
                            Parameters = new Dictionary<string, object> { ["plan"] = "integration" },
                            Dependencies = new List<string> { "Step1" }
                        }
                    }
                };

                var workflowResult = await _orchestrator.ExecuteWorkflow(workflow);
                result.Passed = workflowResult.Success;
                result.Message = workflowResult.Success ? 
                    "Integration test passed" : 
                    $"Integration test failed: {string.Join(", ", workflowResult.Errors)}";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Message = $"Integration test failed: {ex.Message}";
            }

            return result;
        }
    }

    // Chaos Test
    public class ChaosTest : IAgentTest
    {
        public string Name => "Chaos Test";
        private readonly Random _random = new();

        public async Task<TestResult> Run(IAgent agent)
        {
            var result = new TestResult();

            try
            {
                // Simulate various failure scenarios
                var scenarios = new[]
                {
                    SimulateNetworkFailure,
                    SimulateHighLoad,
                    SimulateInvalidInput,
                    SimulateMemoryPressure
                };

                foreach (var scenario in scenarios)
                {
                    await scenario(agent);
                }

                result.Passed = true;
                result.Message = "Agent survived chaos testing";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Message = $"Agent failed chaos test: {ex.Message}";
            }

            return result;
        }

        private async Task SimulateNetworkFailure(IAgent agent)
        {
            // Simulate network timeout
            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["network_status"] = "disconnected"
                }
            };

            await agent.DecideAction(state);
        }

        private async Task SimulateHighLoad(IAgent agent)
        {
            // Send many requests concurrently
            var tasks = Enumerable.Range(0, 100).Select(i =>
                agent.DecideAction(new EnvironmentState
                {
                    Properties = new Dictionary<string, object> { ["request_id"] = i }
                })
            );

            await Task.WhenAll(tasks);
        }

        private async Task SimulateInvalidInput(IAgent agent)
        {
            // Send malformed input
            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = null,
                    ["malformed_data"] = new byte[] { 0xFF, 0xFE, 0xFD }
                }
            };

            await agent.DecideAction(state);
        }

        private async Task SimulateMemoryPressure(IAgent agent)
        {
            // Create large objects to pressure memory
            var largeData = new byte[50_000_000]; // 50MB
            _random.NextBytes(largeData);

            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["large_data"] = largeData
                }
            };

            await agent.DecideAction(state);
        }
    }
}

// Production-Grade Monitoring
namespace AgentSystem.Monitoring
{
    public interface IMetricsCollector
    {
        void RecordMetric(string name, double value, Dictionary<string, string> tags = null);
        Task<MetricsSummary> GetSummary(TimeSpan duration);
        void StartOperation(string operationName);
        void EndOperation(string operationName);
    }

    // Application Insights Integration
    public class ApplicationInsightsMetricsCollector : IMetricsCollector
    {
        private readonly string _instrumentationKey;
        private readonly Dictionary<string, DateTime> _operations = new();
        
        public ApplicationInsightsMetricsCollector(string instrumentationKey)
        {
            _instrumentationKey = instrumentationKey;
            // Initialize Application Insights
        }

        public void RecordMetric(string name, double value, Dictionary<string, string> tags = null)
        {
            // Send metric to Application Insights
        }

        public void StartOperation(string operationName)
        {
            _operations[operationName] = DateTime.UtcNow;
        }

        public void EndOperation(string operationName)
        {
            if (_operations.TryGetValue(operationName, out var startTime))
            {
                var duration = DateTime.UtcNow - startTime;
                RecordMetric($"operation.{operationName}.duration", duration.TotalMilliseconds);
                _operations.Remove(operationName);
            }
        }

        public async Task<MetricsSummary> GetSummary(TimeSpan duration)
        {
            // Query Application Insights for metrics
            return new MetricsSummary();
        }
    }

    // OpenTelemetry Integration
    public class OpenTelemetryMetricsCollector : IMetricsCollector
    {
        // Implementation for OpenTelemetry
        public void RecordMetric(string name, double value, Dictionary<string, string> tags = null)
        {
            // Send metric through OpenTelemetry
        }

        public void StartOperation(string operationName)
        {
            // Start OpenTelemetry span
        }

        public void EndOperation(string operationName)
        {
            // End OpenTelemetry span
        }

        public async Task<MetricsSummary> GetSummary(TimeSpan duration)
        {
            // Query metrics from OpenTelemetry backend
            return new MetricsSummary();
        }
    }
}

// Security and API Integration
namespace AgentSystem.Security
{
    public interface ISecretManager
    {
        Task<string> GetSecretAsync(string secretName);
        Task SetSecretAsync(string secretName, string secretValue);
    }

    // Azure Key Vault Integration
    public class AzureKeyVaultSecretManager : ISecretManager
    {
        private readonly string _vaultUrl;
        
        public AzureKeyVaultSecretManager(string vaultUrl)
        {
            _vaultUrl = vaultUrl;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            // Retrieve secret from Azure Key Vault
            return "secret-value";
        }

        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            // Store secret in Azure Key Vault
        }
    }

    // OAuth2 Authentication
    public interface IOAuth2Provider
    {
        Task<string> GetAccessTokenAsync(string scope);
        Task<bool> ValidateTokenAsync(string token);
    }

    public class OAuth2Provider : IOAuth2Provider
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenEndpoint;

        public OAuth2Provider(HttpClient httpClient, string clientId, string clientSecret, string tokenEndpoint)
        {
            _httpClient = httpClient;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tokenEndpoint = tokenEndpoint;
        }

        public async Task<string> GetAccessTokenAsync(string scope)
        {
            // Implement OAuth2 token retrieval
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["scope"] = scope
            };

            var response = await _httpClient.PostAsync(_tokenEndpoint, 
                new FormUrlEncodedContent(tokenRequest));
            
            // Parse response and return access token
            return "access-token";
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            // Validate token
            return true;
        }
    }

    // GDPR-compliant data handling
    public interface IDataAnonymizer
    {
        Task<T> AnonymizeAsync<T>(T data);
        Task<T> DeanonymizeAsync<T>(T data, string key);
    }

    public class GdprDataAnonymizer : IDataAnonymizer
    {
        private readonly Dictionary<string, string> _anonymizationMap = new();

        public async Task<T> AnonymizeAsync<T>(T data)
        {
            // Implement data anonymization logic
            if (data is Dictionary<string, object> dict)
            {
                var anonymized = new Dictionary<string, object>();
                foreach (var kv in dict)
                {
                    if (IsSensitiveField(kv.Key))
                    {
                        anonymized[kv.Key] = AnonymizeValue(kv.Value);
                    }
                    else
                    {
                        anonymized[kv.Key] = kv.Value;
                    }
                }
                return (T)(object)anonymized;
            }
            return data;
        }

        public async Task<T> DeanonymizeAsync<T>(T data, string key)
        {
            // Implement de-anonymization logic
            return data;
        }

        private bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[] { "email", "ssn", "phone", "address", "name" };
            return sensitiveFields.Any(f => fieldName.ToLower().Contains(f));
        }

        private object AnonymizeValue(object value)
        {
            if (value is string str)
            {
                var hash = ComputeHash(str);
                _anonymizationMap[hash] = str;
                return hash;
            }
            return value;
        }

        private string ComputeHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}

// Real-world API Integration Example
namespace AgentSystem.Examples
{
    public class EnhancedWeatherAgent : BaseAgent
    {
        public override AgentType Type => AgentType.Reactive;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecretManager _secretManager;
        private readonly Polly.CircuitBreaker.AsyncCircuitBreakerPolicy _circuitBreaker;

        public EnhancedWeatherAgent(
            string name,
            IHttpClientFactory httpClientFactory,
            ISecretManager secretManager,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus)
            : base(memory, safetyValidator, logger, messageBus)
        {
            Name = name;
            _httpClientFactory = httpClientFactory;
            _secretManager = secretManager;
            
            // Configure circuit breaker with Polly
            _circuitBreaker = Polly.Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(1));
        }

        public override async Task<IAction> DecideAction(EnvironmentState state)
        {
            if (state.Properties.TryGetValue("query", out var query))
            {
                var location = ExtractLocation(query.ToString());
                if (!string.IsNullOrEmpty(location))
                {
                    try
                    {
                        var weather = await GetWeatherDataWithRetry(location);
                        return new TextOutputAction($"The weather in {location} is: {weather}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to get weather data: {ex.Message}");
                        return new TextOutputAction($"Sorry, I couldn't get weather data for {location} right now.");
                    }
                }
            }

            return new TextOutputAction("Please specify a location for weather information.");
        }

        private async Task<string> GetWeatherDataWithRetry(string location)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var httpClient = _httpClientFactory.CreateClient("WeatherApi");
                var apiKey = await _secretManager.GetSecretAsync("OpenWeatherMapApiKey");
                
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={location}&appid={apiKey}&units=metric";
                
                var response = await httpClient.GetStringAsync(url);
                var weatherData = JsonSerializer.Deserialize<WeatherData>(response);
                
                return $"{weatherData.Weather[0].Main}, {weatherData.Main.Temp}°C, " +
                       $"Humidity: {weatherData.Main.Humidity}%, " +
                       $"Wind: {weatherData.Wind.Speed} m/s";
            });
        }

        private string ExtractLocation(string query)
        {
            // Improved location extraction using NLP or regex
            var patterns = new[]
            {
                @"weather in (\w+)",
                @"forecast for (\w+)",
                @"temperature in (\w+)",
                @"how's the weather in (\w+)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(query, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        private class WeatherData
        {
            public List<WeatherInfo> Weather { get; set; }
            public MainInfo Main { get; set; }
            public WindInfo Wind { get; set; }
        }

        private class WeatherInfo
        {
            public string Main { get; set; }
            public string Description { get; set; }
        }

        private class MainInfo
        {
            public double Temp { get; set; }
            public int Humidity { get; set; }
        }

        private class WindInfo
        {
            public double Speed { get; set; }
        }
    }
}

// Deployment Configuration
namespace AgentSystem.Deployment
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAgentSystem(this IServiceCollection services, IConfiguration configuration)
        {
            // Core services
            services.AddSingleton<ILogger, ConsoleLogger>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            
            // Memory services
            var memoryType = configuration["AgentSystem:MemoryType"];
            switch (memoryType)
            {
                case "Redis":
                    services.AddSingleton<ILongTermMemory>(sp => 
                        new RedisMemoryService(configuration["AgentSystem:Redis:ConnectionString"]));
                    break;
                case "SQL":
                    services.AddSingleton<ILongTermMemory>(sp => 
                        new SqlMemoryService(configuration["AgentSystem:SQL:ConnectionString"]));
                    break;
                default:
                    services.AddSingleton<ILongTermMemory, ConcurrentMemoryService>();
                    break;
            }
            
            services.AddSingleton<IShortTermMemory, ConcurrentMemoryService>();
            
            // Safety and ethics
            services.AddSingleton<ISafetyValidator, EnhancedSafetyValidator>();
            services.AddSingleton<IEthicsEngine, NlpEthicsEngine>();
            services.AddSingleton<INlpService, MockNlpService>(); // Replace with real implementation
            
            // Security
            services.AddSingleton<ISecretManager>(sp => 
                new AzureKeyVaultSecretManager(configuration["AgentSystem:KeyVault:Url"]));
            services.AddSingleton<IOAuth2Provider>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new OAuth2Provider(
                    httpClient,
                    configuration["AgentSystem:OAuth:ClientId"],
                    configuration["AgentSystem:OAuth:ClientSecret"],
                    configuration["AgentSystem:OAuth:TokenEndpoint"]);
            });
            services.AddSingleton<IDataAnonymizer, GdprDataAnonymizer>();
            
            // Monitoring
            var monitoringType = configuration["AgentSystem:Monitoring:Type"];
            switch (monitoringType)
            {
                case "ApplicationInsights":
                    services.AddSingleton<IMetricsCollector>(sp => 
                        new ApplicationInsightsMetricsCollector(
                            configuration["AgentSystem:Monitoring:InstrumentationKey"]));
                    break;
                case "OpenTelemetry":
                    services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();
                    break;
                default:
                    services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();
                    break;
            }
            
            // Orchestration
            services.AddSingleton<IOrchestrator, EnhancedOrchestrator>();
            
            // Agent builder
            services.AddTransient<IAgentBuilder, EnhancedAgentBuilder>();
            
            // HTTP client configuration
            services.AddHttpClient("WeatherApi", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "AgentSystem/1.0");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
            
            return services;
        }

        private static Polly.IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Polly.Extensions.Http.HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private static Polly.IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return Polly.Extensions.Http.HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));
        }
    }

    // Mock NLP Service (for demo)
    public class MockNlpService : INlpService
    {
        public async Task<IntentAnalysisResult> AnalyzeIntent(string text)
        {
            // Mock implementation
            return new IntentAnalysisResult
            {
                IsHarmful = text.Contains("harmful") || text.Contains("dangerous"),
                EthicalScore = text.Contains("help") ? 0.9 : 0.5,
                DetectedIntents = new List<string> { "general_query" }
            };
        }
    }
}

// Configuration Files
namespace AgentSystem.Configuration
{
    public static class ConfigurationFiles
    {
        public const string AppSettingsJson = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft"": ""Warning"",
      ""Microsoft.Hosting.Lifetime"": ""Information""
    }
  },
  ""AgentSystem"": {
    ""MemoryType"": ""Redis"",
    ""Redis"": {
      ""ConnectionString"": ""localhost:6379""
    },
    ""SQL"": {
      ""ConnectionString"": ""Server=localhost;Database=AgentSystem;""
    },
    ""Monitoring"": {
      ""Type"": ""ApplicationInsights"",
      ""InstrumentationKey"": ""your-instrumentation-key""
    },
    ""KeyVault"": {
      ""Url"": ""https://your-keyvault.vault.azure.net/""
    },
    ""OAuth"": {
      ""ClientId"": ""your-client-id"",
      ""ClientSecret"": ""your-client-secret"",
      ""TokenEndpoint"": ""https://login.microsoftonline.com/your-tenant/oauth2/v2.0/token""
    },
    ""SafetySettings"": {
      ""ContentFilterEnabled"": true,
      ""ProhibitedTerms"": [
        ""harmful"",
        ""dangerous"",
        ""illegal""
      ],
      ""EthicalGuidelines"": [
        ""Be helpful"",
        ""Be honest"",
        ""Be harmless""
      ],
      ""RestrictedActionTypes"": [
        ""DeleteFile"",
        ""ModifySystemSettings""
      ]
    }
  },
  ""Agents"": {
    ""WeatherBot"": {
      ""Type"": ""Reactive"",
      ""Rules"": {
        ""GreetingRule"": {
          ""Condition"": ""input.Contains('Hello')"",
          ""Action"": {
            ""Type"": ""TextOutput"",
            ""Parameters"": {
              ""output"": ""Welcome! How can I help you with weather information?""
            }
          }
        },
        ""WeatherQueryRule"": {
          ""Condition"": ""input.Contains('weather')"",
          ""Action"": {
            ""Type"": ""WeatherQuery"",
            ""Parameters"": {
              ""queryType"": ""current""
            }
          }
        }
      }
    }
  }
}";

        public const string DockerFile = @"
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY [""AgentSystem.csproj"", "".""]
RUN dotnet restore ""./AgentSystem.csproj""
COPY . .
WORKDIR ""/src/.""
RUN dotnet build ""AgentSystem.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""AgentSystem.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""AgentSystem.dll""]
";

        public const string KubernetesManifest = @"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: agent-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: agent-system
  template:
    metadata:
      labels:
        app: agent-system
    spec:
      containers:
      - name: agent-system
        image: your-registry/agent-system:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: AgentSystem__Redis__ConnectionString
          valueFrom:
            secretKeyRef:
              name: agent-secrets
              key: redis-connection
        - name: AgentSystem__KeyVault__Url
          valueFrom:
            configMapKeyRef:
              name: agent-config
              key: keyvault-url
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 10
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 15
          periodSeconds: 20
---
apiVersion: v1
kind: Service
metadata:
  name: agent-system-service
spec:
  selector:
    app: agent-system
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
";

        public const string GitHubActionsWorkflow = @"
name: Agent System CI/CD

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 6.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish -c Release -o out
    
    - name: Build Docker image
      run: docker build -t ${{ secrets.DOCKER_REGISTRY }}/agent-system:${{ github.sha }} .
    
    - name: Push Docker image
      run: |
        echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
        docker push ${{ secrets.DOCKER_REGISTRY }}/agent-system:${{ github.sha }}
    
    - name: Deploy to Kubernetes
      uses: azure/k8s-deploy@v1
      with:
        manifests: |
          k8s/deployment.yaml
          k8s/service.yaml
        images: |
          ${{ secrets.DOCKER_REGISTRY }}/agent-system:${{ github.sha }}
";
    }
}
