using System.Collections.Generic;

namespace Aigent.Safety.Models
{
    /// <summary>
    /// Configuration for guardrails
    /// </summary>
    public class GuardrailConfiguration
    {
        /// <summary>
        /// Unique identifier for the guardrail
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the guardrail
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the guardrail
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Type of the guardrail
        /// </summary>
        public GuardrailType Type { get; set; }
        
        /// <summary>
        /// Severity level of the guardrail
        /// </summary>
        public GuardrailSeverity Severity { get; set; } = GuardrailSeverity.Error;
        
        /// <summary>
        /// Whether the guardrail is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Response mode for this guardrail
        /// </summary>
        public FailureResponseMode ResponseMode { get; set; } = FailureResponseMode.Block;
        
        /// <summary>
        /// Additional configuration parameters for the guardrail
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Types of guardrails
    /// </summary>
    public enum GuardrailType
    {
        /// <summary>
        /// Checks for sensitive or personally identifiable information
        /// </summary>
        PiiDetection,
        
        /// <summary>
        /// Checks for harmful or toxic content
        /// </summary>
        ContentModeration,
        
        /// <summary>
        /// Checks for profanity
        /// </summary>
        ProfanityFilter,
        
        /// <summary>
        /// Checks for jailbreaking attempts
        /// </summary>
        JailbreakDetection,
        
        /// <summary>
        /// Checks for prompt injection
        /// </summary>
        PromptInjectionDetection,
        
        /// <summary>
        /// Checks for security vulnerabilities
        /// </summary>
        SecurityCheck,
        
        /// <summary>
        /// Checks for compliance with specific regulations
        /// </summary>
        ComplianceCheck,
        
        /// <summary>
        /// Checks for bias in content
        /// </summary>
        BiasDetection,
        
        /// <summary>
        /// Custom guardrail type
        /// </summary>
        Custom
    }
}
