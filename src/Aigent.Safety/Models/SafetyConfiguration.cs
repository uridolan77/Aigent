using System.Collections.Generic;

namespace Aigent.Safety.Models
{
    /// <summary>
    /// Configuration for safety validators
    /// </summary>
    public class SafetyConfiguration
    {
        /// <summary>
        /// List of guardrail configurations
        /// </summary>
        public List<GuardrailConfiguration> Guardrails { get; set; } = new List<GuardrailConfiguration>();
        
        /// <summary>
        /// List of restricted action types
        /// </summary>
        public List<string> RestrictedActionTypes { get; set; } = new List<string>();
        
        /// <summary>
        /// Default response mode for validation failures
        /// </summary>
        public FailureResponseMode DefaultResponseMode { get; set; } = FailureResponseMode.Block;
        
        /// <summary>
        /// Whether to log validation results
        /// </summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>
        /// Whether to collect metrics for validation operations
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
        
        /// <summary>
        /// Whether to enable content scanning
        /// </summary>
        public bool EnableContentScanning { get; set; } = true;
        
        /// <summary>
        /// Whether validation is enforced or just advisory
        /// </summary>
        public bool EnforceValidation { get; set; } = true;
        
        /// <summary>
        /// Additional configuration options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Response modes for validation failures
    /// </summary>
    public enum FailureResponseMode
    {
        /// <summary>
        /// Block the action entirely
        /// </summary>
        Block,
        
        /// <summary>
        /// Allow the action but with a warning
        /// </summary>
        Warn,
        
        /// <summary>
        /// Modify the action to make it safe
        /// </summary>
        Modify,
        
        /// <summary>
        /// Request human review before proceeding
        /// </summary>
        Review
    }
}
