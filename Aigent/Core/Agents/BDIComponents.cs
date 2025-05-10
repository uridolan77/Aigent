using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for beliefs managers
    /// </summary>
    public interface IBeliefsManager
    {
        /// <summary>
        /// Adds a belief to the belief base
        /// </summary>
        /// <param name="belief">Belief to add</param>
        void AddBelief(Belief belief);
        
        /// <summary>
        /// Removes a belief from the belief base
        /// </summary>
        /// <param name="predicate">Predicate of the belief to remove</param>
        void RemoveBelief(string predicate);
        
        /// <summary>
        /// Gets all beliefs in the belief base
        /// </summary>
        /// <returns>List of beliefs</returns>
        List<Belief> GetBeliefs();
        
        /// <summary>
        /// Gets a belief by predicate
        /// </summary>
        /// <param name="predicate">Predicate of the belief</param>
        /// <returns>The belief, or null if not found</returns>
        Belief GetBelief(string predicate);
        
        /// <summary>
        /// Checks if a belief is true
        /// </summary>
        /// <param name="predicate">Predicate of the belief</param>
        /// <returns>Whether the belief is true</returns>
        bool IsBelief(string predicate);
    }

    /// <summary>
    /// Interface for desire generators
    /// </summary>
    public interface IDesireGenerator
    {
        /// <summary>
        /// Generates desires based on beliefs
        /// </summary>
        /// <param name="beliefs">Current beliefs</param>
        /// <returns>Generated desires</returns>
        List<Desire> GenerateDesires(List<Belief> beliefs);
    }

    /// <summary>
    /// Interface for intention selectors
    /// </summary>
    public interface IIntentionSelector
    {
        /// <summary>
        /// Selects intentions from desires
        /// </summary>
        /// <param name="desires">Available desires</param>
        /// <param name="currentIntentions">Current intentions</param>
        /// <param name="beliefs">Current beliefs</param>
        /// <returns>Selected intentions</returns>
        List<Intention> SelectIntentions(List<Desire> desires, List<Intention> currentIntentions, List<Belief> beliefs);
    }

    /// <summary>
    /// Interface for plan libraries
    /// </summary>
    public interface IPlanLibrary
    {
        /// <summary>
        /// Gets a plan for an intention
        /// </summary>
        /// <param name="intention">Intention to plan for</param>
        /// <param name="beliefs">Current beliefs</param>
        /// <returns>Plan for the intention</returns>
        Plan GetPlan(Intention intention, List<Belief> beliefs);
        
        /// <summary>
        /// Gets an alternative plan for an intention
        /// </summary>
        /// <param name="intention">Intention to plan for</param>
        /// <param name="beliefs">Current beliefs</param>
        /// <param name="failedPlan">Plan that failed</param>
        /// <returns>Alternative plan for the intention</returns>
        Plan GetAlternativePlan(Intention intention, List<Belief> beliefs, Plan failedPlan);
        
        /// <summary>
        /// Adds a plan to the library
        /// </summary>
        /// <param name="plan">Plan to add</param>
        void AddPlan(Plan plan);
    }

    /// <summary>
    /// Represents a plan in the BDI model
    /// </summary>
    public class Plan
    {
        /// <summary>
        /// Goal of the plan
        /// </summary>
        public string Goal { get; set; }
        
        /// <summary>
        /// Context condition for the plan
        /// </summary>
        public Func<List<Belief>, bool> Context { get; set; }
        
        /// <summary>
        /// Actions in the plan
        /// </summary>
        public List<IAction> Actions { get; set; } = new();
        
        /// <summary>
        /// Current step in the plan
        /// </summary>
        public int CurrentStep { get; private set; }
        
        /// <summary>
        /// Whether the plan has failed
        /// </summary>
        public bool HasFailed { get; private set; }
        
        /// <summary>
        /// Reason for failure
        /// </summary>
        public string FailureReason { get; private set; }

        /// <summary>
        /// Gets the next action in the plan
        /// </summary>
        /// <returns>Next action</returns>
        public IAction GetNextAction()
        {
            if (HasFailed || CurrentStep >= Actions.Count)
            {
                return null;
            }
            
            return Actions[CurrentStep++];
        }

        /// <summary>
        /// Marks the plan as failed
        /// </summary>
        /// <param name="reason">Reason for failure</param>
        public void MarkFailed(string reason)
        {
            HasFailed = true;
            FailureReason = reason;
        }

        /// <summary>
        /// Resets the plan
        /// </summary>
        public void Reset()
        {
            CurrentStep = 0;
            HasFailed = false;
            FailureReason = null;
        }
    }

    /// <summary>
    /// Implementation of IBeliefsManager
    /// </summary>
    public class BeliefsManager : IBeliefsManager
    {
        private readonly Dictionary<string, Belief> _beliefs = new();
        private readonly ILogger _logger;

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
            if (string.IsNullOrEmpty(belief.Predicate))
            {
                _logger?.Log(LogLevel.Warning, "Attempted to add belief with null or empty predicate");
                return;
            }
            
            lock (_beliefs)
            {
                if (_beliefs.TryGetValue(belief.Predicate, out var existingBelief))
                {
                    // Update existing belief if new one is more recent or has higher confidence
                    if (belief.Timestamp > existingBelief.Timestamp || belief.Confidence > existingBelief.Confidence)
                    {
                        _beliefs[belief.Predicate] = belief;
                        _logger?.Log(LogLevel.Debug, $"Updated belief: {belief.Predicate}");
                    }
                }
                else
                {
                    // Add new belief
                    _beliefs[belief.Predicate] = belief;
                    _logger?.Log(LogLevel.Debug, $"Added new belief: {belief.Predicate}");
                }
            }
        }

        /// <summary>
        /// Removes a belief from the belief base
        /// </summary>
        /// <param name="predicate">Predicate of the belief to remove</param>
        public void RemoveBelief(string predicate)
        {
            lock (_beliefs)
            {
                if (_beliefs.Remove(predicate))
                {
                    _logger?.Log(LogLevel.Debug, $"Removed belief: {predicate}");
                }
            }
        }

        /// <summary>
        /// Gets all beliefs in the belief base
        /// </summary>
        /// <returns>List of beliefs</returns>
        public List<Belief> GetBeliefs()
        {
            lock (_beliefs)
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
            lock (_beliefs)
            {
                _beliefs.TryGetValue(predicate, out var belief);
                return belief;
            }
        }

        /// <summary>
        /// Checks if a belief is true
        /// </summary>
        /// <param name="predicate">Predicate of the belief</param>
        /// <returns>Whether the belief is true</returns>
        public bool IsBelief(string predicate)
        {
            var belief = GetBelief(predicate);
            
            if (belief == null)
            {
                return false;
            }
            
            if (belief.Value is bool boolValue)
            {
                return boolValue;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Implementation of IDesireGenerator
    /// </summary>
    public class RuleBasedDesireGenerator : IDesireGenerator
    {
        private readonly List<DesireGenerationRule> _rules = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the RuleBasedDesireGenerator class
        /// </summary>
        /// <param name="logger">Logger for recording desire generation</param>
        public RuleBasedDesireGenerator(ILogger logger = null)
        {
            _logger = logger;
            
            // Add default rules
            AddRule(new DesireGenerationRule
            {
                Condition = beliefs => beliefs.Any(b => b.Predicate == "input" && b.Value is string input && input.Contains("help", StringComparison.OrdinalIgnoreCase)),
                DesireFactory = _ => new Desire { Goal = "help_user", Importance = 0.8, Urgency = 0.7 }
            });
            
            AddRule(new DesireGenerationRule
            {
                Condition = beliefs => beliefs.Any(b => b.Predicate == "input" && b.Value is string input && input.Contains("plan", StringComparison.OrdinalIgnoreCase)),
                DesireFactory = _ => new Desire { Goal = "create_plan", Importance = 0.7, Urgency = 0.5 }
            });
            
            AddRule(new DesireGenerationRule
            {
                Condition = beliefs => beliefs.Any(b => b.Predicate == "input" && b.Value is string input && 
                    (input.Contains("urgent", StringComparison.OrdinalIgnoreCase) || input.Contains("emergency", StringComparison.OrdinalIgnoreCase))),
                DesireFactory = _ => new Desire { Goal = "handle_emergency", Importance = 0.9, Urgency = 0.9 }
            });
        }

        /// <summary>
        /// Adds a rule for desire generation
        /// </summary>
        /// <param name="rule">Rule to add</param>
        public void AddRule(DesireGenerationRule rule)
        {
            _rules.Add(rule);
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
                    if (rule.Condition(beliefs))
                    {
                        var desire = rule.DesireFactory(beliefs);
                        desires.Add(desire);
                        _logger?.Log(LogLevel.Debug, $"Generated desire: {desire.Goal}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error in desire generation rule: {ex.Message}", ex);
                }
            }
            
            return desires;
        }
    }

    /// <summary>
    /// Rule for generating desires
    /// </summary>
    public class DesireGenerationRule
    {
        /// <summary>
        /// Condition for the rule
        /// </summary>
        public Func<List<Belief>, bool> Condition { get; set; }
        
        /// <summary>
        /// Factory for creating desires
        /// </summary>
        public Func<List<Belief>, Desire> DesireFactory { get; set; }
    }
}
