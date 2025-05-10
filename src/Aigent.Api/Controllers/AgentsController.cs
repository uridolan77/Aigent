using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aigent.Api.Models;

namespace Aigent.Api.Controllers
{
    /// <summary>
    /// Controller for agent management
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AgentsController : ControllerBase
    {
        private readonly IAgentRegistry _agentRegistry;

        /// <summary>
        /// Initializes a new instance of the AgentsController class
        /// </summary>
        /// <param name="agentRegistry">Agent registry</param>
        public AgentsController(IAgentRegistry agentRegistry)
        {
            _agentRegistry = agentRegistry;
        }

        /// <summary>
        /// Gets all agents
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <returns>List of agents</returns>
        [HttpGet]
        [Authorize(Policy = "ReadOnly")]
        public async Task<ActionResult<ApiResponse<List<AgentDto>>>> GetAgents([FromQuery] AgentQueryParameters queryParameters)
        {
            var pagedAgents = await _agentRegistry.GetAgentsAsync(queryParameters);

            // Add pagination metadata to response headers
            HttpContext.Items["PaginationMetadata"] = pagedAgents.Metadata;

            return Ok(ApiResponse<List<AgentDto>>.Ok(pagedAgents.Items));
        }

        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="id">ID of the agent</param>
        /// <returns>The agent</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "ReadOnly")]
        public async Task<ActionResult<ApiResponse<AgentDto>>> GetAgent(string id)
        {
            var agent = await _agentRegistry.GetAgentAsync(id);

            if (agent == null)
            {
                return NotFound(ApiResponse<AgentDto>.Error($"Agent with ID {id} not found"));
            }

            return Ok(ApiResponse<AgentDto>.Ok(agent));
        }

        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="request">Agent creation request</param>
        /// <returns>The created agent</returns>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<AgentDto>>> CreateAgent([FromBody] CreateAgentRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest(ApiResponse<AgentDto>.Error("Agent name is required"));
            }

            try
            {
                var agent = await _agentRegistry.CreateAgentAsync(request);
                return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, ApiResponse<AgentDto>.Ok(agent));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AgentDto>.Error($"Error creating agent: {ex.Message}"));
            }
        }

        /// <summary>
        /// Deletes an agent
        /// </summary>
        /// <param name="id">ID of the agent</param>
        /// <returns>Whether the agent was deleted</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAgent(string id)
        {
            var result = await _agentRegistry.DeleteAgentAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<bool>.Error($"Agent with ID {id} not found"));
            }

            return Ok(ApiResponse<bool>.Ok(true, "Agent deleted successfully"));
        }

        /// <summary>
        /// Performs an action with an agent
        /// </summary>
        /// <param name="id">ID of the agent</param>
        /// <param name="request">Action request</param>
        /// <returns>Result of the action</returns>
        [HttpPost("{id}/actions")]
        [Authorize(Policy = "ReadOnly")]
        public async Task<ActionResult<ApiResponse<AgentActionResponse>>> PerformAction(string id, [FromBody] AgentActionRequest request)
        {
            if (string.IsNullOrEmpty(request.Input))
            {
                return BadRequest(ApiResponse<AgentActionResponse>.Error("Input is required"));
            }

            try
            {
                var result = await _agentRegistry.PerformActionAsync(id, request);
                return Ok(ApiResponse<AgentActionResponse>.Ok(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<AgentActionResponse>.Error($"Agent with ID {id} not found"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AgentActionResponse>.Error($"Error performing action: {ex.Message}"));
            }
        }
    }
}
