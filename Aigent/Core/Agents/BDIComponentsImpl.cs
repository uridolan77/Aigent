using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
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
