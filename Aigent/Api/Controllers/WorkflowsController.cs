using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aigent.Core;
using Aigent.Orchestration;
using Aigent.Api.Models;

namespace Aigent.Api.Controllers
{
    /// <summary>
    /// Controller for workflow management
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class WorkflowsController : ControllerBase
    {
        private readonly IOrchestrator _orchestrator;

        /// <summary>
        /// Initializes a new instance of the WorkflowsController class
        /// </summary>
        /// <param name="orchestrator">Orchestrator</param>
        public WorkflowsController(IOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// Executes a workflow
        /// </summary>
        /// <param name="request">Workflow creation request</param>
        /// <returns>Result of the workflow execution</returns>
        [HttpPost("execute")]
        [Authorize(Policy = "ReadOnly")]
        public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> ExecuteWorkflow([FromBody] CreateWorkflowRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest(ApiResponse<WorkflowResultDto>.Error("Workflow name is required"));
            }

            if (request.Steps == null || request.Steps.Count == 0)
            {
                return BadRequest(ApiResponse<WorkflowResultDto>.Error("Workflow steps are required"));
            }

            try
            {
                // Convert request to workflow definition
                var workflowType = Enum.Parse<WorkflowType>(request.Type, true);
                var workflowSteps = new List<WorkflowStep>();

                foreach (var stepDto in request.Steps)
                {
                    workflowSteps.Add(new WorkflowStep
                    {
                        Name = stepDto.Name,
                        RequiredAgentType = stepDto.RequiredAgentType,
                        Parameters = stepDto.Parameters ?? new Dictionary<string, object>(),
                        Dependencies = stepDto.Dependencies ?? new List<string>()
                    });
                }

                var workflow = new WorkflowDefinition
                {
                    Name = request.Name,
                    Type = workflowType,
                    Steps = workflowSteps
                };

                // Execute the workflow
                var result = await _orchestrator.ExecuteWorkflow(workflow);

                // Convert result to DTO
                var resultDto = new WorkflowResultDto
                {
                    Success = result.Success,
                    Results = result.Results,
                    Errors = result.Errors
                };

                return Ok(ApiResponse<WorkflowResultDto>.Ok(resultDto));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<WorkflowResultDto>.Error($"Invalid workflow type: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<WorkflowResultDto>.Error($"Error executing workflow: {ex.Message}"));
            }
        }
    }
}
