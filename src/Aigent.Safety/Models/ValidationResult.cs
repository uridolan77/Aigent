using System.Collections.Generic;

namespace Aigent.Safety.Models
{
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
        /// Category of the validation result
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Severity of the validation result
        /// </summary>
        public ValidationSeverity Severity { get; set; }
        
        /// <summary>
        /// List of individual guardrail evaluation results
        /// </summary>
        public List<GuardrailEvaluationResult> GuardrailResults { get; set; } = new List<GuardrailEvaluationResult>();
        
        /// <summary>
        /// Additional metadata for the validation result
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a valid validation result
        /// </summary>
        /// <returns>Valid validation result</returns>
        public static ValidationResult Valid()
        {
            return new ValidationResult
            {
                IsValid = true,
                Message = "Validation passed",
                Severity = ValidationSeverity.None
            };
        }
        
        /// <summary>
        /// Creates an invalid validation result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="severity">Severity of the validation failure</param>
        /// <returns>Invalid validation result</returns>
        public static ValidationResult Invalid(string message, ValidationSeverity severity = ValidationSeverity.High)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = message,
                Severity = severity
            };
        }
        
        /// <summary>
        /// Creates an invalid validation result with a category
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="category">Category of the validation failure</param>
        /// <param name="severity">Severity of the validation failure</param>
        /// <returns>Invalid validation result</returns>
        public static ValidationResult Invalid(string message, string category, ValidationSeverity severity = ValidationSeverity.High)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = message,
                Category = category,
                Severity = severity
            };
        }
    }
    
    /// <summary>
    /// Severity levels for validation results
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// No issues found
        /// </summary>
        None,
        
        /// <summary>
        /// Low severity issues found
        /// </summary>
        Low,
        
        /// <summary>
        /// Medium severity issues found
        /// </summary>
        Medium,
        
        /// <summary>
        /// High severity issues found
        /// </summary>
        High,
        
        /// <summary>
        /// Critical severity issues found
        /// </summary>
        Critical
    }
}
