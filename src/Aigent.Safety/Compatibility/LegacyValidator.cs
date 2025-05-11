using System;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Compatibility
{
    /// <summary>
    /// Legacy implementation of the ISafetyValidator interface for backward compatibility
    /// </summary>
    [Obsolete("Use Aigent.Safety.Interfaces.ISafetyValidator instead. This class is kept for backward compatibility.")]
    public class LegacyValidator : ISafetyValidator
    {
        private readonly Interfaces.ISafetyValidator _validator;
        
        /// <summary>
        /// Initializes a new instance of the LegacyValidator class
        /// </summary>
        /// <param name="validator">The new validator to adapt</param>
        public LegacyValidator(Interfaces.ISafetyValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }
        
        /// <summary>
        /// Validates an action
        /// </summary>
        /// <param name="action">Action to validate</param>
        /// <returns>Validation result</returns>
        public async Task<ValidationResult> ValidateAction(IAction action)
        {
            var result = await _validator.ValidateActionAsync(action);
            
            // Convert new validation result to old validation result
            return new ValidationResult
            {
                IsValid = result.IsValid,
                Message = result.Message
            };
        }
        
        /// <summary>
        /// Adds a guardrail to the validator
        /// </summary>
        /// <param name="guardrail">The guardrail to add</param>
        public void AddGuardrail(IGuardrail guardrail)
        {
            _validator.AddGuardrail(guardrail);
        }
        
        /// <summary>
        /// Adds a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to restrict</param>
        public void AddActionTypeRestriction(string actionType)
        {
            _validator.AddActionTypeRestriction(actionType);
        }
    }
}
