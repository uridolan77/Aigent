using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core;

namespace Aigent.Safety
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
        Task<ValidationResult> ValidateAction(IAction action);
        
        /// <summary>
        /// Adds a guardrail to the validator
        /// </summary>
        /// <param name="guardrail">The guardrail to add</param>
        void AddGuardrail(IGuardrail guardrail);
        
        /// <summary>
        /// Adds a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to restrict</param>
        void AddActionTypeRestriction(string actionType);
    }

    /// <summary>
    /// Interface for guardrails that enforce specific safety constraints
    /// </summary>
    public interface IGuardrail
    {
        /// <summary>
        /// Name of the guardrail
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Validates an action against this guardrail
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <returns>The validation result</returns>
        Task<ValidationResult> Validate(IAction action);
    }

    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Message describing the validation result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// List of specific violations found
        /// </summary>
        public List<string> Violations { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>A successful validation result</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsValid = true,
                Message = "Validation passed"
            };
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="message">Message describing the failure</param>
        /// <param name="violations">Optional list of specific violations</param>
        /// <returns>A failed validation result</returns>
        public static ValidationResult Failure(string message, List<string> violations = null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = message,
                Violations = violations ?? new List<string> { message }
            };
        }
    }
}
