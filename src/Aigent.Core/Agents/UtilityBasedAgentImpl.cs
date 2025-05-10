using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Implementation of IUtilityFunction using a linear combination of features
    /// </summary>
    public class LinearUtilityFunction : IUtilityFunction
    {
        private Dictionary<string, double> _weights = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the LinearUtilityFunction class
        /// </summary>
        /// <param name="logger">Logger for recording utility calculations</param>
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
            if (stateFeatures == null || !stateFeatures.Any())
            {
                _logger?.Log(LogLevel.Warning, "Attempted to calculate utility with null or empty state features");
                return 0.0;
            }
            
            double utility = 0.0;
            
            // Calculate weighted sum of features
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
            if (parameters == null)
            {
                _logger?.Log(LogLevel.Warning, "Attempted to set null parameters");
                return;
            }
            
            _weights = new Dictionary<string, double>(parameters);
            _logger?.Log(LogLevel.Debug, $"Updated utility function parameters with {parameters.Count} weights");
        }
    }

    /// <summary>
    /// Implementation of IActionGenerator
    /// </summary>
    public class BasicActionGenerator : IActionGenerator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the BasicActionGenerator class
        /// </summary>
        /// <param name="logger">Logger for recording action generation</param>
        public BasicActionGenerator(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generates possible actions for a state
        /// </summary>
        /// <param name="state">Current state</param>
        /// <returns>Possible actions</returns>
        public List<IAction> GenerateActions(EnvironmentState state)
        {
            if (state == null)
            {
                _logger?.Log(LogLevel.Warning, "Attempted to generate actions for null state");
                return new List<IAction>();
            }
            
            var actions = new List<IAction>();
            
            // Generate actions based on state properties
            if (state.Properties.ContainsKey("user_query"))
            {
                actions.Add(new TextOutputAction("I'll help you find information about that."));
            }
            
            if (state.Properties.ContainsKey("task_request"))
            {
                actions.Add(new TextOutputAction("I'll help you complete that task."));
            }
            
            if (state.Properties.ContainsKey("error_occurred"))
            {
                actions.Add(new TextOutputAction("I'll help you troubleshoot that error."));
            }
            
            // Add default actions if no specific actions were generated
            if (!actions.Any())
            {
                actions.Add(new TextOutputAction("I'm here to assist you. What would you like to do?"));
                actions.Add(new TextOutputAction("How can I help you today?"));
            }
            
            _logger?.Log(LogLevel.Debug, $"Generated {actions.Count} possible actions");
            return actions;
        }
    }

    /// <summary>
    /// Implementation of IStateEvaluator
    /// </summary>
    public class SimpleStateEvaluator : IStateEvaluator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the SimpleStateEvaluator class
        /// </summary>
        /// <param name="logger">Logger for recording state evaluation</param>
        public SimpleStateEvaluator(ILogger logger = null)
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
            if (state == null)
            {
                _logger?.Log(LogLevel.Warning, "Attempted to evaluate null state");
                return new Dictionary<string, double>();
            }
            
            var features = new Dictionary<string, double>();
            
            // Extract features from state properties
            if (state.Properties.TryGetValue("user_satisfaction", out var satisfaction) && satisfaction is double satisfactionValue)
            {
                features["user_satisfaction"] = satisfactionValue;
            }
            else
            {
                features["user_satisfaction"] = 0.5; // Default value
            }
            
            if (state.Properties.TryGetValue("task_completion", out var completion) && completion is double completionValue)
            {
                features["task_completion"] = completionValue;
            }
            else
            {
                features["task_completion"] = 0.0; // Default value
            }
            
            if (state.Properties.TryGetValue("efficiency", out var efficiency) && efficiency is double efficiencyValue)
            {
                features["efficiency"] = efficiencyValue;
            }
            else
            {
                features["efficiency"] = 0.5; // Default value
            }
            
            _logger?.Log(LogLevel.Debug, $"Evaluated state with {features.Count} features");
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
            if (state == null || action == null)
            {
                _logger?.Log(LogLevel.Warning, "Attempted to predict resulting state with null state or action");
                return new EnvironmentState();
            }
            
            // Create a copy of the current state
            var resultingState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>(state.Properties),
                Timestamp = DateTime.UtcNow
            };
            
            // Predict changes based on action type
            switch (action.ActionType)
            {
                case "TextOutput":
                    // Predict that user satisfaction will increase
                    resultingState.Properties["user_satisfaction"] = 
                        state.Properties.TryGetValue("user_satisfaction", out var satisfaction) && satisfaction is double satisfactionValue
                            ? Math.Min(1.0, satisfactionValue + 0.1)
                            : 0.6;
                    break;
                    
                case "Planning":
                    // Predict that task completion will increase
                    resultingState.Properties["task_completion"] = 
                        state.Properties.TryGetValue("task_completion", out var completion) && completion is double completionValue
                            ? Math.Min(1.0, completionValue + 0.2)
                            : 0.2;
                    break;
                    
                case "Learning":
                    // Predict that efficiency will increase
                    resultingState.Properties["efficiency"] = 
                        state.Properties.TryGetValue("efficiency", out var efficiency) && efficiency is double efficiencyValue
                            ? Math.Min(1.0, efficiencyValue + 0.1)
                            : 0.6;
                    break;
            }
            
            _logger?.Log(LogLevel.Debug, $"Predicted resulting state after action: {action.ActionType}");
            return resultingState;
        }
    }

    /// <summary>
    /// Implementation of IUtilityLearner using gradient descent
    /// </summary>
    public class GradientDescentUtilityLearner : IUtilityLearner
    {
        private readonly double _learningRate;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the GradientDescentUtilityLearner class
        /// </summary>
        /// <param name="learningRate">Learning rate for gradient descent</param>
        /// <param name="logger">Logger for recording learning</param>
        public GradientDescentUtilityLearner(double learningRate = 0.01, ILogger logger = null)
        {
            _learningRate = learningRate;
            _logger = logger;
        }

        /// <summary>
        /// Updates a utility function based on experience
        /// </summary>
        /// <param name="utilityFunction">Utility function to update</param>
        /// <param name="stateFeatures">Features of the state</param>
        /// <param name="action">Action taken</param>
        /// <param name="reward">Reward received</param>
        public void UpdateUtilityFunction(IUtilityFunction utilityFunction, Dictionary<string, double> stateFeatures, IAction action, double reward)
        {
            if (utilityFunction == null || stateFeatures == null || !stateFeatures.Any())
            {
                _logger?.Log(LogLevel.Warning, "Attempted to update utility function with null or invalid parameters");
                return;
            }
            
            // Get current parameters
            var parameters = utilityFunction.GetParameters();
            
            // Calculate current utility
            double currentUtility = utilityFunction.CalculateUtility(stateFeatures);
            
            // Calculate error
            double error = reward - currentUtility;
            
            // Update parameters using gradient descent
            var updatedParameters = new Dictionary<string, double>();
            foreach (var parameter in parameters)
            {
                if (stateFeatures.TryGetValue(parameter.Key, out var featureValue))
                {
                    // Update weight: w = w + learning_rate * error * feature_value
                    updatedParameters[parameter.Key] = parameter.Value + _learningRate * error * featureValue;
                }
                else
                {
                    updatedParameters[parameter.Key] = parameter.Value;
                }
            }
            
            // Set updated parameters
            utilityFunction.SetParameters(updatedParameters);
            
            _logger?.Log(LogLevel.Debug, $"Updated utility function parameters (error: {error}, reward: {reward})");
        }
    }

    /// <summary>
    /// Factory for creating utility-based agents
    /// </summary>
    public static class UtilityBasedAgentFactory
    {
        /// <summary>
        /// Creates a utility-based agent with default components
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        /// <returns>Utility-based agent</returns>
        public static UtilityBasedAgent CreateDefaultUtilityBasedAgent(
            string name,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            var utilityFunction = new LinearUtilityFunction(logger);
            var actionGenerator = new BasicActionGenerator(logger);
            var stateEvaluator = new SimpleStateEvaluator(logger);
            var utilityLearner = new GradientDescentUtilityLearner(0.01, logger);
            
            return new UtilityBasedAgent(
                name,
                utilityFunction,
                actionGenerator,
                stateEvaluator,
                utilityLearner,
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);
        }
    }
}
