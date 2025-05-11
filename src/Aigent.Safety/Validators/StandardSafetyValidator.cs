using System;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Monitoring;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Validators
{
    /// <summary>
    /// Standard implementation of a safety validator with default guardrails
    /// </summary>
    public class StandardSafetyValidator : BaseSafetyValidator
    {
        /// <summary>
        /// Initializes a new instance of the StandardSafetyValidator class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        public StandardSafetyValidator(ILogger logger = null, IMetricsCollector metrics = null)
            : base(logger, metrics)
        {
            // Add default restricted action types
            AddActionTypeRestriction("FileSystemAccess");
            AddActionTypeRestriction("NetworkAccess");
            AddActionTypeRestriction("SystemCommand");
            AddActionTypeRestriction("DatabaseModification");
        }
        
        /// <summary>
        /// Configures the validator from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        public override void Configure(SafetyConfiguration configuration)
        {
            base.Configure(configuration);
            
            // Create and configure guardrails from configuration
            if (configuration.Guardrails != null)
            {
                foreach (var guardrailConfig in configuration.Guardrails)
                {
                    try
                    {
                        var guardrail = CreateGuardrail(guardrailConfig);
                        if (guardrail != null)
                        {
                            AddGuardrail(guardrail);
                        }
                    }                    catch (Exception ex)
                    {
                        Logger?.LogError($"Failed to create guardrail {guardrailConfig.Name}: {ex.Message}");
                    }
                }
            }
        }
          /// <summary>
        /// Creates a guardrail from configuration
        /// </summary>
        /// <param name="configuration">Guardrail configuration</param>
        /// <returns>Guardrail instance</returns>
        protected virtual IGuardrail CreateGuardrail(GuardrailConfiguration configuration)
        {
            Logger?.LogDebug($"Creating guardrail of type {configuration.Type}: {configuration.Name}");
            
            IGuardrail guardrail = configuration.Type switch
            {
                GuardrailType.ContentModeration => new Guardrails.ContentModerationGuardrail(
                    configuration.Id ?? Guid.NewGuid().ToString(),
                    configuration.Name ?? "Content Moderation",
                    configuration.Description ?? "Checks for harmful or toxic content",
                    configuration.Severity,
                    Logger),
                
                GuardrailType.ProfanityFilter => new Guardrails.ProfanityFilterGuardrail(
                    configuration.Id ?? Guid.NewGuid().ToString(),
                    configuration.Name ?? "Profanity Filter",
                    configuration.Description ?? "Filters profanity from content",
                    configuration.Severity,
                    Logger),
                
                _ => null
            };
            
            if (guardrail != null)
            {
                guardrail.Configure(configuration);
            }
            
            return guardrail;
        }
        
        /// <summary>
        /// Validates an action for safety and compliance
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <returns>The validation result</returns>
        public override async Task<ValidationResult> ValidateActionAsync(IAction action)
        {
            var result = await base.ValidateActionAsync(action);
            
            // Apply additional custom validation logic here if needed
            
            return result;
        }
    }
}
