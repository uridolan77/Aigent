using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Monitoring;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Guardrails
{
    /// <summary>
    /// Guardrail that filters profanity from content
    /// </summary>
    public class ProfanityFilterGuardrail : BaseGuardrail
    {
        private readonly ILogger _logger;
        private readonly HashSet<string> _profanityList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _enableMasking = true;
        private string _maskCharacter = "*";
        
        /// <summary>
        /// Initializes a new instance of the ProfanityFilterGuardrail class
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="name">Name</param>
        /// <param name="description">Description</param>
        /// <param name="severity">Severity level</param>
        /// <param name="logger">Logger</param>
        public ProfanityFilterGuardrail(
            string id, 
            string name = "Profanity Filter", 
            string description = "Filters profanity from content", 
            GuardrailSeverity severity = GuardrailSeverity.Warning,
            ILogger logger = null) 
            : base(id, name, description, severity)
        {
            _logger = logger;
            
            // Add some basic profanity words
            // In a real implementation, this would be loaded from a more comprehensive dictionary
            AddProfanityWords(new[]
            {
                "profanity1",
                "profanity2",
                "profanity3"
            });
        }
        
        /// <summary>
        /// Configures the guardrail from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        public override void Configure(GuardrailConfiguration configuration)
        {
            base.Configure(configuration);
            
            if (configuration.Parameters.TryGetValue("profanityList", out var profanityList) &&
                profanityList is List<string> wordsList)
            {
                AddProfanityWords(wordsList);
            }
            
            if (configuration.Parameters.TryGetValue("enableMasking", out var enableMasking) &&
                enableMasking is bool masking)
            {
                _enableMasking = masking;
            }
            
            if (configuration.Parameters.TryGetValue("maskCharacter", out var maskChar) &&
                maskChar is string character && !string.IsNullOrEmpty(character))
            {
                _maskCharacter = character;
            }
        }
        
        /// <summary>
        /// Adds profanity words to the filter
        /// </summary>
        /// <param name="words">The words to add</param>
        public void AddProfanityWords(IEnumerable<string> words)
        {
            if (words == null)
            {
                return;
            }
            
            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    _profanityList.Add(word.Trim());
                }
            }
            
            _logger?.LogDebug($"Updated profanity list with {_profanityList.Count} words");
        }
        
        /// <summary>
        /// Masks profanity in text
        /// </summary>
        /// <param name="text">The text to mask</param>
        /// <returns>The masked text</returns>
        public string MaskProfanity(string text)
        {
            if (string.IsNullOrEmpty(text) || !_enableMasking)
            {
                return text;
            }
            
            var result = text;
            
            foreach (var word in _profanityList)
            {
                var pattern = $"\\b{Regex.Escape(word)}\\b";
                var replacement = new string(_maskCharacter[0], word.Length);
                result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
            }
            
            return result;
        }
        
        /// <summary>
        /// Internal implementation of action evaluation
        /// </summary>
        /// <param name="action">The action to evaluate</param>
        /// <returns>The evaluation result</returns>
        protected override Task<GuardrailEvaluationResult> EvaluateActionInternalAsync(IAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            
            // Check for text properties and validate them
            var properties = action.GetType().GetProperties();
            bool profanityFound = false;
            
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    var text = property.GetValue(action) as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        foreach (var word in _profanityList)
                        {
                            var pattern = $"\\b{Regex.Escape(word)}\\b";
                            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                            {
                                profanityFound = true;
                                break;
                            }
                        }
                        
                        if (profanityFound)
                        {
                            break;
                        }
                    }
                }
            }
            
            if (profanityFound)
            {
                var result = GuardrailEvaluationResult.Fail(
                    Id,
                    Name,
                    "Profanity detected in content",
                    Severity);
                
                if (_enableMasking)
                {
                    result.Metadata["canFilter"] = true;
                }
                
                return Task.FromResult(result);
            }
            
            return Task.FromResult(GuardrailEvaluationResult.Pass(Id, Name));
        }
        
        /// <summary>
        /// Internal implementation of text evaluation
        /// </summary>
        /// <param name="text">The text to evaluate</param>
        /// <param name="context">Optional additional context</param>
        /// <returns>The evaluation result</returns>
        protected override Task<GuardrailEvaluationResult> EvaluateTextInternalAsync(string text, IDictionary<string, object> context = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Task.FromResult(GuardrailEvaluationResult.Pass(Id, Name));
            }
            
            bool profanityFound = false;
            string foundWord = null;
            
            foreach (var word in _profanityList)
            {
                var pattern = $"\\b{Regex.Escape(word)}\\b";
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    profanityFound = true;
                    foundWord = match.Value;
                    break;
                }
            }
            
            if (profanityFound)
            {
                var result = GuardrailEvaluationResult.Fail(
                    Id,
                    Name,
                    $"Profanity detected: '{foundWord}'",
                    Severity);
                
                if (_enableMasking)
                {
                    result.Metadata["canFilter"] = true;
                    result.Metadata["filteredContent"] = MaskProfanity(text);
                }
                
                return Task.FromResult(result);
            }
            
            return Task.FromResult(GuardrailEvaluationResult.Pass(Id, Name));
        }
    }
}
