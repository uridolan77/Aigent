using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;

namespace Aigent.Configuration
{
    /// <summary>
    /// Interface for agent builder services
    /// </summary>
    public interface IAgentBuilder
    {
        /// <summary>
        /// Sets the configuration for the agent
        /// </summary>
        /// <param name="configuration">Configuration for the agent</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithConfiguration(AgentConfiguration configuration);
        
        /// <summary>
        /// Sets the memory service for the agent
        /// </summary>
        /// <typeparam name="T">Type of memory service</typeparam>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithMemory<T>() where T : IMemoryService;
        
        /// <summary>
        /// Adds a guardrail to the agent
        /// </summary>
        /// <param name="guardrail">Guardrail to add</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithGuardrail(IGuardrail guardrail);
        
        /// <summary>
        /// Sets the machine learning model for the agent
        /// </summary>
        /// <typeparam name="T">Type of ML model</typeparam>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithMLModel<T>() where T : IMLModel;
        
        /// <summary>
        /// Loads rules for the agent from a file
        /// </summary>
        /// <param name="filePath">Path to the rules file</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithRulesFromFile(string filePath);
        
        /// <summary>
        /// Builds the agent
        /// </summary>
        /// <returns>The built agent</returns>
        IAgent Build();
    }

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
        /// Predicts an output based on input features
        /// </summary>
        /// <param name="features">Input features</param>
        /// <returns>Prediction result</returns>
        object Predict(object features);
        
        /// <summary>
        /// Trains the model on a dataset
        /// </summary>
        /// <param name="trainingData">Training data</param>
        void Train(object trainingData);
    }
}
