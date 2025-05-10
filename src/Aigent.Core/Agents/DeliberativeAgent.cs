using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Deliberative agent that plans and reasons before acting
    /// </summary>
    public class DeliberativeAgent : BaseAgent
    {
        /// <summary>
        /// Type of the agent
        /// </summary>
        public override AgentType Type => AgentType.Deliberative;

        private readonly IPlanner _planner;
        private readonly ILearner _learner;

        /// <summary>
        /// Initializes a new instance of the DeliberativeAgent class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="planner">Planner for the agent</param>
        /// <param name="learner">Learner for the agent</param>
        /// <param name="memory">Memory service for the agent</param>
        /// <param name="safetyValidator">Safety validator for the agent</param>
        /// <param name="logger">Logger for the agent</param>
        /// <param name="messageBus">Message bus for the agent</param>
        /// <param name="metrics">Metrics collector for the agent</param>
        public DeliberativeAgent(
            string name,
            IPlanner planner,
            ILearner learner,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
            : base(memory, safetyValidator, logger, messageBus, metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _learner = learner ?? throw new ArgumentNullException(nameof(learner));

            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string> { "Planning", "Reasoning", "Learning" },
                SkillLevels = new Dictionary<string, double>
                {
                    ["planning"] = 0.9,
                    ["reasoning"] = 0.85,
                    ["learning"] = 0.8
                },
                LoadFactor = 0.3,
                HistoricalPerformance = 0.8
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
                // Retrieve context from memory
                var context = await _memory.RetrieveContext<Dictionary<string, object>>("context")
                    ?? new Dictionary<string, object>();

                // Generate a plan
                var plan = await _planner.CreatePlan(state, context);

                // Select the best action from the plan
                var action = plan.FirstOrDefault();
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

                // No valid action found
                _logger.Log(LogLevel.Information, $"Agent {Name} could not generate a valid plan");
                _metrics?.RecordMetric($"agent.{Id}.no_valid_plan", 1.0);

                return new TextOutputAction("I need more information to make a decision.");
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
                // Learn from the experience
                await _learner.Learn(state, action, result);

                // Store the experience in memory
                await _memory.StoreContext($"experience_{Guid.NewGuid()}", new
                {
                    State = state,
                    Action = action.ActionType,
                    Result = result.Success,
                    Timestamp = DateTime.UtcNow
                }, TimeSpan.FromDays(30));

                _logger.Log(LogLevel.Debug, $"Agent {Name} learned from action {action.ActionType}");
                _metrics?.RecordMetric($"agent.{Id}.learning_events", 1.0);
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_learn");
            }
        }
    }
}
