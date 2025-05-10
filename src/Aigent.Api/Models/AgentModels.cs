using System.Collections.Generic;
using Aigent.Core;

namespace Aigent.Api.Models
{
    /// <summary>
    /// Agent creation request model
    /// </summary>
    public class CreateAgentRequest
    {
        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the agent
        /// </summary>
        public AgentType Type { get; set; }
        
        /// <summary>
        /// Memory service type to use
        /// </summary>
        public string MemoryServiceType { get; set; }
        
        /// <summary>
        /// Additional settings for the agent
        /// </summary>
        public Dictionary<string, object> Settings { get; set; }
    }

    /// <summary>
    /// Agent data transfer object
    /// </summary>
    public class AgentDto
    {
        /// <summary>
        /// ID of the agent
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the agent
        /// </summary>
        public AgentType Type { get; set; }
        
        /// <summary>
        /// Capabilities of the agent
        /// </summary>
        public AgentCapabilitiesDto Capabilities { get; set; }
        
        /// <summary>
        /// Status of the agent
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Agent capabilities data transfer object
    /// </summary>
    public class AgentCapabilitiesDto
    {
        /// <summary>
        /// Types of actions this agent can perform
        /// </summary>
        public List<string> SupportedActionTypes { get; set; }
        
        /// <summary>
        /// Skill levels for different domains
        /// </summary>
        public Dictionary<string, double> SkillLevels { get; set; }
        
        /// <summary>
        /// Current load factor of the agent
        /// </summary>
        public double LoadFactor { get; set; }
        
        /// <summary>
        /// Historical performance metric
        /// </summary>
        public double HistoricalPerformance { get; set; }
    }

    /// <summary>
    /// Agent action request model
    /// </summary>
    public class AgentActionRequest
    {
        /// <summary>
        /// Input for the agent
        /// </summary>
        public string Input { get; set; }
        
        /// <summary>
        /// Additional parameters for the action
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
    }

    /// <summary>
    /// Agent action response model
    /// </summary>
    public class AgentActionResponse
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string ActionType { get; set; }
        
        /// <summary>
        /// Result of the action
        /// </summary>
        public ActionResultDto Result { get; set; }
    }

    /// <summary>
    /// Action result data transfer object
    /// </summary>
    public class ActionResultDto
    {
        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Data produced by the action
        /// </summary>
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// Workflow creation request model
    /// </summary>
    public class CreateWorkflowRequest
    {
        /// <summary>
        /// Name of the workflow
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the workflow
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Steps in the workflow
        /// </summary>
        public List<WorkflowStepDto> Steps { get; set; }
    }

    /// <summary>
    /// Workflow step data transfer object
    /// </summary>
    public class WorkflowStepDto
    {
        /// <summary>
        /// Name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of agent required for this step
        /// </summary>
        public AgentType RequiredAgentType { get; set; }
        
        /// <summary>
        /// Parameters for the step
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
        
        /// <summary>
        /// Dependencies on other steps
        /// </summary>
        public List<string> Dependencies { get; set; }
    }

    /// <summary>
    /// Workflow result data transfer object
    /// </summary>
    public class WorkflowResultDto
    {
        /// <summary>
        /// Whether the workflow was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Results of individual steps
        /// </summary>
        public Dictionary<string, object> Results { get; set; }
        
        /// <summary>
        /// Errors that occurred during execution
        /// </summary>
        public List<string> Errors { get; set; }
    }
}
