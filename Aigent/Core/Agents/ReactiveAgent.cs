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
    /// Reactive agent that responds to stimuli based on predefined rules
    /// </summary>
    public class ReactiveAgent : BaseAgent
    {
        /// <summary>
        /// Type of the agent
        /// </summary>
        public override AgentType Type => AgentType.Reactive;
        
        private readonly Dictionary<string, Func<EnvironmentState, IAction>> _rules;

        /// <summary>
        /// Initializes a new instance of the ReactiveAgent class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="rules">Rules for the agent</param>
        /// <param name="memory">Memory service for the agent</param>
        /// <param name="safetyValidator">Safety validator for the agent</param>
        /// <param name="logger">Logger for the agent</param>
        /// <param name="messageBus">Message bus for the agent</param>
        /// <param name="metrics">Metrics collector for the agent</param>
        public ReactiveAgent(
            string name,
            Dictionary<string, Func<EnvironmentState, IAction>> rules,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
            : base(memory, safetyValidator, logger, messageBus, metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            
            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string> { "TextOutput", "ReactiveResponse" },
                SkillLevels = new Dictionary<string, double>
                {
                    ["quick_response"] = 0.9,
                    ["pattern_matching"] = 0.8
                },
                LoadFactor = 0.1,
                HistoricalPerformance = 0.85
            };
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
                // Apply each rule in order
                foreach (var rule in _rules)
                {
                    var action = rule.Value(state);
                    if (action != null)
                    {
                        // Validate the action
                        var validationResult = await _safetyValidator.ValidateAction(action);
                        if (validationResult.IsValid)
                        {
                            _logger.Log(LogLevel.Debug, $"Agent {Name} selected action: {action.ActionType}");
                            _metrics?.RecordMetric($"agent.{Id}.action_selected", 1.0);
                            return action;
                        }
                        else
                        {
                            _logger.Log(LogLevel.Warning, $"Agent {Name} action {action.ActionType} failed validation: {validationResult.Message}");
                            _metrics?.RecordMetric($"agent.{Id}.action_validation_failure", 1.0);
                        }
                    }
                }
                
                // No matching rule found
                _logger.Log(LogLevel.Information, $"Agent {Name} found no matching rule for input");
                _metrics?.RecordMetric($"agent.{Id}.no_matching_rule", 1.0);
                
                return new TextOutputAction("I'm not sure how to respond to that.");
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_decide_action");
            }
        }
    }

    /// <summary>
    /// Generic action implementation
    /// </summary>
    public class GenericAction : IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string ActionType { get; }
        
        /// <summary>
        /// Parameters for the action
        /// </summary>
        public Dictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// Priority of the action
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Estimated cost of the action
        /// </summary>
        public double EstimatedCost { get; set; }

        /// <summary>
        /// Initializes a new instance of the GenericAction class
        /// </summary>
        /// <param name="actionType">Type of the action</param>
        /// <param name="parameters">Parameters for the action</param>
        public GenericAction(string actionType, Dictionary<string, object> parameters)
        {
            ActionType = actionType;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        public Task<ActionResult> Execute()
        {
            // Generic implementation - in a real system, this would dispatch to appropriate handlers
            return Task.FromResult(new ActionResult
            {
                Success = true,
                Message = $"Executed {ActionType}",
                Data = new Dictionary<string, object>
                {
                    ["action_type"] = ActionType,
                    ["parameters"] = Parameters
                }
            });
        }
    }

    /// <summary>
    /// Text output action
    /// </summary>
    public class TextOutputAction : IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string ActionType => "TextOutput";
        
        /// <summary>
        /// Parameters for the action
        /// </summary>
        public Dictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// Priority of the action
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Estimated cost of the action
        /// </summary>
        public double EstimatedCost { get; set; }

        /// <summary>
        /// Initializes a new instance of the TextOutputAction class
        /// </summary>
        /// <param name="text">Text to output</param>
        public TextOutputAction(string text)
        {
            Parameters = new Dictionary<string, object>
            {
                ["text"] = text
            };
        }

        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        public Task<ActionResult> Execute()
        {
            var text = Parameters["text"].ToString();
            
            return Task.FromResult(new ActionResult
            {
                Success = true,
                Message = "Text output generated",
                Data = new Dictionary<string, object>
                {
                    ["text"] = text
                }
            });
        }
    }
}
