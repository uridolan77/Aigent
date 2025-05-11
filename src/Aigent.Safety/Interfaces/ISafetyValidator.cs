using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Interfaces
{
    /// <summary>
    /// Interface for safety validation services
    /// </summary>
    public interface ISafetyValidator
    {
        /// <summary>
        /// Validates an action for safety and compliance
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <returns>The validation result</returns>
        Task<ValidationResult> ValidateActionAsync(IAction action);
        
        /// <summary>
        /// Validates a text message for safety and compliance
        /// </summary>
        /// <param name="text">The text to validate</param>
        /// <param name="context">Optional additional context</param>
        /// <returns>The validation result</returns>
        Task<ValidationResult> ValidateTextAsync(string text, IDictionary<string, object> context = null);
        
        /// <summary>
        /// Adds a guardrail to the validator
        /// </summary>
        /// <param name="guardrail">The guardrail to add</param>
        void AddGuardrail(IGuardrail guardrail);
        
        /// <summary>
        /// Removes a guardrail from the validator
        /// </summary>
        /// <param name="guardrailId">The ID of the guardrail to remove</param>
        /// <returns>True if the guardrail was removed, false otherwise</returns>
        bool RemoveGuardrail(string guardrailId);
        
        /// <summary>
        /// Gets all guardrails registered with this validator
        /// </summary>
        /// <returns>A list of guardrails</returns>
        IReadOnlyList<IGuardrail> GetGuardrails();
        
        /// <summary>
        /// Adds a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to restrict</param>
        void AddActionTypeRestriction(string actionType);
        
        /// <summary>
        /// Removes a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to allow</param>
        /// <returns>True if the restriction was removed, false otherwise</returns>
        bool RemoveActionTypeRestriction(string actionType);
        
        /// <summary>
        /// Gets all restricted action types
        /// </summary>
        /// <returns>A list of restricted action types</returns>
        IReadOnlyList<string> GetRestrictedActionTypes();
        
        /// <summary>
        /// Configures the validator from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        void Configure(SafetyConfiguration configuration);
    }
}
