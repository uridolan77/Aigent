using System;
using System.Collections.Generic;
using System.Linq;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for machine learning models
    /// </summary>
    public interface IMLModel
    {
        /// <summary>
        /// Name of the model
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Initializes the model
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Makes a prediction using the model
        /// </summary>
        /// <param name="features">Features to make prediction from</param>
        /// <returns>Prediction result</returns>
        object Predict(object features);
        
        /// <summary>
        /// Trains the model on new data
        /// </summary>
        /// <param name="trainingData">Data to train on</param>
        void Train(object trainingData);
    }

    /// <summary>
    /// Interface for feature extractors
    /// </summary>
    public interface IFeatureExtractor
    {
        /// <summary>
        /// Extracts features from an environment state
        /// </summary>
        /// <param name="state">State to extract features from</param>
        /// <returns>Extracted features</returns>
        object ExtractFeatures(EnvironmentState state);
    }

    /// <summary>
    /// Simple feature extractor
    /// </summary>
    public class SimpleFeatureExtractor : IFeatureExtractor
    {
        /// <summary>
        /// Extracts features from an environment state
        /// </summary>
        /// <param name="state">State to extract features from</param>
        /// <returns>Extracted features</returns>
        public object ExtractFeatures(EnvironmentState state)
        {
            var features = new Dictionary<string, object>();
            
            // Extract text features
            if (state.Properties.TryGetValue("input", out var input) && input is string inputStr)
            {
                features["has_greeting"] = inputStr.Contains("hello", StringComparison.OrdinalIgnoreCase) || 
                                          inputStr.Contains("hi", StringComparison.OrdinalIgnoreCase);
                features["has_question"] = inputStr.Contains("?");
                features["has_help"] = inputStr.Contains("help", StringComparison.OrdinalIgnoreCase);
                features["text_length"] = inputStr.Length;
            }
            
            // Extract categorical features
            if (state.Properties.TryGetValue("category", out var category))
            {
                features["category"] = category;
            }
            
            // Extract numerical features
            if (state.Properties.TryGetValue("user_age", out var age) && age is int ageValue)
            {
                features["age"] = ageValue;
            }
            
            return features;
        }
    }

    /// <summary>
    /// Simple rule-based ML model
    /// </summary>
    public class SimpleRuleBasedModel : IMLModel
    {
        /// <summary>
        /// Name of the model
        /// </summary>
        public string Name => "SimpleRuleBasedModel";
        
        private readonly Dictionary<string, double> _actionScores = new();
        
        /// <summary>
        /// Initializes the model
        /// </summary>
        public void Initialize()
        {
            // Initialize with default action scores
            _actionScores["TextOutput"] = 0.5;
            _actionScores["Recommendation"] = 0.3;
            _actionScores["Classification"] = 0.2;
        }
        
        /// <summary>
        /// Makes a prediction using the model
        /// </summary>
        /// <param name="features">Features to make prediction from</param>
        /// <returns>Prediction result</returns>
        public object Predict(object features)
        {
            if (features is Dictionary<string, object> featureDict)
            {
                // Simple rule-based prediction
                if (featureDict.TryGetValue("has_greeting", out var hasGreeting) && hasGreeting is bool hasGreetingBool && hasGreetingBool)
                {
                    return "TextOutput";
                }
                
                if (featureDict.TryGetValue("category", out var category) && category != null)
                {
                    return "Recommendation";
                }
                
                // Return action scores as fallback
                return new Dictionary<string, double>(_actionScores);
            }
            
            // Default prediction
            return "TextOutput";
        }
        
        /// <summary>
        /// Trains the model on new data
        /// </summary>
        /// <param name="trainingData">Data to train on</param>
        public void Train(object trainingData)
        {
            if (trainingData is dynamic data)
            {
                string label = data.Label;
                double reward = data.Reward;
                
                // Simple reinforcement learning update
                if (_actionScores.TryGetValue(label, out var currentScore))
                {
                    double learningRate = 0.1;
                    _actionScores[label] = currentScore + learningRate * (reward - currentScore);
                }
                else
                {
                    _actionScores[label] = reward;
                }
            }
        }
    }

    /// <summary>
    /// Factory for creating neural network agents
    /// </summary>
    public static class NeuralNetworkAgentFactory
    {
        /// <summary>
        /// Creates a neural network agent with default components
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="memory">Memory service</param>
        /// <param name="safetyValidator">Safety validator</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="metrics">Metrics collector</param>
        /// <returns>Neural network agent</returns>
        public static NeuralNetworkAgent CreateDefaultNeuralNetworkAgent(
            string name,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            var model = new SimpleRuleBasedModel();
            var featureExtractor = new SimpleFeatureExtractor();
            
            return new NeuralNetworkAgent(
                name,
                model,
                featureExtractor,
                memory,
                safetyValidator,
                logger,
                messageBus,
                metrics);
        }
    }
}
