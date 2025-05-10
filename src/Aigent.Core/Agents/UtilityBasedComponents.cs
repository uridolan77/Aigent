using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Linear utility function implementation
    /// </summary>
    public class LinearUtilityFunction : IUtilityFunction
    {
        private Dictionary<string, double> _weights = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the LinearUtilityFunction class
        /// </summary>
        /// <param name="logger">Logger for recording utility function operations</param>
        public LinearUtilityFunction(ILogger logger = null)
        {
            _logger = logger;
            
            // Initialize with default weights
            _weights["user_satisfaction"] = 0.5;
            _weights["task_completion"] = 0.3;
            _weights["efficiency"] = 0.2;
        }

        /// <summary>
        /// Calculates utility of a state
        /// </summary>
        /// <param name="stateFeatures">Features of the state</param>
        /// <returns>Utility value</returns>
        public double CalculateUtility(Dictionary<string, double> stateFeatures)
        {
            double utility = 0.0;
            
            foreach (var feature in stateFeatures)
            {
                if (_weights.TryGetValue(feature.Key, out var weight))
                {
                    utility += weight * feature.Value;
                }
            }
            
            _logger?.Log(LogLevel.Debug, $"Calculated utility: {utility}");
            return utility;
        }

        /// <summary>
        /// Gets parameters of the utility function
        /// </summary>
        /// <returns>Parameters of the utility function</returns>
        public Dictionary<string, double> GetParameters()
        {
            return new Dictionary<string, double>(_weights);
        }

        /// <summary>
        /// Sets parameters of the utility function
        /// </summary>
        /// <param name="parameters">Parameters to set</param>
        public void SetParameters(Dictionary<string, double> parameters)
        {
            _weights = new Dictionary<string, double>(parameters);
            _logger?.Log(LogLevel.Debug, $"Set utility function parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
        }
    }

    /// <summary>
    /// Rule-based action generator implementation
    /// </summary>
    public class RuleBasedActionGenerator : IActionGenerator
    {
        private readonly List<ActionGenerationRule> _rules = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the RuleBasedActionGenerator class
        /// </summary>
        /// <param name="logger">Logger for recording action generation</param>
        public RuleBasedActionGenerator(ILogger logger = null)
        {
            _logger = logger;
            
            // Add default rules
            AddRule(new ActionGenerationRule
            {
                Condition = state => state.Properties.TryGetValue("input", out var input) && 
                                    input is string inputStr && 
                                    inputStr.Contains("help", StringComparison.OrdinalIgnoreCase),
                ActionFactory = _ => new TextOutputAction("I'm here to help. What do you need assistance with?")
            });
            
            AddRule(new ActionGenerationRule
            {
                Condition = state => state.Properties.TryGetValue("input", out var input) && 
                                    input is string inputStr && 
                                    inputStr.Contains("plan", StringComparison.OrdinalIgnoreCase),
                ActionFactory = _ => new TextOutputAction("Let's create a plan. What are you trying to accomplish?")
            });
            
            AddRule(new ActionGenerationRule
            {
                Condition = state => state.Properties.TryGetValue("input", out var input) && 
                                    input is string inputStr && 
                                    (inputStr.Contains("urgent", StringComparison.OrdinalIgnoreCase) || 
                                     inputStr.Contains("emergency", StringComparison.OrdinalIgnoreCase)),
                ActionFactory = _ => new TextOutputAction("This is an emergency situation. I'll prioritize this task immediately.")
            });
            
            // Default action rule (always applies)
            AddRule(new ActionGenerationRule
            {
                Condition = _ => true,
                ActionFactory = state => new TextOutputAction("I understand your request and I'm processing it.")
            });
        }

        /// <summary>
        /// Adds a rule for action generation
        /// </summary>
        /// <param name="rule">Rule to add</param>
        public void AddRule(ActionGenerationRule rule)
        {
            _rules.Add(rule);
        }

        /// <summary>
        /// Generates possible actions for a state
        /// </summary>
        /// <param name="state">Current state</param>
        /// <returns>Possible actions</returns>
        public List<IAction> GenerateActions(EnvironmentState state)
        {
            var actions = new List<IAction>();
            
            foreach (var rule in _rules)
            {
                try
                {
                    if (rule.Condition(state))
                    {
                        var action = rule.ActionFactory(state);
                        actions.Add(action);
                        _logger?.Log(LogLevel.Debug, $"Generated action: {action.ActionType}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error in action generation rule: {ex.Message}", ex);
                }
            }
            
            return actions;
        }
    }

    /// <summary>
    /// Rule for generating actions
    /// </summary>
    public class ActionGenerationRule
    {
        /// <summary>
        /// Condition for the rule
        /// </summary>
        public Func<EnvironmentState, bool> Condition { get; set; }
        
        /// <summary>
        /// Factory for creating actions
        /// </summary>
        public Func<EnvironmentState, IAction> ActionFactory { get; set; }
    }

    /// <summary>
    /// Feature-based state evaluator implementation
    /// </summary>
    public class FeatureBasedStateEvaluator : IStateEvaluator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the FeatureBasedStateEvaluator class
        /// </summary>
        /// <param name="logger">Logger for recording state evaluation</param>
        public FeatureBasedStateEvaluator(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Evaluates features of a state
        /// </summary>
        /// <param name="state">State to evaluate</param>
        /// <returns>Features of the state</returns>
        public Dictionary<string, double> EvaluateState(EnvironmentState state)
        {
            var features = new Dictionary<string, double>();
            
            // Extract user satisfaction feature
            if (state.Properties.TryGetValue("user_satisfaction", out var satisfaction) && satisfaction is double satisfactionValue)
            {
                features["user_satisfaction"] = satisfactionValue;
            }
            else
            {
                // Estimate user satisfaction from input
                if (state.Properties.TryGetValue("input", out var input) && input is string inputStr)
                {
                    if (inputStr.Contains("thank", StringComparison.OrdinalIgnoreCase) || 
                        inputStr.Contains("great", StringComparison.OrdinalIgnoreCase) ||
                        inputStr.Contains("good", StringComparison.OrdinalIgnoreCase))
                    {
                        features["user_satisfaction"] = 0.8;
                    }
                    else if (inputStr.Contains("bad", StringComparison.OrdinalIgnoreCase) || 
                             inputStr.Contains("wrong", StringComparison.OrdinalIgnoreCase) ||
                             inputStr.Contains("not", StringComparison.OrdinalIgnoreCase))
                    {
                        features["user_satisfaction"] = 0.2;
                    }
                    else
                    {
                        features["user_satisfaction"] = 0.5;
                    }
                }
                else
                {
                    features["user_satisfaction"] = 0.5;
                }
            }
            
            // Extract task completion feature
            if (state.Properties.TryGetValue("task_completion", out var completion) && completion is double completionValue)
            {
                features["task_completion"] = completionValue;
            }
            else
            {
                features["task_completion"] = 0.0;
            }
            
            // Extract efficiency feature
            if (state.Properties.TryGetValue("efficiency", out var efficiency) && efficiency is double efficiencyValue)
            {
                features["efficiency"] = efficiencyValue;
            }
            else
            {
                features["efficiency"] = 0.5;
            }
            
            _logger?.Log(LogLevel.Debug, $"Evaluated state features: {string.Join(", ", features.Select(f => $"{f.Key}={f.Value}"))}");
            return features;
        }

        /// <summary>
        /// Predicts the resulting state after taking an action
        /// </summary>
        /// <param name="state">Current state</param>
        /// <param name="action">Action to take</param>
        /// <returns>Predicted resulting state</returns>
        public EnvironmentState PredictResultingState(EnvironmentState state, IAction action)
        {
            var resultingState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>(state.Properties),
                Timestamp = DateTime.UtcNow
            };
            
            // Predict changes based on action type
            switch (action.ActionType)
            {
                case "TextOutput":
                    if (action.Parameters.TryGetValue("text", out var text) && text is string textStr)
                    {
                        // Predict user satisfaction based on text content
                        if (textStr.Contains("help", StringComparison.OrdinalIgnoreCase) || 
                            textStr.Contains("assist", StringComparison.OrdinalIgnoreCase))
                        {
                            resultingState.Properties["user_satisfaction"] = 0.7;
                        }
                        else if (textStr.Contains("error", StringComparison.OrdinalIgnoreCase) || 
                                 textStr.Contains("can't", StringComparison.OrdinalIgnoreCase) ||
                                 textStr.Contains("unable", StringComparison.OrdinalIgnoreCase))
                        {
                            resultingState.Properties["user_satisfaction"] = 0.3;
                        }
                        else
                        {
                            resultingState.Properties["user_satisfaction"] = 0.5;
                        }
                        
                        // Predict task completion
                        resultingState.Properties["task_completion"] = 0.5;
                    }
                    break;
                    
                case "Planning":
                    resultingState.Properties["user_satisfaction"] = 0.8;
                    resultingState.Properties["task_completion"] = 0.7;
                    break;
                    
                case "Recommendation":
                    resultingState.Properties["user_satisfaction"] = 0.7;
                    resultingState.Properties["task_completion"] = 0.6;
                    break;
                    
                default:
                    resultingState.Properties["user_satisfaction"] = 0.5;
                    resultingState.Properties["task_completion"] = 0.5;
                    break;
            }
            
            // Predict efficiency (always high for now)
            resultingState.Properties["efficiency"] = 0.8;
            
            _logger?.Log(LogLevel.Debug, $"Predicted resulting state for action {action.ActionType}");
            return resultingState;
        }
    }
}
