using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core.Interfaces;
using Aigent.Safety.Interfaces;
using Aigent.Safety.Models;

namespace Aigent.Safety.Guardrails
{
    /// <summary>
    /// Base implementation of a guardrail
    /// </summary>
    public abstract class BaseGuardrail : IGuardrail
    {
        /// <summary>
        /// Gets the unique identifier for this guardrail
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// Gets the name of this guardrail
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the description of this guardrail
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Gets the severity level of this guardrail
        /// </summary>
        public GuardrailSeverity Severity { get; protected set; }
        
        /// <summary>
        /// Gets whether this guardrail is enabled
        /// </summary>
        protected bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Gets the parameters for this guardrail
        /// </summary>
        protected Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Initializes a new instance of the BaseGuardrail class
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="name">Name</param>
        /// <param name="description">Description</param>
        /// <param name="severity">Severity level</param>
        protected BaseGuardrail(string id, string name, string description, GuardrailSeverity severity)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Severity = severity;
        }
        
        /// <summary>
        /// Evaluates an action against this guardrail
        /// </summary>
        /// <param name="action">The action to evaluate</param>
        /// <returns>The evaluation result</returns>
        public virtual Task<GuardrailEvaluationResult> EvaluateActionAsync(IAction action)
        {
            if (!Enabled)
            {
                return Task.FromResult(GuardrailEvaluationResult.Pass(Id, Name));
            }
            
            if (action == null)
            {
                return Task.FromResult(GuardrailEvaluationResult.Fail(
                    Id, Name, "Action cannot be null", Severity));
            }
            
            return EvaluateActionInternalAsync(action);
        }
        
        /// <summary>
        /// Evaluates text against this guardrail
        /// </summary>
        /// <param name="text">The text to evaluate</param>
        /// <param name="context">Optional additional context</param>
        /// <returns>The evaluation result</returns>
        public virtual Task<GuardrailEvaluationResult> EvaluateTextAsync(string text, IDictionary<string, object> context = null)
        {
            if (!Enabled)
            {
                return Task.FromResult(GuardrailEvaluationResult.Pass(Id, Name));
            }
            
            if (string.IsNullOrEmpty(text))
            {
                return Task.FromResult(GuardrailEvaluationResult.Pass(Id, Name));
            }
            
            return EvaluateTextInternalAsync(text, context);
        }
        
        /// <summary>
        /// Configures the guardrail from a configuration object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        public virtual void Configure(GuardrailConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            Enabled = configuration.Enabled;
            Severity = configuration.Severity;
            
            // Copy parameters
            foreach (var parameter in configuration.Parameters)
            {
                Parameters[parameter.Key] = parameter.Value;
            }
        }
        
        /// <summary>
        /// Internal implementation of action evaluation
        /// </summary>
        /// <param name="action">The action to evaluate</param>
        /// <returns>The evaluation result</returns>
        protected abstract Task<GuardrailEvaluationResult> EvaluateActionInternalAsync(IAction action);
        
        /// <summary>
        /// Internal implementation of text evaluation
        /// </summary>
        /// <param name="text">The text to evaluate</param>
        /// <param name="context">Optional additional context</param>
        /// <returns>The evaluation result</returns>
        protected abstract Task<GuardrailEvaluationResult> EvaluateTextInternalAsync(string text, IDictionary<string, object> context = null);
        
        /// <summary>
        /// Gets a parameter value
        /// </summary>
        /// <typeparam name="T">Type of the parameter</typeparam>
        /// <param name="key">Key of the parameter</param>
        /// <param name="defaultValue">Default value if the parameter is not found</param>
        /// <returns>Parameter value</returns>
        protected T GetParameterValue<T>(string key, T defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
                try
                {
                    // Try to convert the value
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // Conversion failed, return default
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
    }
}
