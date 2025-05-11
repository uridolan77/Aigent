namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Interface for guardrails that provide safety constraints for agents
    /// </summary>
    public interface IGuardrail
    {
        /// <summary>
        /// Gets the name of the guardrail
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the description of the guardrail
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Checks if an action is allowed based on guardrail constraints
        /// </summary>
        /// <param name="action">Action to check</param>
        /// <param name="state">Current environment state</param>
        /// <returns>True if the action is allowed, false otherwise</returns>
        bool AllowAction(IAction action, EnvironmentState state);
    }
}
