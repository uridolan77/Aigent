using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Memory.Interfaces
{
    /// <summary>
    /// Interface for memory service factories that create memory services for agents
    /// </summary>
    public interface IMemoryServiceFactory : Core.Interfaces.IMemoryServiceFactory
    {
        /// <summary>
        /// Creates a memory service for an agent with specific options
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="options">Memory service options</param>
        /// <returns>Memory service for the agent</returns>
        IMemoryService CreateMemoryService(string agentId, MemoryServiceOptions options);
        
        /// <summary>
        /// Creates a short-term memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Short-term memory service for the agent</returns>
        IShortTermMemory CreateShortTermMemory(string agentId);
        
        /// <summary>
        /// Creates a long-term memory service for an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Long-term memory service for the agent</returns>
        ILongTermMemory CreateLongTermMemory(string agentId);
    }
}
