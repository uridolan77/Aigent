namespace Aigent.Core
{
    /// <summary>
    /// Interface for memory service factories
    /// </summary>
    [System.Obsolete("Use Aigent.Core.Interfaces.IMemoryServiceFactory instead. This interface is maintained for backward compatibility.")]
    public interface IMemoryServiceFactory
    {
        /// <summary>
        /// Creates a memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Memory service for the agent</returns>
        IMemoryService CreateMemoryService(string agentId);
    }
}
