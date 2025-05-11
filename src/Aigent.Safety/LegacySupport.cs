using System;
using Aigent.Safety.Compatibility;

namespace Aigent.Safety
{
    /// <summary>
    /// Provides support for legacy code using the old safety interfaces
    /// </summary>
    public static class LegacySupport
    {
        /// <summary>
        /// Creates a legacy validator adapter
        /// </summary>
        /// <param name="validator">The new validator to adapt</param>
        /// <returns>A legacy validator adapter</returns>
        public static ISafetyValidator CreateLegacyAdapter(Interfaces.ISafetyValidator validator)
        {
            return new LegacyValidator(validator);
        }
        
        /// <summary>
        /// Converts a new validation result to a legacy validation result
        /// </summary>
        /// <param name="result">The new validation result</param>
        /// <returns>A legacy validation result</returns>
        public static ValidationResult ToLegacyResult(Models.ValidationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            
            return new ValidationResult
            {
                IsValid = result.IsValid,
                Message = result.Message
            };
        }
        
        /// <summary>
        /// Converts a legacy validation result to a new validation result
        /// </summary>
        /// <param name="result">The legacy validation result</param>
        /// <returns>A new validation result</returns>
        public static Models.ValidationResult FromLegacyResult(ValidationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            
            return new Models.ValidationResult
            {
                IsValid = result.IsValid,
                Message = result.Message,
                Severity = result.IsValid ? Models.ValidationSeverity.None : Models.ValidationSeverity.High
            };
        }
    }
}
