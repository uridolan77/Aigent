using System;
using System.Collections.Generic;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;

namespace Aigent.Configuration.Builders
{
    /// <summary>
    /// Interface for agent builder services that construct agent instances
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
        /// Sets a specific memory service instance for the agent
        /// </summary>
        /// <param name="memoryService">Memory service to use</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithMemory(IMemoryService memoryService);
        
        /// <summary>
        /// Adds a guardrail to the agent for safety constraints
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
        /// Sets a specific ML model instance for the agent
        /// </summary>
        /// <param name="mlModel">ML model to use</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithMLModel(IMLModel mlModel);
        
        /// <summary>
        /// Loads rules for the agent from a file
        /// </summary>
        /// <param name="filePath">Path to the rules file</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithRulesFromFile(string filePath);
        
        /// <summary>
        /// Adds a specific rule to the agent
        /// </summary>
        /// <param name="ruleName">Name of the rule</param>
        /// <param name="ruleAction">Action function that implements the rule</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithRule(string ruleName, Func<EnvironmentState, IAction> ruleAction);
        
        /// <summary>
        /// Sets the safety validator for the agent
        /// </summary>
        /// <param name="safetyValidator">Safety validator to use</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithSafetyValidator(ISafetyValidator safetyValidator);
        
        /// <summary>
        /// Sets additional configuration options for the agent
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        /// <returns>The builder for method chaining</returns>
        IAgentBuilder WithOption(string key, object value);
        
        /// <summary>
        /// Builds the agent with all configured options
        /// </summary>
        /// <returns>The built agent</returns>
        IAgent Build();
    }
}
