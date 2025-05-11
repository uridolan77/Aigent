namespace Aigent.Core.Models
{
    /// <summary>
    /// Type of agent based on its decision-making approach
    /// </summary>
    public enum AgentType
    {
        /// <summary>
        /// Agent that reacts to environment changes based on predefined rules
        /// </summary>
        Reactive,
        
        /// <summary>
        /// Agent that plans actions based on a model of the world
        /// </summary>
        Deliberative,
        
        /// <summary>
        /// Agent that combines reactive and deliberative approaches
        /// </summary>
        Hybrid,
        
        /// <summary>
        /// Agent based on the Belief-Desire-Intention model
        /// </summary>
        BDI,
        
        /// <summary>
        /// Agent that makes decisions based on utility functions
        /// </summary>
        UtilityBased,
        
        /// <summary>
        /// Agent that uses machine learning for decision making
        /// </summary>
        Learning,
        
        /// <summary>
        /// Agent that adapts its behavior based on experience
        /// </summary>
        Adaptive,
        
        /// <summary>
        /// Agent that performs as a multi-agent system coordinator
        /// </summary>
        Orchestrator
    }
}
