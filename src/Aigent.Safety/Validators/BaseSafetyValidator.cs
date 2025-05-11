using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Monitoring;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Validators
{
    /// <summary>
    /// Base implementation of a safety validator
    /// </summary>
    public abstract class BaseSafetyValidator : Interfaces.ISafetyValidator
    {
        private readonly List<IGuardrail> _guardrails = new();
        private readonly HashSet<string> _restrictedActionTypes = new();
        
        /// <summary>
        /// Gets the logger used by this validator
        /// </summary>
        protected ILogger Logger { get; }
        
        /// <summary>
        /// Gets the metrics collector used by this validator
        /// </summary>
        protected IMetricsCollector Metrics { get; }
        
        /// <summary>
        /// Gets the default response mode for validation failures
        /// </summary>
        protected FailureResponseMode DefaultResponseMode { get; set; } = FailureResponseMode.Block;
        
        /// <summary>
        /// Gets whether validation is enforced or just advisory
        /// </summary>
        protected bool EnforceValidation { get; set; } = true;
        
        /// <summary>
        /// Gets whether to enable content scanning
        /// </summary>
        protected bool EnableContentScanning { get; set; } = true;
        
        /// <summary>
        /// Initializes a new instance of the BaseSafetyValidator class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        protected BaseSafetyValidator(ILogger logger = null, IMetricsCollector metrics = null)
        {
            Logger = logger;
            Metrics = metrics;
        }
        
        /// <summary>
        /// Validates an action for safety and compliance
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <returns>The validation result</returns>
        public virtual async Task<ValidationResult> ValidateActionAsync(IAction action)
        {
            if (action == null)
            {
                return ValidationResult.Invalid("Action cannot be null", ValidationSeverity.High);
            }
            
            Logger?.Debug($"Validating action of type: {action.Type}");
            Metrics?.Increment("safety.validation.action.count");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check for restricted action types
                if (_restrictedActionTypes.Contains(action.Type))
                {
                    return ValidationResult.Invalid(
                        $"Action type '{action.Type}' is restricted",
                        "ActionTypeRestriction",
                        ValidationSeverity.High);
                }
                
                // Check each guardrail
                var result = new ValidationResult
                {
                    IsValid = true,
                    Message = "Validation passed"
                };
                
                foreach (var guardrail in _guardrails)
                {
                    var guardrailResult = await guardrail.EvaluateActionAsync(action);
                    result.GuardrailResults.Add(guardrailResult);
                    
                    if (!guardrailResult.Passed)
                    {
                        result.IsValid = false;
                        result.Message = guardrailResult.Message;
                        
                        // Use the highest severity found
                        if (MapSeverity(guardrailResult.Severity) > result.Severity)
                        {
                            result.Severity = MapSeverity(guardrailResult.Severity);
                        }
                    }
                }
                
                return result;
            }
            finally
            {
                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Metrics?.Histogram("safety.validation.action.duration", elapsedMs);
                Logger?.Debug($"Action validation completed in {elapsedMs}ms");
            }
        }
        
        /// <summary>
        /// Validates a text message for safety and compliance
        /// </summary>
        /// <param name="text">The text to validate</param>
        /// <param name="context">Optional additional context</param>
        /// <returns>The validation result</returns>
        public virtual async Task<ValidationResult> ValidateTextAsync(string text, IDictionary<string, object> context = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return ValidationResult.Valid();
            }
            
            Logger?.Debug("Validating text content");
            Metrics?.Increment("safety.validation.text.count");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                if (!EnableContentScanning)
                {
                    Logger?.Debug("Content scanning is disabled");
                    return ValidationResult.Valid();
                }
                
                // Check each guardrail
                var result = new ValidationResult
                {
                    IsValid = true,
                    Message = "Validation passed"
                };
                
                foreach (var guardrail in _guardrails)
                {
                    var guardrailResult = await guardrail.EvaluateTextAsync(text, context);
                    result.GuardrailResults.Add(guardrailResult);
                    
                    if (!guardrailResult.Passed)
                    {
                        result.IsValid = false;
                        result.Message = guardrailResult.Message;
                        
                        // Use the highest severity found
                        if (MapSeverity(guardrailResult.Severity) > result.Severity)
                        {
                            result.Severity = MapSeverity(guardrailResult.Severity);
                        }
                    }
                }
                
                return result;
            }
            finally
            {
                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Metrics?.Histogram("safety.validation.text.duration", elapsedMs);
                Logger?.Debug($"Text validation completed in {elapsedMs}ms");
            }
        }
        
        /// <summary>
        /// Adds a guardrail to the validator
        /// </summary>
        /// <param name="guardrail">The guardrail to add</param>
        public void AddGuardrail(IGuardrail guardrail)
        {
            if (guardrail == null)
            {
                throw new ArgumentNullException(nameof(guardrail));
            }
            
            _guardrails.Add(guardrail);
            Logger?.Debug($"Added guardrail: {guardrail.Name} ({guardrail.Id})");
        }
        
        /// <summary>
        /// Removes a guardrail from the validator
        /// </summary>
        /// <param name="guardrailId">The ID of the guardrail to remove</param>
        /// <returns>True if the guardrail was removed, false otherwise</returns>
        public bool RemoveGuardrail(string guardrailId)
        {
            var guardrail = _guardrails.FirstOrDefault(g => g.Id == guardrailId);
            if (guardrail != null)
            {
                _guardrails.Remove(guardrail);
                Logger?.Debug($"Removed guardrail: {guardrail.Name} ({guardrail.Id})");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets all guardrails registered with this validator
        /// </summary>
        /// <returns>A list of guardrails</returns>
        public IReadOnlyList<IGuardrail> GetGuardrails()
        {
            return _guardrails.AsReadOnly();
        }
        
        /// <summary>
        /// Adds a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to restrict</param>
        public void AddActionTypeRestriction(string actionType)
        {
            if (string.IsNullOrEmpty(actionType))
            {
                throw new ArgumentException("Action type cannot be null or empty", nameof(actionType));
            }
            
            _restrictedActionTypes.Add(actionType);
            Logger?.Debug($"Added action type restriction: {actionType}");
        }
        
        /// <summary>
        /// Removes a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to allow</param>
        /// <returns>True if the restriction was removed, false otherwise</returns>
        public bool RemoveActionTypeRestriction(string actionType)
        {
            var result = _restrictedActionTypes.Remove(actionType);
            if (result)
            {
                Logger?.Debug($"Removed action type restriction: {actionType}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all restricted action types
        /// </summary>
        /// <returns>A list of restricted action types</returns>
        public IReadOnlyList<string> GetRestrictedActionTypes()
        {
            return _restrictedActionTypes.ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Configures the validator from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        public virtual void Configure(SafetyConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            // Set basic configuration
            DefaultResponseMode = configuration.DefaultResponseMode;
            EnforceValidation = configuration.EnforceValidation;
            EnableContentScanning = configuration.EnableContentScanning;
            
            // Add restricted action types
            foreach (var actionType in configuration.RestrictedActionTypes)
            {
                AddActionTypeRestriction(actionType);
            }
            
            Logger?.Debug("Applied safety configuration");
        }
        
        /// <summary>
        /// Maps guardrail severity to validation severity
        /// </summary>
        /// <param name="guardrailSeverity">Guardrail severity</param>
        /// <returns>Equivalent validation severity</returns>
        protected virtual ValidationSeverity MapSeverity(GuardrailSeverity guardrailSeverity)
        {
            return guardrailSeverity switch
            {
                GuardrailSeverity.Info => ValidationSeverity.Low,
                GuardrailSeverity.Warning => ValidationSeverity.Medium,
                GuardrailSeverity.Error => ValidationSeverity.High,
                GuardrailSeverity.Critical => ValidationSeverity.Critical,
                _ => ValidationSeverity.Medium
            };
        }
    }
}
