using System.Threading.Tasks;
using Aigent.Core;

namespace Aigent.Safety
{
    /// <summary>
    /// Interface for safety validators
    /// </summary>
    public interface ISafetyValidator
    {
        /// <summary>
        /// Validates an action
        /// </summary>
        /// <param name="action">Action to validate</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateAction(IAction action);
    }
    
    /// <summary>
    /// Result of a validation
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
        /// Creates a valid validation result
        /// </summary>
        /// <returns>Valid validation result</returns>
        public static ValidationResult Valid()
        {
            return new ValidationResult
            {
                IsValid = true,
                Message = "Validation passed"
            };
        }
        
        /// <summary>
        /// Creates an invalid validation result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>Invalid validation result</returns>
        public static ValidationResult Invalid(string message)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = message
            };
        }
    }
}
