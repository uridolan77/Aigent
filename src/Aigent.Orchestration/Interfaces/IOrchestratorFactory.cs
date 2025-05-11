using Aigent.Orchestration.Models;

namespace Aigent.Orchestration.Interfaces
{
    /// <summary>
    /// Interface for factories that create orchestrators
    /// </summary>
    public interface IOrchestratorFactory
    {
        /// <summary>
        /// Creates a new orchestrator
        /// </summary>
        /// <returns>A new orchestrator</returns>
        IOrchestrator CreateOrchestrator();
        
        /// <summary>
        /// Creates a new orchestrator with the specified configuration
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        /// <returns>A new orchestrator</returns>
        IOrchestrator CreateOrchestrator(OrchestratorConfiguration configuration);
    }
}
