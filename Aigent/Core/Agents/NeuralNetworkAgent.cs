using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;
using Aigent.Configuration;

namespace Aigent.Core
{
    /// <summary>
    /// Neural network-based agent that uses machine learning for decision making
    /// </summary>
    public class NeuralNetworkAgent : BaseAgent
    {
        /// <summary>
        /// Type of the agent
        /// </summary>
        public override AgentType Type => AgentType.Deliberative;
        
        private readonly IMLModel _model;
        private readonly IFeatureExtractor _featureExtractor;

        /// <summary>
        /// Initializes a new instance of the NeuralNetworkAgent class
        /// </summary>
        /// <param name="name">Name of the agent</param>
        /// <param name="model">ML model for the agent</param>
        /// <param name="featureExtractor">Feature extractor for the agent</param>
        /// <param name="memory">Memory service for the agent</param>
        /// <param name="safetyValidator">Safety validator for the agent</param>
        /// <param name="logger">Logger for the agent</param>
        /// <param name="messageBus">Message bus for the agent</param>
        /// <param name="metrics">Metrics collector for the agent</param>
        public NeuralNetworkAgent(
            string name,
            IMLModel model,
            IFeatureExtractor featureExtractor,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
            : base(memory, safetyValidator, logger, messageBus, metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
            
            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string> { "Prediction", "Classification", "Recommendation" },
                SkillLevels = new Dictionary<string, double>
                {
                    ["prediction"] = 0.9,
                    ["pattern_recognition"] = 0.95,
                    ["learning"] = 0.9
                },
                LoadFactor = 0.4,
                HistoricalPerformance = 0.85
            };
        }

        /// <summary>
        /// Initializes the agent and its resources
        /// </summary>
        public override async Task Initialize()
        {
            await base.Initialize();
            
            // Initialize the ML model
            _model.Initialize();
            
            _logger.Log(LogLevel.Information, $"Neural network agent {Name} initialized with model {_model.Name}");
        }

        /// <summary>
        /// Decides on an action based on the current environment state
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <returns>The action to be performed</returns>
        public override async Task<IAction> DecideAction(EnvironmentState state)
        {
            _metrics?.StartOperation($"agent_{Id}_decide_action");
            
            try
            {
                // Extract features from the state
                var features = _featureExtractor.ExtractFeatures(state);
                
                // Get prediction from the model
                var prediction = _model.Predict(features);
                
                // Convert prediction to action
                var action = ConvertPredictionToAction(prediction, state);
                
                // Validate the action
                var validationResult = await _safetyValidator.ValidateAction(action);
                if (validationResult.IsValid)
                {
                    _logger.Log(LogLevel.Debug, $"Agent {Name} selected action: {action.ActionType}");
                    _metrics?.RecordMetric($"agent.{Id}.action_selected", 1.0);
                    return action;
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Agent {Name} action {action.ActionType} failed validation: {validationResult.Message}");
                    _metrics?.RecordMetric($"agent.{Id}.action_validation_failure", 1.0);
                    
                    // Fall back to a safe action
                    return new TextOutputAction("I'm not sure what to do in this situation.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in neural network agent decision making", ex);
                _metrics?.RecordMetric($"agent.{Id}.decision_error", 1.0);
                
                return new TextOutputAction("I encountered an error while processing your request.");
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_decide_action");
            }
        }

        /// <summary>
        /// Learns from the result of an action
        /// </summary>
        /// <param name="state">State when the action was performed</param>
        /// <param name="action">The action that was performed</param>
        /// <param name="result">The result of the action</param>
        public override async Task Learn(EnvironmentState state, IAction action, ActionResult result)
        {
            _metrics?.StartOperation($"agent_{Id}_learn");
            
            try
            {
                // Extract features from the state
                var features = _featureExtractor.ExtractFeatures(state);
                
                // Create training data
                var trainingData = new
                {
                    Features = features,
                    Label = action.ActionType,
                    Reward = result.Success ? 1.0 : -0.5
                };
                
                // Train the model
                _model.Train(trainingData);
                
                // Store the experience in memory
                await _memory.StoreContext($"nn_experience_{Guid.NewGuid()}", new
                {
                    State = state,
                    Action = action.ActionType,
                    Result = result.Success,
                    Timestamp = DateTime.UtcNow
                }, TimeSpan.FromDays(30));
                
                _logger.Log(LogLevel.Debug, $"Neural network agent {Name} learned from action {action.ActionType}");
                _metrics?.RecordMetric($"agent.{Id}.learning_events", 1.0);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in neural network agent learning", ex);
                _metrics?.RecordMetric($"agent.{Id}.learning_error", 1.0);
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_learn");
            }
        }

        private IAction ConvertPredictionToAction(object prediction, EnvironmentState state)
        {
            // Convert model prediction to an action
            if (prediction is string actionType)
            {
                switch (actionType)
                {
                    case "TextOutput":
                        return new TextOutputAction(GenerateTextResponse(state));
                    case "Recommendation":
                        return new GenericAction("Recommendation", new Dictionary<string, object>
                        {
                            ["items"] = GenerateRecommendations(state)
                        });
                    default:
                        return new GenericAction(actionType, new Dictionary<string, object>());
                }
            }
            else if (prediction is Dictionary<string, double> actionScores)
            {
                // Select action with highest score
                var bestAction = actionScores.OrderByDescending(kv => kv.Value).First().Key;
                return new GenericAction(bestAction, new Dictionary<string, object>());
            }
            
            // Default action
            return new TextOutputAction("I'm not sure what to do in this situation.");
        }

        private string GenerateTextResponse(EnvironmentState state)
        {
            // Generate a text response based on the state
            if (state.Properties.TryGetValue("input", out var input) && input is string inputStr)
            {
                if (inputStr.Contains("hello", StringComparison.OrdinalIgnoreCase))
                {
                    return "Hello! How can I assist you today?";
                }
                else if (inputStr.Contains("help", StringComparison.OrdinalIgnoreCase))
                {
                    return "I'm here to help. What do you need assistance with?";
                }
            }
            
            return "I understand your request and I'm processing it.";
        }

        private List<string> GenerateRecommendations(EnvironmentState state)
        {
            // Generate recommendations based on the state
            var recommendations = new List<string>();
            
            if (state.Properties.TryGetValue("category", out var category) && category is string categoryStr)
            {
                switch (categoryStr.ToLower())
                {
                    case "books":
                        recommendations.AddRange(new[] { "The Great Gatsby", "To Kill a Mockingbird", "1984" });
                        break;
                    case "movies":
                        recommendations.AddRange(new[] { "The Shawshank Redemption", "The Godfather", "Pulp Fiction" });
                        break;
                    default:
                        recommendations.Add("No specific recommendations available for this category.");
                        break;
                }
            }
            
            return recommendations;
        }
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
}
