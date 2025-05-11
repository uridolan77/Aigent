using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Interfaces
{
    /// <summary>
    /// Interface for safety guardrails
    /// </summary>
    public interface IGuardrail
    {
        /// <summary>
        /// Gets the unique identifier for this guardrail
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Gets the name of this guardrail
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the description of this guardrail
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Gets the severity level of this guardrail
        /// </summary>
        GuardrailSeverity Severity { get; }
        
        /// <summary>
        /// Evaluates an action against this guardrail
        /// </summary>
        /// <param name="action">The action to evaluate</param>
        /// <returns>The evaluation result</returns>
        Task<GuardrailEvaluationResult> EvaluateActionAsync(IAction action);
        
        /// <summary>
        /// Evaluates text against this guardrail
        /// </summary>
        /// <param name="text">The text to evaluate</param>
        /// <param name="context">Optional additional context</param>
        /// <returns>The evaluation result</returns>
        Task<GuardrailEvaluationResult> EvaluateTextAsync(string text, IDictionary<string, object> context = null);
        
        /// <summary>
        /// Configures the guardrail from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        void Configure(GuardrailConfiguration configuration);
    }
}
