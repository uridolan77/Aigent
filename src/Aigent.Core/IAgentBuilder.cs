namespace Aigent.Core
{
    /// <summary>
    /// Interface for agent builders
    /// </summary>
    public interface IAgentBuilder
    {
        /// <summary>
        /// Sets the configuration for the agent
        /// </summary>
        /// <param name="configuration">Agent configuration</param>
        /// <returns>The builder</returns>
        IAgentBuilder WithConfiguration(AgentConfiguration configuration);
        
        /// <summary>
        /// Sets the memory service for the agent
        /// </summary>
        /// <typeparam name="T">Type of the memory service</typeparam>
        /// <returns>The builder</returns>
        IAgentBuilder WithMemory<T>() where T : IMemoryService;
        
        /// <summary>
        /// Builds the agent
        /// </summary>
        /// <returns>The built agent</returns>
        IAgent Build();
    }
}
