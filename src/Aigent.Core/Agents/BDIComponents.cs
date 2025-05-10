using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Represents a belief in the BDI model
    /// </summary>
    public class Belief
    {
        /// <summary>
        /// Predicate of the belief
        /// </summary>
        public string Predicate { get; set; }
        
        /// <summary>
        /// Value of the belief
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Confidence in the belief (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// When the belief was formed
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a desire in the BDI model
    /// </summary>
    public class Desire
    {
        /// <summary>
        /// Goal of the desire
        /// </summary>
        public string Goal { get; set; }
        
        /// <summary>
        /// Importance of the desire (0.0 to 1.0)
        /// </summary>
        public double Importance { get; set; }
        
        /// <summary>
        /// Urgency of the desire (0.0 to 1.0)
        /// </summary>
        public double Urgency { get; set; }
    }

    /// <summary>
    /// Represents an intention in the BDI model
    /// </summary>
    public class Intention
    {
        /// <summary>
        /// Goal of the intention
        /// </summary>
        public string Goal { get; set; }
        
        /// <summary>
        /// Priority of the intention (0.0 to 1.0)
        /// </summary>
        public double Priority { get; set; }
        
        /// <summary>
        /// Progress towards completing the intention (0.0 to 1.0)
        /// </summary>
        public double Progress { get; set; }
        
        /// <summary>
        /// Whether the intention is completed
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// Whether the intention is impossible to achieve
        /// </summary>
        public bool IsImpossible { get; set; }
    }

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
        public List<IAction> Actions { get; set; } = new List<IAction>();
        
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
}
