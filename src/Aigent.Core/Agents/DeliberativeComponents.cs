using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for planners
    /// </summary>
    public interface IPlanner
    {
        /// <summary>
        /// Creates a plan based on the current state and context
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <param name="context">Context for planning</param>
        /// <returns>List of actions in the plan</returns>
        Task<List<IAction>> CreatePlan(EnvironmentState state, Dictionary<string, object> context);
    }

    /// <summary>
    /// Interface for learners
    /// </summary>
    public interface ILearner
    {
        /// <summary>
        /// Learns from an experience
        /// </summary>
        /// <param name="state">State when the action was performed</param>
        /// <param name="action">The action that was performed</param>
        /// <param name="result">The result of the action</param>
        Task Learn(EnvironmentState state, IAction action, ActionResult result);
    }

    /// <summary>
    /// Simple rule-based planner
    /// </summary>
    public class SimpleRulePlanner : IPlanner
    {
        private readonly List<PlanningRule> _rules;

        /// <summary>
        /// Initializes a new instance of the SimpleRulePlanner class
        /// </summary>
        /// <param name="rules">Rules for planning</param>
        public SimpleRulePlanner(List<PlanningRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Creates a plan based on the current state and context
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <param name="context">Context for planning</param>
        /// <returns>List of actions in the plan</returns>
        public Task<List<IAction>> CreatePlan(EnvironmentState state, Dictionary<string, object> context)
        {
            var plan = new List<IAction>();
            
            foreach (var rule in _rules)
            {
                if (rule.Condition(state, context))
                {
                    plan.Add(rule.Action);
                }
            }
            
            return Task.FromResult(plan);
        }
    }

    /// <summary>
    /// Rule for planning
    /// </summary>
    public class PlanningRule
    {
        /// <summary>
        /// Condition for the rule
        /// </summary>
        public Func<EnvironmentState, Dictionary<string, object>, bool> Condition { get; set; }
        
        /// <summary>
        /// Action to take when the condition is met
        /// </summary>
        public IAction Action { get; set; }
    }

    /// <summary>
    /// Simple reinforcement learner
    /// </summary>
    public class SimpleReinforcementLearner : ILearner
    {
        private readonly Dictionary<string, double> _qValues = new();

        /// <summary>
        /// Learns from an experience
        /// </summary>
        /// <param name="state">State when the action was performed</param>
        /// <param name="action">The action that was performed</param>
        /// <param name="result">The result of the action</param>
        public Task Learn(EnvironmentState state, IAction action, ActionResult result)
        {
            // Simple Q-learning update
            var stateKey = GetStateKey(state);
            var actionKey = action.ActionType;
            var key = $"{stateKey}:{actionKey}";
            
            var reward = result.Success ? 1.0 : -0.5;
            var currentQ = _qValues.GetValueOrDefault(key, 0.0);
            var learningRate = 0.1;
            
            _qValues[key] = currentQ + learningRate * (reward - currentQ);
            
            return Task.CompletedTask;
        }

        private string GetStateKey(EnvironmentState state)
        {
            // Simplified state key generation
            return string.Join("_", state.Properties.Keys);
        }
    }

    /// <summary>
    /// Factory for creating deliberative agents
    /// </summary>
    public static class DeliberativeAgentFactory
    {
        /// <summary>
        /// Creates a deliberative agent with default components
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        /// <returns>Deliberative agent</returns>
        public static DeliberativeAgent CreateDefaultDeliberativeAgent(
            string name,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            var rules = new List<PlanningRule>
            {
                new PlanningRule
                {
                    Condition = (state, context) => state.Properties.ContainsKey("user_query"),
                    Action = new TextOutputAction("I'll help you find information about that.")
                },
                new PlanningRule
                {
                    Condition = (state, context) => state.Properties.ContainsKey("task_request"),
                    Action = new TextOutputAction("I'll help you complete that task.")
                },
                new PlanningRule
                {
                    Condition = (state, context) => state.Properties.ContainsKey("error_occurred"),
                    Action = new TextOutputAction("I'll help you troubleshoot that error.")
                }
            };
            
            var planner = new SimpleRulePlanner(rules);
            var learner = new SimpleReinforcementLearner();
            
            return new DeliberativeAgent(
                name,
                planner,
                learner,
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);
        }
    }
}
