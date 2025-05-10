using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Gradient descent utility learner implementation
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
        public GradientDescentUtilityLearner(double learningRate = 0.1, ILogger logger = null)
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
            // Get current parameters
            var parameters = utilityFunction.GetParameters();
            
            // Calculate current utility
            double currentUtility = utilityFunction.CalculateUtility(stateFeatures);
            
            // Calculate error
            double error = reward - currentUtility;
            
            // Update parameters using gradient descent
            var updatedParameters = new Dictionary<string, double>();
            
            foreach (var param in parameters)
            {
                if (stateFeatures.TryGetValue(param.Key, out var featureValue))
                {
                    // Update weight: w = w + learning_rate * error * feature_value
                    double updatedWeight = param.Value + _learningRate * error * featureValue;
                    updatedParameters[param.Key] = updatedWeight;
                }
                else
                {
                    updatedParameters[param.Key] = param.Value;
                }
            }
            
            // Set updated parameters
            utilityFunction.SetParameters(updatedParameters);
            
            _logger?.Log(LogLevel.Debug, $"Updated utility function parameters with error {error}");
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
            var actionGenerator = new RuleBasedActionGenerator(logger);
            var stateEvaluator = new FeatureBasedStateEvaluator(logger);
            var utilityLearner = new GradientDescentUtilityLearner(0.1, logger);
            
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

        /// <summary>
        /// Creates a utility-based agent with custom components
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="utilityFunction">Utility function</param>
        /// <param name="actionGenerator">Action generator</param>
        /// <param name="stateEvaluator">State evaluator</param>
        /// <param name="utilityLearner">Utility learner</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        /// <returns>Utility-based agent</returns>
        public static UtilityBasedAgent CreateCustomUtilityBasedAgent(
            string name,
            IUtilityFunction utilityFunction,
            IActionGenerator actionGenerator,
            IStateEvaluator stateEvaluator,
            IUtilityLearner utilityLearner,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
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

    /// <summary>
    /// Advanced utility function that combines multiple utility components
    /// </summary>
    public class CompositeUtilityFunction : IUtilityFunction
    {
        private readonly Dictionary<string, IUtilityFunction> _components = new();
        private readonly Dictionary<string, double> _weights = new();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the CompositeUtilityFunction class
        /// </summary>
        /// <param name="logger">Logger for recording utility function operations</param>
        public CompositeUtilityFunction(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a component utility function
        /// </summary>
        /// <param name="name">Name of the component</param>
        /// <param name="component">Component utility function</param>
        /// <param name="weight">Weight of the component</param>
        public void AddComponent(string name, IUtilityFunction component, double weight)
        {
            _components[name] = component;
            _weights[name] = weight;
            _logger?.Log(LogLevel.Debug, $"Added utility component: {name} with weight {weight}");
        }

        /// <summary>
        /// Calculates utility of a state
        /// </summary>
        /// <param name="stateFeatures">Features of the state</param>
        /// <returns>Utility value</returns>
        public double CalculateUtility(Dictionary<string, double> stateFeatures)
        {
            double utility = 0.0;
            double totalWeight = 0.0;
            
            foreach (var component in _components)
            {
                var weight = _weights[component.Key];
                var componentUtility = component.Value.CalculateUtility(stateFeatures);
                utility += weight * componentUtility;
                totalWeight += weight;
            }
            
            // Normalize by total weight
            if (totalWeight > 0)
            {
                utility /= totalWeight;
            }
            
            _logger?.Log(LogLevel.Debug, $"Calculated composite utility: {utility}");
            return utility;
        }

        /// <summary>
        /// Gets parameters of the utility function
        /// </summary>
        /// <returns>Parameters of the utility function</returns>
        public Dictionary<string, double> GetParameters()
        {
            var parameters = new Dictionary<string, double>();
            
            // Include component weights
            foreach (var weight in _weights)
            {
                parameters[$"weight_{weight.Key}"] = weight.Value;
            }
            
            // Include component parameters
            foreach (var component in _components)
            {
                var componentParams = component.Value.GetParameters();
                foreach (var param in componentParams)
                {
                    parameters[$"{component.Key}_{param.Key}"] = param.Value;
                }
            }
            
            return parameters;
        }

        /// <summary>
        /// Sets parameters of the utility function
        /// </summary>
        /// <param name="parameters">Parameters to set</param>
        public void SetParameters(Dictionary<string, double> parameters)
        {
            // Set component weights
            foreach (var param in parameters.Where(p => p.Key.StartsWith("weight_")))
            {
                var componentName = param.Key.Substring("weight_".Length);
                if (_weights.ContainsKey(componentName))
                {
                    _weights[componentName] = param.Value;
                }
            }
            
            // Set component parameters
            foreach (var component in _components)
            {
                var componentParams = new Dictionary<string, double>();
                var prefix = $"{component.Key}_";
                
                foreach (var param in parameters.Where(p => p.Key.StartsWith(prefix)))
                {
                    var paramName = param.Key.Substring(prefix.Length);
                    componentParams[paramName] = param.Value;
                }
                
                if (componentParams.Any())
                {
                    component.Value.SetParameters(componentParams);
                }
            }
            
            _logger?.Log(LogLevel.Debug, $"Set composite utility function parameters");
        }
    }
}
