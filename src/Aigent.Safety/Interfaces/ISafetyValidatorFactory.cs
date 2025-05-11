using Aigent.Core.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Interfaces
{
    /// <summary>
    /// Interface for factories that create safety validators
    /// </summary>
    public interface ISafetyValidatorFactory
    {
        /// <summary>
        /// Creates a safety validator
        /// </summary>
        /// <returns>A new safety validator</returns>
        ISafetyValidator CreateValidator();
        
        /// <summary>
        /// Creates a safety validator with a specific configuration
        /// </summary>
        /// <param name="configuration">The configuration for the validator</param>
        /// <returns>A new safety validator</returns>
        ISafetyValidator CreateValidator(SafetyConfiguration configuration);
        
        /// <summary>
        /// Creates a guardrail of a specific type
        /// </summary>
        /// <param name="type">The type of guardrail to create</param>
        /// <returns>A new guardrail</returns>
        IGuardrail CreateGuardrail(GuardrailType type);
        
        /// <summary>
        /// Creates a guardrail of a specific type with a configuration
        /// </summary>
        /// <param name="type">The type of guardrail to create</param>
        /// <param name="configuration">The configuration for the guardrail</param>
        /// <returns>A new guardrail</returns>
        IGuardrail CreateGuardrail(GuardrailType type, GuardrailConfiguration configuration);
    }
}
