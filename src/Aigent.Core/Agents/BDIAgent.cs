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
    /// BDI (Belief-Desire-Intention) agent that models mental attitudes
    /// </summary>
    public class BDIAgent : BaseAgent
    {
        /// <summary>
        /// Type of the agent
        /// </summary>
        public override AgentType Type => AgentType.BDI;

        private readonly IBeliefsManager _beliefsManager;
        private readonly IDesireGenerator _desireGenerator;
        private readonly IIntentionSelector _intentionSelector;
        private readonly IPlanLibrary _planLibrary;

        /// <summary>
        /// Current intentions of the agent
        /// </summary>
        private readonly List<Intention> _currentIntentions = new();

        /// <summary>
        /// Initializes a new instance of the BDIAgent class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="beliefsManager">Beliefs manager</param>
        /// <param name="desireGenerator">Desire generator</param>
        /// <param name="intentionSelector">Intention selector</param>
        /// <param name="planLibrary">Plan library</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        public BDIAgent(
            string name,
            IBeliefsManager beliefsManager,
            IDesireGenerator desireGenerator,
            IIntentionSelector intentionSelector,
            IPlanLibrary planLibrary,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
            : base(memory, safetyValidator, logger, messageBus, metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _beliefsManager = beliefsManager ?? throw new ArgumentNullException(nameof(beliefsManager));
            _desireGenerator = desireGenerator ?? throw new ArgumentNullException(nameof(desireGenerator));
            _intentionSelector = intentionSelector ?? throw new ArgumentNullException(nameof(intentionSelector));
            _planLibrary = planLibrary ?? throw new ArgumentNullException(nameof(planLibrary));

            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string> { "Reasoning", "Planning", "Learning", "Adaptation" },
                SkillLevels = new Dictionary<string, double>
                {
                    ["reasoning"] = 0.95,
                    ["planning"] = 0.9,
                    ["adaptation"] = 0.85,
                    ["learning"] = 0.8
                },
                LoadFactor = 0.4,
                HistoricalPerformance = 0.85
            };
        }

        /// <summary>
        /// Initializes the agent and its resources
        /// </summary>
        public override async Task Initialize()
        {
            await base.Initialize();

            // Initialize beliefs from memory
            var storedBeliefs = await _memory.RetrieveContext<List<Belief>>("beliefs");
            if (storedBeliefs != null)
            {
                foreach (var belief in storedBeliefs)
                {
                    _beliefsManager.AddBelief(belief);
                }
            }

            // Initialize intentions from memory
            var storedIntentions = await _memory.RetrieveContext<List<Intention>>("intentions");
            if (storedIntentions != null)
            {
                _currentIntentions.AddRange(storedIntentions);
            }

            _logger.Log(LogLevel.Information, $"BDI agent {Name} initialized with {_beliefsManager.GetBeliefs().Count} beliefs and {_currentIntentions.Count} intentions");
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
                // Update beliefs based on percepts
                UpdateBeliefs(state);

                // Generate desires based on beliefs
                var desires = _desireGenerator.GenerateDesires(_beliefsManager.GetBeliefs());

                // Filter and prioritize intentions
                _currentIntentions.RemoveAll(i => i.IsCompleted || i.IsImpossible);

                // Select new intentions from desires if needed
                if (_currentIntentions.Count < 3) // Limit number of concurrent intentions
                {
                    var newIntentions = _intentionSelector.SelectIntentions(
                        desires,
                        _currentIntentions,
                        _beliefsManager.GetBeliefs());

                    _currentIntentions.AddRange(newIntentions);

                    // Store updated intentions in memory
                    await _memory.StoreContext("intentions", _currentIntentions);
                }

                // Select the highest priority intention
                var activeIntention = _currentIntentions
                    .OrderByDescending(i => i.Priority)
                    .FirstOrDefault();

                if (activeIntention == null)
                {
                    _logger.Log(LogLevel.Information, $"Agent {Name} has no active intentions");
                    return new TextOutputAction("I don't have any active goals at the moment.");
                }

                // Get plan for the intention
                var plan = _planLibrary.GetPlan(activeIntention, _beliefsManager.GetBeliefs());

                if (plan == null)
                {
                    _logger.Log(LogLevel.Warning, $"No plan found for intention: {activeIntention.Goal}");
                    activeIntention.IsImpossible = true;
                    return new TextOutputAction($"I don't know how to achieve the goal: {activeIntention.Goal}");
                }

                // Get next action from plan
                var action = plan.GetNextAction();

                // Validate the action
                var validationResult = await _safetyValidator.ValidateAction(action);
                if (validationResult.IsValid)
                {
                    _logger.Log(LogLevel.Debug, $"Agent {Name} selected action: {action.ActionType} for intention: {activeIntention.Goal}");
                    _metrics?.RecordMetric($"agent.{Id}.action_selected", 1.0);
                    return action;
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Agent {Name} action {action.ActionType} failed validation: {validationResult.Message}");
                    _metrics?.RecordMetric($"agent.{Id}.action_validation_failure", 1.0);

                    // Mark the plan as failed
                    plan.MarkFailed(validationResult.Message);

                    // Try to find an alternative plan
                    var alternativePlan = _planLibrary.GetAlternativePlan(activeIntention, _beliefsManager.GetBeliefs(), plan);

                    if (alternativePlan != null)
                    {
                        _logger.Log(LogLevel.Information, $"Found alternative plan for intention: {activeIntention.Goal}");
                        return alternativePlan.GetNextAction();
                    }

                    // No alternative plan found
                    activeIntention.IsImpossible = true;
                    return new TextOutputAction($"I can't find a safe way to achieve the goal: {activeIntention.Goal}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in BDI agent decision making: {ex.Message}", ex);
                _metrics?.RecordMetric($"agent.{Id}.decision_error", 1.0);

                return new TextOutputAction("I encountered an error in my reasoning process.");
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
                // Update beliefs based on action result
                if (result.Success)
                {
                    _beliefsManager.AddBelief(new Belief
                    {
                        Predicate = $"action_{action.ActionType}_successful",
                        Value = true,
                        Confidence = 0.9,
                        Timestamp = DateTime.UtcNow
                    });

                    // Update intention status
                    var relatedIntention = _currentIntentions.FirstOrDefault(i =>
                        _planLibrary.GetPlan(i, _beliefsManager.GetBeliefs())?.Actions.Contains(action) == true);

                    if (relatedIntention != null)
                    {
                        relatedIntention.Progress += 0.2;
                        if (relatedIntention.Progress >= 1.0)
                        {
                            relatedIntention.IsCompleted = true;
                            _logger.Log(LogLevel.Information, $"Intention completed: {relatedIntention.Goal}");
                        }
                    }
                }
                else
                {
                    _beliefsManager.AddBelief(new Belief
                    {
                        Predicate = $"action_{action.ActionType}_failed",
                        Value = true,
                        Confidence = 0.7,
                        Timestamp = DateTime.UtcNow
                    });

                    // Update plan status
                    var relatedIntention = _currentIntentions.FirstOrDefault(i =>
                        _planLibrary.GetPlan(i, _beliefsManager.GetBeliefs())?.Actions.Contains(action) == true);

                    if (relatedIntention != null)
                    {
                        var plan = _planLibrary.GetPlan(relatedIntention, _beliefsManager.GetBeliefs());
                        plan?.MarkFailed(result.Message);
                    }
                }

                // Store updated beliefs in memory
                await _memory.StoreContext("beliefs", _beliefsManager.GetBeliefs());

                // Store updated intentions in memory
                await _memory.StoreContext("intentions", _currentIntentions);

                _logger.Log(LogLevel.Debug, $"BDI agent {Name} learned from action {action.ActionType}");
                _metrics?.RecordMetric($"agent.{Id}.learning_events", 1.0);
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_learn");
            }
        }

        /// <summary>
        /// Updates beliefs based on percepts from the environment
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        private void UpdateBeliefs(EnvironmentState state)
        {
            foreach (var percept in state.Properties)
            {
                _beliefsManager.AddBelief(new Belief
                {
                    Predicate = percept.Key,
                    Value = percept.Value,
                    Confidence = 0.8,
                    Timestamp = state.Timestamp
                });
            }
        }
    }
}
