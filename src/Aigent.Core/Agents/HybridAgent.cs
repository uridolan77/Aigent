using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Hybrid agent that combines reactive and deliberative approaches
    /// </summary>
    public class HybridAgent : BaseAgent
    {
        /// <summary>
        /// Type of the agent
        /// </summary>
        public override AgentType Type => AgentType.Hybrid;

        private readonly ReactiveAgent _reactiveComponent;
        private readonly DeliberativeAgent _deliberativeComponent;
        private readonly double _reactiveThreshold;

        /// <summary>
        /// Initializes a new instance of the HybridAgent class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="reactiveComponent">Reactive component of the agent</param>
        /// <param name="deliberativeComponent">Deliberative component of the agent</param>
        /// <param name="reactiveThreshold">Threshold for using the reactive component</param>
        /// <param name="memory">Memory service for the agent</param>
        /// <param name="safetyValidator">Safety validator for the agent</param>
        /// <param name="logger">Logger for the agent</param>
        /// <param name="messageBus">Message bus for the agent</param>
        /// <param name="metrics">Metrics collector for the agent</param>
        public HybridAgent(
            string name,
            ReactiveAgent reactiveComponent,
            DeliberativeAgent deliberativeComponent,
            double reactiveThreshold,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
            : base(memory, safetyValidator, logger, messageBus, metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _reactiveComponent = reactiveComponent ?? throw new ArgumentNullException(nameof(reactiveComponent));
            _deliberativeComponent = deliberativeComponent ?? throw new ArgumentNullException(nameof(deliberativeComponent));
            _reactiveThreshold = reactiveThreshold;

            // Combine capabilities from both components
            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string>(reactiveComponent.Capabilities.SupportedActionTypes),
                SkillLevels = new Dictionary<string, double>(reactiveComponent.Capabilities.SkillLevels),
                LoadFactor = (reactiveComponent.Capabilities.LoadFactor + deliberativeComponent.Capabilities.LoadFactor) / 2,
                HistoricalPerformance = Math.Max(reactiveComponent.Capabilities.HistoricalPerformance, deliberativeComponent.Capabilities.HistoricalPerformance)
            };

            // Add deliberative capabilities
            foreach (var actionType in deliberativeComponent.Capabilities.SupportedActionTypes)
            {
                if (!Capabilities.SupportedActionTypes.Contains(actionType))
                {
                    Capabilities.SupportedActionTypes.Add(actionType);
                }
            }

            // Combine skill levels
            foreach (var skill in deliberativeComponent.Capabilities.SkillLevels)
            {
                if (Capabilities.SkillLevels.ContainsKey(skill.Key))
                {
                    Capabilities.SkillLevels[skill.Key] = Math.Max(Capabilities.SkillLevels[skill.Key], skill.Value);
                }
                else
                {
                    Capabilities.SkillLevels[skill.Key] = skill.Value;
                }
            }
        }

        /// <summary>
        /// Initializes the agent and its resources
        /// </summary>
        public override async Task Initialize()
        {
            await base.Initialize();

            // Initialize both components
            await _reactiveComponent.Initialize();
            await _deliberativeComponent.Initialize();

            _logger.Log(LogLevel.Information, $"Hybrid agent {Name} initialized with reactive threshold {_reactiveThreshold}");
        }

        /// <summary>
        /// Decides on an action based on the current environment state
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <returns>The action to be performed</returns>
        public override async Task<IAction> DecideAction(EnvironmentState state)
        {
            _metrics?.StartOperation($"agent_{Id}_decide_action");

            try
            {
                // Determine whether to use reactive or deliberative approach
                var urgency = CalculateUrgency(state);

                if (urgency >= _reactiveThreshold)
                {
                    _logger.Log(LogLevel.Debug, $"Agent {Name} using reactive approach (urgency: {urgency})");
                    _metrics?.RecordMetric($"agent.{Id}.reactive_decision", 1.0);

                    return await _reactiveComponent.DecideAction(state);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, $"Agent {Name} using deliberative approach (urgency: {urgency})");
                    _metrics?.RecordMetric($"agent.{Id}.deliberative_decision", 1.0);

                    return await _deliberativeComponent.DecideAction(state);
                }
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_decide_action");
            }
        }

        /// <summary>
        /// Learns from the result of an action
        /// </summary>
        /// <param name="state">State when the action was performed</param>
        /// <param name="action">The action that was performed</param>
        /// <param name="result">The result of the action</param>
        public override async Task Learn(EnvironmentState state, IAction action, ActionResult result)
        {
            _metrics?.StartOperation($"agent_{Id}_learn");

            try
            {
                // Both components learn from the experience
                await _reactiveComponent.Learn(state, action, result);
                await _deliberativeComponent.Learn(state, action, result);

                // Store the experience in memory
                await _memory.StoreContext($"hybrid_experience_{Guid.NewGuid()}", new
                {
                    State = state,
                    Action = action.ActionType,
                    Result = result.Success,
                    Timestamp = DateTime.UtcNow
                }, TimeSpan.FromDays(30));

                _logger.Log(LogLevel.Debug, $"Hybrid agent {Name} learned from action {action.ActionType}");
                _metrics?.RecordMetric($"agent.{Id}.learning_events", 1.0);
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_learn");
            }
        }

        /// <summary>
        /// Shuts down the agent and releases resources
        /// </summary>
        public override async Task Shutdown()
        {
            await base.Shutdown();

            // Shutdown both components
            await _reactiveComponent.Shutdown();
            await _deliberativeComponent.Shutdown();

            _logger.Log(LogLevel.Information, $"Hybrid agent {Name} shut down");
        }

        private double CalculateUrgency(EnvironmentState state)
        {
            // Calculate urgency based on state properties
            double urgency = 0.5; // Default urgency

            // Check for urgent keywords
            if (state.Properties.TryGetValue("input", out var input) && input is string inputStr)
            {
                if (inputStr.Contains("urgent", StringComparison.OrdinalIgnoreCase) ||
                    inputStr.Contains("emergency", StringComparison.OrdinalIgnoreCase) ||
                    inputStr.Contains("immediately", StringComparison.OrdinalIgnoreCase))
                {
                    urgency += 0.3;
                }
            }

            // Check for time constraints
            if (state.Properties.TryGetValue("deadline", out var deadline) && deadline is DateTime deadlineTime)
            {
                var timeRemaining = deadlineTime - DateTime.UtcNow;
                if (timeRemaining.TotalMinutes < 5)
                {
                    urgency += 0.2;
                }
            }

            // Cap urgency at 1.0
            return Math.Min(urgency, 1.0);
        }
    }

    /// <summary>
    /// Factory for creating hybrid agents
    /// </summary>
    public static class HybridAgentFactory
    {
        /// <summary>
        /// Creates a hybrid agent with default components
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        /// <returns>Hybrid agent</returns>
        public static HybridAgent CreateDefaultHybridAgent(
            string name,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            // Create default reactive rules
            var reactiveRules = new Dictionary<string, Func<EnvironmentState, IAction>>
            {
                ["greeting"] = state =>
                {
                    if (state.Properties.TryGetValue("input", out var input) &&
                        input is string inputStr &&
                        (inputStr.Contains("hello", StringComparison.OrdinalIgnoreCase) ||
                         inputStr.Contains("hi", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new TextOutputAction("Hello! How can I help you today?");
                    }
                    return null;
                },

                ["emergency"] = state =>
                {
                    if (state.Properties.TryGetValue("input", out var input) &&
                        input is string inputStr &&
                        (inputStr.Contains("emergency", StringComparison.OrdinalIgnoreCase) ||
                         inputStr.Contains("urgent", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new TextOutputAction("I'll prioritize this right away!");
                    }
                    return null;
                }
            };

            // Create reactive component
            var reactiveComponent = new ReactiveAgent(
                $"{name}_Reactive",
                reactiveRules,
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);

            // Create deliberative component
            var deliberativeComponent = DeliberativeAgentFactory.CreateDefaultDeliberativeAgent(
                $"{name}_Deliberative",
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);

            // Create hybrid agent
            return new HybridAgent(
                name,
                reactiveComponent,
                deliberativeComponent,
                0.7, // Reactive threshold
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);
        }
    }
}
