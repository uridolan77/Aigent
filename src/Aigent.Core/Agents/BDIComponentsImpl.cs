using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Implementation of IBeliefsManager
    /// </summary>
    public class BeliefsManager : IBeliefsManager
    {
        private readonly Dictionary<string, Belief> _beliefs = new();
        private readonly ILogger _logger;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the BeliefsManager class
        /// </summary>
        /// <param name="logger">Logger for recording belief operations</param>
        public BeliefsManager(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a belief to the belief base
        /// </summary>
        /// <param name="belief">Belief to add</param>
        public void AddBelief(Belief belief)
        {
            if (belief == null || string.IsNullOrEmpty(belief.Predicate))
            {
                _logger?.Log(LogLevel.Warning, "Attempted to add null belief or belief with null predicate");
                return;
            }

            lock (_lock)
            {
                _beliefs[belief.Predicate] = belief;
                _logger?.Log(LogLevel.Debug, $"Added/updated belief: {belief.Predicate} = {belief.Value} (confidence: {belief.Confidence})");
            }
        }

        /// <summary>
        /// Removes a belief from the belief base
        /// </summary>
        /// <param name="predicate">Predicate of the belief to remove</param>
        public void RemoveBelief(string predicate)
        {
            if (string.IsNullOrEmpty(predicate))
            {
                _logger?.Log(LogLevel.Warning, "Attempted to remove belief with null predicate");
                return;
            }

            lock (_lock)
            {
                if (_beliefs.Remove(predicate))
                {
                    _logger?.Log(LogLevel.Debug, $"Removed belief: {predicate}");
                }
                else
                {
                    _logger?.Log(LogLevel.Debug, $"Attempted to remove non-existent belief: {predicate}");
                }
            }
        }

        /// <summary>
        /// Gets all beliefs in the belief base
        /// </summary>
        /// <returns>List of beliefs</returns>
        public List<Belief> GetBeliefs()
        {
            lock (_lock)
            {
                return _beliefs.Values.ToList();
            }
        }

        /// <summary>
        /// Gets a belief by predicate
        /// </summary>
        /// <param name="predicate">Predicate of the belief</param>
        /// <returns>The belief, or null if not found</returns>
        public Belief GetBelief(string predicate)
        {
            if (string.IsNullOrEmpty(predicate))
            {
                _logger?.Log(LogLevel.Warning, "Attempted to get belief with null predicate");
                return null;
            }

            lock (_lock)
            {
                if (_beliefs.TryGetValue(predicate, out var belief))
                {
                    return belief;
                }

                _logger?.Log(LogLevel.Debug, $"Belief not found: {predicate}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a belief is true
        /// </summary>
        /// <param name="predicate">Predicate of the belief</param>
        /// <returns>Whether the belief is true</returns>
        public bool IsBelief(string predicate)
        {
            if (string.IsNullOrEmpty(predicate))
            {
                _logger?.Log(LogLevel.Warning, "Attempted to check belief with null predicate");
                return false;
            }

            lock (_lock)
            {
                if (_beliefs.TryGetValue(predicate, out var belief))
                {
                    if (belief.Value is bool boolValue)
                    {
                        return boolValue;
                    }

                    return belief.Value != null;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Implementation of IDesireGenerator
    /// </summary>
    public class RuleBasedDesireGenerator : IDesireGenerator
    {
        private readonly List<Func<List<Belief>, List<Desire>>> _rules = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the RuleBasedDesireGenerator class
        /// </summary>
        /// <param name="logger">Logger for recording desire generation</param>
        public RuleBasedDesireGenerator(ILogger logger = null)
        {
            _logger = logger;

            // Add default rules
            AddRule(GenerateHelpUserDesire);
            AddRule(GenerateEmergencyDesire);
            AddRule(GeneratePlanningDesire);
        }

        /// <summary>
        /// Adds a rule for generating desires
        /// </summary>
        /// <param name="rule">Rule for generating desires</param>
        public void AddRule(Func<List<Belief>, List<Desire>> rule)
        {
            if (rule == null)
            {
                _logger?.Log(LogLevel.Warning, "Attempted to add null rule");
                return;
            }

            _rules.Add(rule);
            _logger?.Log(LogLevel.Debug, "Added desire generation rule");
        }

        /// <summary>
        /// Generates desires based on beliefs
        /// </summary>
        /// <param name="beliefs">Current beliefs</param>
        /// <returns>Generated desires</returns>
        public List<Desire> GenerateDesires(List<Belief> beliefs)
        {
            var desires = new List<Desire>();

            foreach (var rule in _rules)
            {
                try
                {
                    var ruleDesires = rule(beliefs);
                    if (ruleDesires != null && ruleDesires.Any())
                    {
                        desires.AddRange(ruleDesires);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, $"Error in desire generation rule: {ex.Message}");
                }
            }

            _logger?.Log(LogLevel.Debug, $"Generated {desires.Count} desires");
            return desires;
        }

        private List<Desire> GenerateHelpUserDesire(List<Belief> beliefs)
        {
            // Check if user needs help
            var userNeedsHelp = beliefs.Any(b =>
                b.Predicate.Contains("user_needs_help") ||
                b.Predicate.Contains("user_asked_question"));

            if (userNeedsHelp)
            {
                return new List<Desire>
                {
                    new Desire
                    {
                        Goal = "help_user",
                        Importance = 0.8,
                        Urgency = 0.7
                    }
                };
            }

            return new List<Desire>();
        }

        private List<Desire> GenerateEmergencyDesire(List<Belief> beliefs)
        {
            // Check if there's an emergency
            var emergency = beliefs.Any(b =>
                b.Predicate.Contains("emergency") ||
                b.Predicate.Contains("urgent"));

            if (emergency)
            {
                return new List<Desire>
                {
                    new Desire
                    {
                        Goal = "handle_emergency",
                        Importance = 1.0,
                        Urgency = 1.0
                    }
                };
            }

            return new List<Desire>();
        }

        private List<Desire> GeneratePlanningDesire(List<Belief> beliefs)
        {
            // Check if planning is needed
            var needsPlanning = beliefs.Any(b =>
                b.Predicate.Contains("needs_planning") ||
                b.Predicate.Contains("complex_task"));

            if (needsPlanning)
            {
                return new List<Desire>
                {
                    new Desire
                    {
                        Goal = "create_plan",
                        Importance = 0.7,
                        Urgency = 0.5
                    }
                };
            }

            return new List<Desire>();
        }
    }

    /// <summary>
    /// Implementation of IIntentionSelector
    /// </summary>
    public class PriorityBasedIntentionSelector : IIntentionSelector
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the PriorityBasedIntentionSelector class
        /// </summary>
        /// <param name="logger">Logger for recording intention selection</param>
        public PriorityBasedIntentionSelector(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Selects intentions from desires
        /// </summary>
        /// <param name="desires">Available desires</param>
        /// <param name="currentIntentions">Current intentions</param>
        /// <param name="beliefs">Current beliefs</param>
        /// <returns>Selected intentions</returns>
        public List<Intention> SelectIntentions(List<Desire> desires, List<Intention> currentIntentions, List<Belief> beliefs)
        {
            var newIntentions = new List<Intention>();

            // Filter out desires that are already intentions
            var currentGoals = currentIntentions.Select(i => i.Goal).ToHashSet();
            var newDesires = desires.Where(d => !currentGoals.Contains(d.Goal)).ToList();

            // Calculate priority based on importance and urgency
            var prioritizedDesires = newDesires
                .OrderByDescending(d => d.Importance * 0.6 + d.Urgency * 0.4)
                .Take(3) // Limit number of new intentions
                .ToList();

            foreach (var desire in prioritizedDesires)
            {
                var intention = new Intention
                {
                    Goal = desire.Goal,
                    Priority = desire.Importance * 0.6 + desire.Urgency * 0.4,
                    Progress = 0.0,
                    IsCompleted = false,
                    IsImpossible = false
                };

                newIntentions.Add(intention);
                _logger?.Log(LogLevel.Debug, $"Selected intention: {intention.Goal} with priority {intention.Priority}");
            }

            return newIntentions;
        }
    }

    /// <summary>
    /// Implementation of IPlanLibrary
    /// </summary>
    public class PlanLibrary : IPlanLibrary
    {
        private readonly Dictionary<string, List<Plan>> _plans = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the PlanLibrary class
        /// </summary>
        /// <param name="logger">Logger for recording plan operations</param>
        public PlanLibrary(ILogger logger = null)
        {
            _logger = logger;

            // Add default plans
            AddPlan(new Plan
            {
                Goal = "help_user",
                Context = _ => true,
                Actions = new List<IAction>
                {
                    new TextOutputAction("I'm here to help. What do you need assistance with?")
                }
            });

            AddPlan(new Plan
            {
                Goal = "create_plan",
                Context = _ => true,
                Actions = new List<IAction>
                {
                    new TextOutputAction("Let's create a plan. What are you trying to accomplish?")
                }
            });

            AddPlan(new Plan
            {
                Goal = "handle_emergency",
                Context = _ => true,
                Actions = new List<IAction>
                {
                    new TextOutputAction("This is an emergency situation. I'll prioritize this task immediately.")
                }
            });
        }

        /// <summary>
        /// Adds a plan to the library
        /// </summary>
        /// <param name="plan">Plan to add</param>
        public void AddPlan(Plan plan)
        {
            if (string.IsNullOrEmpty(plan.Goal))
            {
                _logger?.Log(LogLevel.Warning, "Attempted to add plan with null or empty goal");
                return;
            }

            lock (_plans)
            {
                if (!_plans.TryGetValue(plan.Goal, out var plans))
                {
                    plans = new List<Plan>();
                    _plans[plan.Goal] = plans;
                }

                plans.Add(plan);
                _logger?.Log(LogLevel.Debug, $"Added plan for goal: {plan.Goal}");
            }
        }

        /// <summary>
        /// Gets a plan for an intention
        /// </summary>
        /// <param name="intention">Intention to plan for</param>
        /// <param name="beliefs">Current beliefs</param>
        /// <returns>Plan for the intention</returns>
        public Plan GetPlan(Intention intention, List<Belief> beliefs)
        {
            lock (_plans)
            {
                if (!_plans.TryGetValue(intention.Goal, out var plans))
                {
                    _logger?.Log(LogLevel.Warning, $"No plans found for goal: {intention.Goal}");
                    return null;
                }

                // Find a plan whose context condition is satisfied
                foreach (var plan in plans)
                {
                    if (!plan.HasFailed && (plan.Context == null || plan.Context(beliefs)))
                    {
                        _logger?.Log(LogLevel.Debug, $"Selected plan for goal: {intention.Goal}");
                        return plan;
                    }
                }

                _logger?.Log(LogLevel.Warning, $"No applicable plans found for goal: {intention.Goal}");
                return null;
            }
        }

        /// <summary>
        /// Gets an alternative plan for an intention
        /// </summary>
        /// <param name="intention">Intention to plan for</param>
        /// <param name="beliefs">Current beliefs</param>
        /// <param name="failedPlan">Plan that failed</param>
        /// <returns>Alternative plan for the intention</returns>
        public Plan GetAlternativePlan(Intention intention, List<Belief> beliefs, Plan failedPlan)
        {
            lock (_plans)
            {
                if (!_plans.TryGetValue(intention.Goal, out var plans))
                {
                    _logger?.Log(LogLevel.Warning, $"No plans found for goal: {intention.Goal}");
                    return null;
                }

                // Find an alternative plan whose context condition is satisfied
                foreach (var plan in plans)
                {
                    if (plan != failedPlan && !plan.HasFailed && (plan.Context == null || plan.Context(beliefs)))
                    {
                        _logger?.Log(LogLevel.Debug, $"Selected alternative plan for goal: {intention.Goal}");
                        return plan;
                    }
                }

                _logger?.Log(LogLevel.Warning, $"No alternative plans found for goal: {intention.Goal}");
                return null;
            }
        }
    }

    /// <summary>
    /// Factory for creating BDI agents
    /// </summary>
    public static class BDIAgentFactory
    {
        /// <summary>
        /// Creates a BDI agent with default components
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        /// <returns>BDI agent</returns>
        public static BDIAgent CreateDefaultBDIAgent(
            string name,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            var beliefsManager = new BeliefsManager(logger);
            var desireGenerator = new RuleBasedDesireGenerator(logger);
            var intentionSelector = new PriorityBasedIntentionSelector(logger);
            var planLibrary = new PlanLibrary(logger);

            return new BDIAgent(
                name,
                beliefsManager,
                desireGenerator,
                intentionSelector,
                planLibrary,
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);
        }

        /// <summary>
        /// Creates a BDI agent with custom components
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
        /// <returns>BDI agent</returns>
        public static BDIAgent CreateCustomBDIAgent(
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
        {
            return new BDIAgent(
                name,
                beliefsManager,
                desireGenerator,
                intentionSelector,
                planLibrary,
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);
        }
    }
}
