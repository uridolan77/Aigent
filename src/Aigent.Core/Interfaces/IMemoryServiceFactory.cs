namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Interface for memory service factories
    /// </summary>
    public interface IMemoryServiceFactory
    {
        /// <summary>
        /// Creates a memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Memory service</returns>
        IMemoryService CreateMemoryService(string agentId);
    }
}
