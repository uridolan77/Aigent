using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Utility-based agent that selects actions based on utility functions
    /// </summary>
    public class UtilityBasedAgent : BaseAgent
    {
        /// <summary>
        /// Type of the agent
        /// </summary>
        public override AgentType Type => AgentType.UtilityBased;
        
        private readonly IUtilityFunction _utilityFunction;
        private readonly IActionGenerator _actionGenerator;
        private readonly IStateEvaluator _stateEvaluator;
        private readonly IUtilityLearner _utilityLearner;

        /// <summary>
        /// Initializes a new instance of the UtilityBasedAgent class
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
        public UtilityBasedAgent(
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
            : base(memory, safetyValidator, logger, messageBus, metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _utilityFunction = utilityFunction ?? throw new ArgumentNullException(nameof(utilityFunction));
            _actionGenerator = actionGenerator ?? throw new ArgumentNullException(nameof(actionGenerator));
            _stateEvaluator = stateEvaluator ?? throw new ArgumentNullException(nameof(stateEvaluator));
            _utilityLearner = utilityLearner ?? throw new ArgumentNullException(nameof(utilityLearner));
            
            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string> { "Decision", "Optimization", "Learning", "Adaptation" },
                SkillLevels = new Dictionary<string, double>
                {
                    ["decision_making"] = 0.95,
                    ["optimization"] = 0.9,
                    ["adaptation"] = 0.85,
                    ["learning"] = 0.8
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
            
            // Load utility function parameters from memory
            var parameters = await _memory.RetrieveContext<Dictionary<string, double>>("utility_parameters");
            if (parameters != null)
            {
                _utilityFunction.SetParameters(parameters);
            }
            
            _logger.Log(LogLevel.Information, $"Utility-based agent {Name} initialized");
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
                // Evaluate the current state
                var stateFeatures = _stateEvaluator.EvaluateState(state);
                
                // Generate possible actions
                var possibleActions = _actionGenerator.GenerateActions(state);
                
                if (!possibleActions.Any())
                {
                    _logger.Log(LogLevel.Information, $"Agent {Name} could not generate any actions");
                    return new TextOutputAction("I'm not sure what actions to take in this situation.");
                }
                
                // Calculate utility for each action
                var actionUtilities = new Dictionary<IAction, double>();
                
                foreach (var action in possibleActions)
                {
                    // Predict the resulting state after taking this action
                    var predictedState = _stateEvaluator.PredictResultingState(state, action);
                    var predictedStateFeatures = _stateEvaluator.EvaluateState(predictedState);
                    
                    // Calculate utility of the predicted state
                    var utility = _utilityFunction.CalculateUtility(predictedStateFeatures);
                    actionUtilities[action] = utility;
                }
                
                // Select action with highest utility
                var bestAction = actionUtilities.OrderByDescending(kv => kv.Value).First().Key;
                
                // Validate the action
                var validationResult = await _safetyValidator.ValidateAction(bestAction);
                if (validationResult.IsValid)
                {
                    _logger.Log(LogLevel.Debug, $"Agent {Name} selected action: {bestAction.ActionType} with utility: {actionUtilities[bestAction]}");
                    _metrics?.RecordMetric($"agent.{Id}.action_selected", 1.0);
                    return bestAction;
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Agent {Name} action {bestAction.ActionType} failed validation: {validationResult.Message}");
                    _metrics?.RecordMetric($"agent.{Id}.action_validation_failure", 1.0);
                    
                    // Remove the invalid action and try again
                    possibleActions.Remove(bestAction);
                    actionUtilities.Remove(bestAction);
                    
                    if (actionUtilities.Any())
                    {
                        var alternativeAction = actionUtilities.OrderByDescending(kv => kv.Value).First().Key;
                        _logger.Log(LogLevel.Information, $"Selected alternative action: {alternativeAction.ActionType} with utility: {actionUtilities[alternativeAction]}");
                        return alternativeAction;
                    }
                    
                    return new TextOutputAction("I can't find a safe action to take in this situation.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in utility-based agent decision making: {ex.Message}", ex);
                _metrics?.RecordMetric($"agent.{Id}.decision_error", 1.0);
                
                return new TextOutputAction("I encountered an error in my decision-making process.");
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
                // Evaluate the state before the action
                var initialStateFeatures = _stateEvaluator.EvaluateState(state);
                
                // Calculate reward based on action result
                double reward = result.Success ? 1.0 : -0.5;
                
                // If the action produced data, use it to enhance the reward
                if (result.Data.TryGetValue("satisfaction", out var satisfaction) && satisfaction is double satisfactionValue)
                {
                    reward *= satisfactionValue;
                }
                
                // Update utility function based on the reward
                _utilityLearner.UpdateUtilityFunction(_utilityFunction, initialStateFeatures, action, reward);
                
                // Store updated utility function parameters in memory
                await _memory.StoreContext("utility_parameters", _utilityFunction.GetParameters());
                
                _logger.Log(LogLevel.Debug, $"Utility-based agent {Name} learned from action {action.ActionType} with reward {reward}");
                _metrics?.RecordMetric($"agent.{Id}.learning_events", 1.0);
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_learn");
            }
        }
    }

    /// <summary>
    /// Interface for utility functions
    /// </summary>
    public interface IUtilityFunction
    {
        /// <summary>
        /// Calculates utility of a state
        /// </summary>
        /// <param name="stateFeatures">Features of the state</param>
        /// <returns>Utility value</returns>
        double CalculateUtility(Dictionary<string, double> stateFeatures);
        
        /// <summary>
        /// Gets parameters of the utility function
        /// </summary>
        /// <returns>Parameters of the utility function</returns>
        Dictionary<string, double> GetParameters();
        
        /// <summary>
        /// Sets parameters of the utility function
        /// </summary>
        /// <param name="parameters">Parameters to set</param>
        void SetParameters(Dictionary<string, double> parameters);
    }

    /// <summary>
    /// Interface for action generators
    /// </summary>
    public interface IActionGenerator
    {
        /// <summary>
        /// Generates possible actions for a state
        /// </summary>
        /// <param name="state">Current state</param>
        /// <returns>Possible actions</returns>
        List<IAction> GenerateActions(EnvironmentState state);
    }

    /// <summary>
    /// Interface for state evaluators
    /// </summary>
    public interface IStateEvaluator
    {
        /// <summary>
        /// Evaluates features of a state
        /// </summary>
        /// <param name="state">State to evaluate</param>
        /// <returns>Features of the state</returns>
        Dictionary<string, double> EvaluateState(EnvironmentState state);
        
        /// <summary>
        /// Predicts the resulting state after taking an action
        /// </summary>
        /// <param name="state">Current state</param>
        /// <param name="action">Action to take</param>
        /// <returns>Predicted resulting state</returns>
        EnvironmentState PredictResultingState(EnvironmentState state, IAction action);
    }

    /// <summary>
    /// Interface for utility learners
    /// </summary>
    public interface IUtilityLearner
    {
        /// <summary>
        /// Updates a utility function based on experience
        /// </summary>
        /// <param name="utilityFunction">Utility function to update</param>
        /// <param name="stateFeatures">Features of the state</param>
        /// <param name="action">Action taken</param>
        /// <param name="reward">Reward received</param>
        void UpdateUtilityFunction(IUtilityFunction utilityFunction, Dictionary<string, double> stateFeatures, IAction action, double reward);
    }
}
