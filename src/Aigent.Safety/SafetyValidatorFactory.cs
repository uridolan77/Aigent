using System;
using Aigent.Monitoring;
using Aigent.Safety.Guardrails;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;
using Aigent.Safety.Validators;

namespace Aigent.Safety
{
    /// <summary>
    /// Factory for creating safety validators and guardrails
    /// </summary>
    public class SafetyValidatorFactory : ISafetyValidatorFactory
    {
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        
        /// <summary>
        /// Initializes a new instance of the SafetyValidatorFactory class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        public SafetyValidatorFactory(ILogger logger = null, IMetricsCollector metrics = null)
        {
            _logger = logger;
            _metrics = metrics;
        }
        
        /// <summary>
        /// Creates a safety validator
        /// </summary>
        /// <returns>A new safety validator</returns>
        public Interfaces.ISafetyValidator CreateValidator()
        {
            return new StandardSafetyValidator(_logger, _metrics);
        }
        
        /// <summary>
        /// Creates a safety validator with a specific configuration
        /// </summary>
        /// <param name="configuration">The configuration for the validator</param>
        /// <returns>A new safety validator</returns>
        public Interfaces.ISafetyValidator CreateValidator(SafetyConfiguration configuration)
        {
            var validator = new StandardSafetyValidator(_logger, _metrics);
            validator.Configure(configuration);
            return validator;
        }        /// <summary>
        /// Creates a guardrail of a specific type
        /// </summary>
        /// <param name="type">The type of guardrail to create</param>
        /// <returns>A new guardrail</returns>
        public IGuardrail CreateGuardrail(GuardrailType type)
        {
            _logger?.LogDebug($"Creating guardrail of type {type}");
            
            switch (type)
            {
                case GuardrailType.ContentModeration:
                    return new Guardrails.ContentModerationGuardrail(
                        Guid.NewGuid().ToString(),
                        "Content Moderation",
                        "Checks for harmful or toxic content",
                        GuardrailSeverity.Error,
                        _logger);
                
                case GuardrailType.PiiDetection:
                    // TODO: Implement PII detection guardrail
                    _logger?.LogWarning($"PII Detection guardrail not yet implemented");
                    break;
                  case GuardrailType.ProfanityFilter:
                    return new Guardrails.ProfanityFilterGuardrail(
                        Guid.NewGuid().ToString(),
                        "Profanity Filter",
                        "Filters profanity from content",
                        GuardrailSeverity.Warning,
                        _logger);
                
                case GuardrailType.JailbreakDetection:
                    // TODO: Implement jailbreak detection guardrail
                    _logger?.LogWarning($"Jailbreak Detection guardrail not yet implemented");
                    break;
                
                default:
                    _logger?.LogWarning($"Unknown guardrail type: {type}");
                    break;
            }
            
            return null;
        }
        
        /// <summary>
        /// Creates a guardrail of a specific type with a configuration
        /// </summary>
        /// <param name="type">The type of guardrail to create</param>
        /// <param name="configuration">The configuration for the guardrail</param>
        /// <returns>A new guardrail</returns>
        public IGuardrail CreateGuardrail(GuardrailType type, GuardrailConfiguration configuration)
        {
            var guardrail = CreateGuardrail(type);
            
            if (guardrail != null && configuration != null)
            {
                guardrail.Configure(configuration);
            }
            
            return guardrail;
        }
    }
}
