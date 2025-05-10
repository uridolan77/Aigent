namespace Aigent.Core
{
    /// <summary>
    /// Types of agents
    /// </summary>
    public enum AgentType
    {
        /// <summary>
        /// Reactive agent that responds directly to stimuli
        /// </summary>
        Reactive,
        
        /// <summary>
        /// Deliberative agent that plans and reasons
        /// </summary>
        Deliberative,
        
        /// <summary>
        /// Hybrid agent that combines reactive and deliberative approaches
        /// </summary>
        Hybrid,
        
        /// <summary>
        /// BDI (Belief-Desire-Intention) agent that models mental attitudes
        /// </summary>
        BDI,
        
        /// <summary>
        /// Utility-based agent that selects actions based on utility functions
        /// </summary>
        UtilityBased,
        
        /// <summary>
        /// Learning agent that improves over time
        /// </summary>
        Learning
    }
}
