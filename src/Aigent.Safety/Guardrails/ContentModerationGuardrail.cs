using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Monitoring;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Guardrails
{
    /// <summary>
    /// Guardrail that implements content moderation by checking for harmful or toxic content
    /// </summary>
    public class ContentModerationGuardrail : BaseGuardrail
    {
        private readonly ILogger _logger;
        private readonly List<string> _toxicPhrases = new List<string>();
        private readonly List<Regex> _toxicPatterns = new List<Regex>();
        private double _thresholdScore = 0.7;
        private bool _enableRegexScanning = true;
        
        /// <summary>
        /// Initializes a new instance of the ContentModerationGuardrail class
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="name">Name</param>
        /// <param name="description">Description</param>
        /// <param name="severity">Severity level</param>
        /// <param name="logger">Logger</param>
        public ContentModerationGuardrail(
            string id, 
            string name = "Content Moderation", 
            string description = "Checks for harmful or toxic content", 
            GuardrailSeverity severity = GuardrailSeverity.Error,
            ILogger logger = null) 
            : base(id, name, description, severity)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Configures the guardrail from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        public override void Configure(GuardrailConfiguration configuration)
        {
            base.Configure(configuration);
            
            if (configuration.Parameters.TryGetValue("toxicPhrases", out var toxicPhrases) &&
                toxicPhrases is List<string> phrasesList)
            {
                foreach (var phrase in phrasesList)
                {
                    AddToxicPhrase(phrase);
                }
            }
            
            if (configuration.Parameters.TryGetValue("toxicPatterns", out var toxicPatterns) &&
                toxicPatterns is List<string> patternsList)
            {
                foreach (var pattern in patternsList)
                {
                    AddToxicPattern(pattern);
                }
            }
            
            if (configuration.Parameters.TryGetValue("thresholdScore", out var thresholdScore) &&
                thresholdScore is double score)
            {
                _thresholdScore = score;
            }
            
            if (configuration.Parameters.TryGetValue("enableRegexScanning", out var enableRegexScanning) &&
                enableRegexScanning is bool enableScanning)
            {
                _enableRegexScanning = enableScanning;
            }
        }
        
        /// <summary>
        /// Adds a phrase to check for in content
        /// </summary>
        /// <param name="phrase">The phrase to check for</param>
        public void AddToxicPhrase(string phrase)
        {
            if (!string.IsNullOrEmpty(phrase) && !_toxicPhrases.Contains(phrase))
            {
                _toxicPhrases.Add(phrase);
            }
        }
        
        /// <summary>
        /// Adds a regex pattern to check for in content
        /// </summary>
        /// <param name="pattern">The regex pattern to check for</param>
        public void AddToxicPattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                try
                {                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    _toxicPatterns.Add(regex);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Invalid regex pattern '{pattern}': {ex.Message}");
                }
            }
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
            
            // For actions, we check if there are any text properties to evaluate
            var properties = action.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(string))
                .ToList();
            
            foreach (var property in properties)
            {
                var value = property.GetValue(action) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    var result = EvaluateText(value);
                    if (!result.Passed)
                    {
                        return Task.FromResult(result);
                    }
                }
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
            return Task.FromResult(EvaluateText(text));
        }
        
        private GuardrailEvaluationResult EvaluateText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return GuardrailEvaluationResult.Pass(Id, Name);
            }
            
            // Check for exact phrases
            foreach (var phrase in _toxicPhrases)
            {
                if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    return GuardrailEvaluationResult.Fail(
                        Id, 
                        Name, 
                        $"Content contains prohibited phrase: '{phrase}'", 
                        Severity);
                }
            }
            
            // Check for regex patterns
            if (_enableRegexScanning)
            {
                foreach (var pattern in _toxicPatterns)
                {
                    var match = pattern.Match(text);
                    if (match.Success)
                    {
                        return GuardrailEvaluationResult.Fail(
                            Id,
                            Name,
                            $"Content matches prohibited pattern: '{match.Value}'",
                            Severity);
                    }
                }
            }
            
            // Use a basic toxicity estimation algorithm
            // In a real implementation, this would likely use a more sophisticated 
            // ML-based toxicity detection service
            var toxicityScore = EstimateToxicity(text);
            
            if (toxicityScore > _thresholdScore)
            {
                var result = GuardrailEvaluationResult.Fail(
                    Id,
                    Name,
                    $"Content may contain harmful language (score: {toxicityScore:F2})",
                    Severity);
                
                result.ConfidenceScore = toxicityScore;
                return result;
            }
            
            return GuardrailEvaluationResult.Pass(Id, Name);
        }
        
        private double EstimateToxicity(string text)
        {
            // This is a placeholder for a real toxicity estimation algorithm
            // In a real implementation, this would use a pre-trained model or call
            // an external API for toxicity detection
            
            // For this example, we'll use a simple heuristic based on 
            // the presence of potentially harmful words
            var toxicWords = new[] 
            { 
                "hate", "violent", "kill", "attack", "harm", "threat",
                "abuse", "illegal", "racist", "offensive", "discriminatory"
            };
            
            int toxicWordCount = 0;
            
            foreach (var word in toxicWords)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    toxicWordCount++;
                }
            }
            
            // Calculate a simple toxicity score based on the presence of toxic words
            // This is just a demonstration - a real implementation would be more sophisticated
            return Math.Min(1.0, toxicWordCount / 5.0);
        }
    }
}
