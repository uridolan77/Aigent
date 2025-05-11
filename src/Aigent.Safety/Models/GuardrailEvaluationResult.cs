using System.Collections.Generic;

namespace Aigent.Safety.Models
{
    /// <summary>
    /// Result of a guardrail evaluation
    /// </summary>
    public class GuardrailEvaluationResult
    {
        /// <summary>
        /// ID of the guardrail that produced this result
        /// </summary>
        public string GuardrailId { get; set; }
        
        /// <summary>
        /// Name of the guardrail that produced this result
        /// </summary>
        public string GuardrailName { get; set; }
        
        /// <summary>
        /// Whether the guardrail evaluation passed
        /// </summary>
        public bool Passed { get; set; }
        
        /// <summary>
        /// Message describing the evaluation result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Severity of the guardrail
        /// </summary>
        public GuardrailSeverity Severity { get; set; }
        
        /// <summary>
        /// Category of the guardrail
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Score representing the confidence in the evaluation (0-1)
        /// </summary>
        public double? ConfidenceScore { get; set; }
        
        /// <summary>
        /// Additional metadata for the evaluation result
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a passed evaluation result
        /// </summary>
        /// <param name="guardrailId">ID of the guardrail</param>
        /// <param name="guardrailName">Name of the guardrail</param>
        /// <returns>Passed evaluation result</returns>
        public static GuardrailEvaluationResult Pass(string guardrailId, string guardrailName)
        {
            return new GuardrailEvaluationResult
            {
                GuardrailId = guardrailId,
                GuardrailName = guardrailName,
                Passed = true,
                Message = "Guardrail check passed"
            };
        }
        
        /// <summary>
        /// Creates a failed evaluation result
        /// </summary>
        /// <param name="guardrailId">ID of the guardrail</param>
        /// <param name="guardrailName">Name of the guardrail</param>
        /// <param name="message">Error message</param>
        /// <param name="severity">Severity of the failure</param>
        /// <returns>Failed evaluation result</returns>
        public static GuardrailEvaluationResult Fail(string guardrailId, string guardrailName, string message, GuardrailSeverity severity)
        {
            return new GuardrailEvaluationResult
            {
                GuardrailId = guardrailId,
                GuardrailName = guardrailName,
                Passed = false,
                Message = message,
                Severity = severity
            };
        }
    }
    
    /// <summary>
    /// Severity levels for guardrails
    /// </summary>
    public enum GuardrailSeverity
    {
        /// <summary>
        /// Information only, does not block actions
        /// </summary>
        Info,
        
        /// <summary>
        /// Warning, may or may not block actions
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error, blocks actions
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical error, blocks actions and logs incidents
        /// </summary>
        Critical
    }
}
