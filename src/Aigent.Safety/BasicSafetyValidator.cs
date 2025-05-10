using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Monitoring;

namespace Aigent.Safety
{
    /// <summary>
    /// Basic safety validator implementation
    /// </summary>
    public class BasicSafetyValidator : ISafetyValidator
    {
        private readonly ILogger _logger;
        private readonly HashSet<string> _allowedActionTypes = new()
        {
            "TextOutput",
            "Planning",
            "Learning",
            "Recommendation",
            "Reasoning",
            "Adaptation"
        };
        
        /// <summary>
        /// Initializes a new instance of the BasicSafetyValidator class
        /// </summary>
        /// <param name="logger">Logger</param>
        public BasicSafetyValidator(ILogger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Validates an action
        /// </summary>
        /// <param name="action">Action to validate</param>
        /// <returns>Validation result</returns>
        public Task<ValidationResult> ValidateAction(IAction action)
        {
            if (action == null)
            {
                _logger?.Log(LogLevel.Warning, "Null action validation attempt");
                return Task.FromResult(ValidationResult.Invalid("Action cannot be null"));
            }
            
            // Check if action type is allowed
            if (!_allowedActionTypes.Contains(action.ActionType))
            {
                _logger?.Log(LogLevel.Warning, $"Disallowed action type: {action.ActionType}");
                return Task.FromResult(ValidationResult.Invalid($"Action type {action.ActionType} is not allowed"));
            }
            
            // Validate text output actions
            if (action.ActionType == "TextOutput")
            {
                if (!action.Parameters.TryGetValue("text", out var text) || string.IsNullOrEmpty(text?.ToString()))
                {
                    _logger?.Log(LogLevel.Warning, "TextOutput action with empty text");
                    return Task.FromResult(ValidationResult.Invalid("TextOutput action must have non-empty text"));
                }
                
                // Check for harmful content (simplified)
                var textContent = text.ToString();
                var harmfulPhrases = new[] { "hack", "exploit", "illegal", "harmful" };
                
                foreach (var phrase in harmfulPhrases)
                {
                    if (textContent.Contains(phrase))
                    {
                        _logger?.Log(LogLevel.Warning, $"Potentially harmful content detected: {phrase}");
                        return Task.FromResult(ValidationResult.Invalid($"Potentially harmful content detected: {phrase}"));
                    }
                }
            }
            
            _logger?.Log(LogLevel.Debug, $"Action validated: {action.ActionType}");
            return Task.FromResult(ValidationResult.Valid());
        }
    }
}
