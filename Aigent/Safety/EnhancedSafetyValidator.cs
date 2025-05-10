using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Monitoring;

namespace Aigent.Safety
{
    /// <summary>
    /// Enhanced implementation of ISafetyValidator with multiple guardrails and ethics
    /// </summary>
    public class EnhancedSafetyValidator : ISafetyValidator
    {
        private readonly List<IGuardrail> _guardrails = new();
        private readonly HashSet<string> _restrictedActionTypes = new();
        private readonly ILogger _logger;
        private readonly IEthicsEngine _ethicsEngine;
        private readonly IMetricsCollector _metrics;

        /// <summary>
        /// Initializes a new instance of the EnhancedSafetyValidator class
        /// </summary>
        /// <param name="logger">Logger for recording validation activities</param>
        /// <param name="ethicsEngine">Ethics engine for ethical validation</param>
        /// <param name="metrics">Metrics collector for monitoring validation performance</param>
        public EnhancedSafetyValidator(
            ILogger logger, 
            IEthicsEngine ethicsEngine = null,
            IMetricsCollector metrics = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ethicsEngine = ethicsEngine;
            _metrics = metrics;
        }

        /// <summary>
        /// Adds a guardrail to the validator
        /// </summary>
        /// <param name="guardrail">The guardrail to add</param>
        public void AddGuardrail(IGuardrail guardrail)
        {
            _guardrails.Add(guardrail);
            _logger.Log(LogLevel.Information, $"Added guardrail: {guardrail.Name}");
        }

        /// <summary>
        /// Adds a restriction for a specific action type
        /// </summary>
        /// <param name="actionType">The action type to restrict</param>
        public void AddActionTypeRestriction(string actionType)
        {
            _restrictedActionTypes.Add(actionType);
            _logger.Log(LogLevel.Information, $"Restricted action type: {actionType}");
        }

        /// <summary>
        /// Validates an action for safety and compliance
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <returns>The validation result</returns>
        public async Task<ValidationResult> ValidateAction(IAction action)
        {
            _metrics?.StartOperation("safety_validate_action");
            
            try
            {
                // Check restricted action types
                if (_restrictedActionTypes.Contains(action.ActionType))
                {
                    _logger.Log(LogLevel.Warning, $"Attempted restricted action type: {action.ActionType}");
                    _metrics?.RecordMetric("safety.restricted_action_attempt", 1.0);
                    
                    return ValidationResult.Failure($"Action type '{action.ActionType}' is restricted");
                }

                var violations = new List<string>();

                // Run through all guardrails
                foreach (var guardrail in _guardrails)
                {
                    var result = await guardrail.Validate(action);
                    if (!result.IsValid)
                    {
                        violations.AddRange(result.Violations);
                        _logger.Log(LogLevel.Warning, $"Guardrail {guardrail.Name} validation failed: {result.Message}");
                        _metrics?.RecordMetric($"safety.guardrail.{guardrail.Name}.violation", 1.0);
                    }
                }

                if (violations.Any())
                {
                    _metrics?.RecordMetric("safety.validation_failure", 1.0);
                    
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Action validation failed",
                        Violations = violations
                    };
                }

                _metrics?.RecordMetric("safety.validation_success", 1.0);
                return ValidationResult.Success();
            }
            finally
            {
                _metrics?.EndOperation("safety_validate_action");
            }
        }
    }

    /// <summary>
    /// Interface for ethics engines that validate actions against ethical guidelines
    /// </summary>
    public interface IEthicsEngine
    {
        /// <summary>
        /// Validates parameters against ethical guidelines
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>Whether the parameters are ethically valid</returns>
        Task<bool> ValidateAsync(Dictionary<string, object> parameters);
    }

    /// <summary>
    /// Guardrail that enforces ethical constraints
    /// </summary>
    public class EthicalConstraintGuardrail : IGuardrail
    {
        /// <summary>
        /// Name of the guardrail
        /// </summary>
        public string Name => "Ethical Constraints";
        
        private readonly IEthicsEngine _ethicsEngine;
        private readonly List<string> _ethicalGuidelines;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the EthicalConstraintGuardrail class
        /// </summary>
        /// <param name="ethicsEngine">Ethics engine for validation</param>
        /// <param name="ethicalGuidelines">List of ethical guidelines</param>
        /// <param name="logger">Logger for recording validation activities</param>
        public EthicalConstraintGuardrail(
            IEthicsEngine ethicsEngine, 
            List<string> ethicalGuidelines,
            ILogger logger = null)
        {
            _ethicsEngine = ethicsEngine ?? throw new ArgumentNullException(nameof(ethicsEngine));
            _ethicalGuidelines = ethicalGuidelines ?? throw new ArgumentNullException(nameof(ethicalGuidelines));
            _logger = logger;
        }

        /// <summary>
        /// Validates an action against ethical constraints
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <returns>The validation result</returns>
        public async Task<ValidationResult> Validate(IAction action)
        {
            try
            {
                var isValid = await _ethicsEngine.ValidateAsync(action.Parameters);
                
                if (!isValid)
                {
                    _logger?.Log(LogLevel.Warning, $"Action violates ethical guidelines: {action.ActionType}");
                    return ValidationResult.Failure("Action violates ethical guidelines");
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error in ethical validation: {ex.Message}");
                return ValidationResult.Failure($"Error in ethical validation: {ex.Message}");
            }
        }
    }
}
