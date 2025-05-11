using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aigent.Core.Models;
using Aigent.Monitoring;
using Aigent.Communication.Interfaces;
using Aigent.Orchestration.Interfaces;
using Aigent.Orchestration.Models;

namespace Aigent.Orchestration.Engines
{
    /// <summary>
    /// Standard implementation of workflow execution engine
    /// </summary>
    public class StandardWorkflowEngine : IWorkflowEngine
    {
        private readonly ILogger _logger;
        private readonly IMessageBus _messageBus;
        private readonly IMetricsCollector _metrics;
        private readonly ConcurrentDictionary<string, WorkflowStatus> _workflowStatuses = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _workflowCancellationTokens = new();
        private WorkflowEngineConfiguration _configuration = WorkflowEngineConfiguration.Default();
        
        /// <summary>
        /// Initializes a new instance of the StandardWorkflowEngine class
        /// </summary>
        /// <param name="logger">Logger for engine activities</param>
        /// <param name="messageBus">Message bus for communication</param>
        /// <param name="metrics">Metrics collector for monitoring</param>
        public StandardWorkflowEngine(
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _metrics = metrics;
        }
        
        /// <summary>
        /// Executes a workflow
        /// </summary>
        /// <param name="workflow">Workflow to execute</param>
        /// <param name="context">Context for workflow execution</param>
        /// <returns>Result of the workflow execution</returns>
        public async Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, WorkflowContext context)
        {
            _metrics?.RecordMetric($"workflow_engine.execute.{workflow.Name}.start", 1.0);
            
            // Create cancellation token source with timeout
            var cts = new CancellationTokenSource();
            var timeoutMs = workflow.TimeoutSeconds * 1000;
            cts.CancelAfter(timeoutMs);
            
            // Store cancellation token for this workflow
            _workflowCancellationTokens[workflow.Id] = cts;
            
            // Create and initialize workflow status
            var status = new WorkflowStatus
            {
                WorkflowId = workflow.Id,
                InstanceId = context.InstanceId,
                WorkflowName = workflow.Name,
                State = WorkflowState.Running,
                StartTime = DateTime.UtcNow,
                TotalSteps = workflow.Steps.Count
            };
            
            _workflowStatuses[workflow.Id] = status;
            
            // Update initial status
            await UpdateWorkflowStatusAsync(status);
            
            try
            {
                _logger.Log(LogLevel.Information, $"Starting workflow execution: {workflow.Name} (ID: {workflow.Id})");
                
                // Execute the workflow based on its type
                WorkflowResult result;
                
                switch (workflow.Type)
                {
                    case WorkflowType.Sequential:
                        result = await ExecuteSequentialWorkflowAsync(workflow, context, status, cts.Token);
                        break;
                    case WorkflowType.Parallel:
                        result = await ExecuteParallelWorkflowAsync(workflow, context, status, cts.Token);
                        break;
                    case WorkflowType.Conditional:
                        result = await ExecuteConditionalWorkflowAsync(workflow, context, status, cts.Token);
                        break;
                    case WorkflowType.Hierarchical:
                        result = await ExecuteHierarchicalWorkflowAsync(workflow, context, status, cts.Token);
                        break;
                    default:
                        throw new NotSupportedException($"Workflow type not supported: {workflow.Type}");
                }
                
                // Update final status
                status.State = result.Success ? WorkflowState.Completed : WorkflowState.Failed;
                status.EndTime = DateTime.UtcNow;
                status.ErrorMessage = result.Success ? null : result.Message;
                
                await UpdateWorkflowStatusAsync(status);
                
                _logger.Log(LogLevel.Information, 
                    $"Workflow execution {(result.Success ? "succeeded" : "failed")}: {workflow.Name} (ID: {workflow.Id})");
                
                _metrics?.RecordMetric($"workflow_engine.execute.{workflow.Name}.success", result.Success ? 1.0 : 0.0);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, $"Workflow execution cancelled or timed out: {workflow.Name} (ID: {workflow.Id})");
                
                // Update status to cancelled or timed out
                status.State = cts.Token.IsCancellationRequested ? WorkflowState.Cancelled : WorkflowState.TimedOut;
                status.EndTime = DateTime.UtcNow;
                status.ErrorMessage = cts.Token.IsCancellationRequested
                    ? "Workflow was cancelled"
                    : $"Workflow timed out after {workflow.TimeoutSeconds} seconds";
                
                await UpdateWorkflowStatusAsync(status);
                
                _metrics?.RecordMetric($"workflow_engine.execute.{workflow.Name}.cancelled", 1.0);
                
                return WorkflowResult.Failed(
                    workflow.Id,
                    workflow.Name,
                    status.ErrorMessage,
                    new List<WorkflowError>
                    {
                        new WorkflowError
                        {
                            Code = cts.Token.IsCancellationRequested ? "WORKFLOW_CANCELLED" : "WORKFLOW_TIMEOUT",
                            Message = status.ErrorMessage,
                            Severity = ErrorSeverity.Critical,
                            Timestamp = DateTime.UtcNow
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing workflow '{workflow.Name}': {ex.Message}", ex);
                
                // Update status to failed
                status.State = WorkflowState.Failed;
                status.EndTime = DateTime.UtcNow;
                status.ErrorMessage = $"Error executing workflow: {ex.Message}";
                
                await UpdateWorkflowStatusAsync(status);
                
                _metrics?.RecordMetric($"workflow_engine.execute.{workflow.Name}.error", 1.0);
                
                return WorkflowResult.Failed(
                    workflow.Id,
                    workflow.Name,
                    $"Error executing workflow: {ex.Message}",
                    new List<WorkflowError>
                    {
                        new WorkflowError
                        {
                            Code = "EXECUTION_ERROR",
                            Message = ex.Message,
                            Severity = ErrorSeverity.Critical,
                            Timestamp = DateTime.UtcNow,
                            Details = new Dictionary<string, object>
                            {
                                ["ExceptionType"] = ex.GetType().Name,
                                ["StackTrace"] = ex.StackTrace
                            }
                        }
                    });
            }
            finally
            {
                // Clean up
                _workflowCancellationTokens.TryRemove(workflow.Id, out _);
                
                _metrics?.RecordMetric($"workflow_engine.execute.{workflow.Name}.end", 1.0);
            }
        }
        
        /// <summary>
        /// Gets the status of a workflow
        /// </summary>
        /// <param name="workflowId">ID of the workflow</param>
        /// <returns>Status of the workflow</returns>
        public Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId)
        {
            if (string.IsNullOrEmpty(workflowId))
            {
                throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));
            }
            
            _workflowStatuses.TryGetValue(workflowId, out var status);
            return Task.FromResult(status);
        }
        
        /// <summary>
        /// Cancels a running workflow
        /// </summary>
        /// <param name="workflowId">ID of the workflow to cancel</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task CancelWorkflowAsync(string workflowId)
        {
            if (string.IsNullOrEmpty(workflowId))
            {
                throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));
            }
            
            if (_workflowCancellationTokens.TryGetValue(workflowId, out var cts))
            {
                _logger.Log(LogLevel.Information, $"Cancelling workflow: {workflowId}");
                cts.Cancel();
                
                // Update status if available
                if (_workflowStatuses.TryGetValue(workflowId, out var status))
                {
                    status.State = WorkflowState.Cancelled;
                    status.EndTime = DateTime.UtcNow;
                    status.ErrorMessage = "Workflow was cancelled";
                    
                    UpdateWorkflowStatusAsync(status);
                }
            }
            else
            {
                _logger.Log(LogLevel.Warning, $"Cannot cancel workflow {workflowId}: not found or already completed");
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets all running workflows
        /// </summary>
        /// <returns>Collection of running workflow statuses</returns>
        public IReadOnlyCollection<WorkflowStatus> GetRunningWorkflows()
        {
            return _workflowStatuses.Values
                .Where(s => s.State == WorkflowState.Running)
                .ToList()
                .AsReadOnly();
        }
        
        /// <summary>
        /// Configures the workflow engine
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        public void Configure(WorkflowEngineConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger.Log(LogLevel.Information, "Workflow engine configuration updated");
        }
        
        private async Task<WorkflowResult> ExecuteSequentialWorkflowAsync(
            WorkflowDefinition workflow, 
            WorkflowContext context,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var result = new WorkflowResult
            {
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Name,
                Success = true,
                StartTime = DateTime.UtcNow
            };
            
            var stepResults = new Dictionary<string, ActionResult>();
            var errors = new List<WorkflowError>();
            int completedSteps = 0;
            
            foreach (var step in workflow.Steps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Workflow execution was cancelled", cancellationToken);
                }
                
                // Update status with current step
                status.CurrentStepId = step.Id;
                status.CurrentStepName = step.Name;
                
                // Create step status
                var stepStatus = new StepStatus
                {
                    StepId = step.Id,
                    StepName = step.Name,
                    State = StepState.Running,
                    StartTime = DateTime.UtcNow
                };
                
                status.StepStatuses[step.Id] = stepStatus;
                await UpdateWorkflowStatusAsync(status);
                
                _logger.Log(LogLevel.Debug, $"Executing step: {step.Name} (ID: {step.Id})");
                
                try
                {
                    // Execute step
                    var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
                    
                    // Record result
                    stepResults[step.Id] = stepResult;
                    
                    // Update step status
                    stepStatus.State = stepResult.Success ? StepState.Completed : StepState.Failed;
                    stepStatus.EndTime = DateTime.UtcNow;
                    stepStatus.DurationMs = (long)((stepStatus.EndTime.Value - stepStatus.StartTime.Value).TotalMilliseconds);
                    stepStatus.ErrorMessage = stepResult.Success ? null : stepResult.Message;
                    
                    // Update workflow status
                    completedSteps++;
                    status.CompletedSteps = completedSteps;
                    status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                    
                    if (!stepResult.Success)
                    {
                        status.FailedSteps++;
                        
                        // Add error
                        var error = new WorkflowError
                        {
                            Code = "STEP_FAILED",
                            Message = stepResult.Message,
                            StepId = step.Id,
                            StepName = step.Name,
                            Timestamp = DateTime.UtcNow,
                            Severity = step.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error
                        };
                        
                        errors.Add(error);
                        
                        // Handle step failure according to workflow error handling mode
                        if (step.IsCritical && workflow.ErrorHandlingMode == ErrorHandlingMode.StopWorkflow)
                        {
                            result.Success = false;
                            result.Message = $"Critical step '{step.Name}' failed: {stepResult.Message}";
                            break;
                        }
                        
                        // Check if we should continue on failure
                        if (!step.ContinueOnFailure && workflow.ErrorHandlingMode != ErrorHandlingMode.IgnoreErrors)
                        {
                            result.Success = false;
                            result.Message = $"Step '{step.Name}' failed: {stepResult.Message}";
                            break;
                        }
                    }
                    
                    await UpdateWorkflowStatusAsync(status);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing step '{step.Name}': {ex.Message}", ex);
                    
                    // Update step status
                    stepStatus.State = StepState.Failed;
                    stepStatus.EndTime = DateTime.UtcNow;
                    stepStatus.DurationMs = (long)((stepStatus.EndTime.Value - stepStatus.StartTime.Value).TotalMilliseconds);
                    stepStatus.ErrorMessage = ex.Message;
                    
                    // Update workflow status
                    completedSteps++;
                    status.CompletedSteps = completedSteps;
                    status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                    status.FailedSteps++;
                    
                    // Add error
                    var error = new WorkflowError
                    {
                        Code = "STEP_ERROR",
                        Message = ex.Message,
                        StepId = step.Id,
                        StepName = step.Name,
                        Timestamp = DateTime.UtcNow,
                        Severity = step.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error,
                        Details = new Dictionary<string, object>
                        {
                            ["ExceptionType"] = ex.GetType().Name,
                            ["StackTrace"] = ex.StackTrace
                        }
                    };
                    
                    errors.Add(error);
                    
                    // Handle error according to workflow error handling mode
                    if (step.IsCritical && workflow.ErrorHandlingMode == ErrorHandlingMode.StopWorkflow)
                    {
                        result.Success = false;
                        result.Message = $"Critical step '{step.Name}' failed with error: {ex.Message}";
                        break;
                    }
                    
                    // Check if we should continue on failure
                    if (!step.ContinueOnFailure && workflow.ErrorHandlingMode != ErrorHandlingMode.IgnoreErrors)
                    {
                        result.Success = false;
                        result.Message = $"Step '{step.Name}' failed with error: {ex.Message}";
                        break;
                    }
                    
                    await UpdateWorkflowStatusAsync(status);
                }
            }
            
            // Finalize result
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)((result.EndTime - result.StartTime).TotalMilliseconds);
            result.StepResults = stepResults;
            result.Errors = errors;
            
            if (errors.Count > 0 && string.IsNullOrEmpty(result.Message))
            {
                result.Success = false;
                result.Message = $"Workflow completed with {errors.Count} errors";
            }
            
            if (result.Success && string.IsNullOrEmpty(result.Message))
            {
                result.Message = "Workflow executed successfully";
            }
            
            return result;
        }
        
        private async Task<WorkflowResult> ExecuteParallelWorkflowAsync(
            WorkflowDefinition workflow, 
            WorkflowContext context,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var result = new WorkflowResult
            {
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Name,
                Success = true,
                StartTime = DateTime.UtcNow
            };
            
            var tasks = new List<Task<(string StepId, ActionResult Result, WorkflowError Error)>>();
            var stepResults = new ConcurrentDictionary<string, ActionResult>();
            var errors = new ConcurrentBag<WorkflowError>();
            var maxConcurrentSteps = _configuration.MaxConcurrentStepsPerWorkflow;
            var completedSteps = 0;
            
            // Initialize all step statuses
            foreach (var step in workflow.Steps)
            {
                status.StepStatuses[step.Id] = new StepStatus
                {
                    StepId = step.Id,
                    StepName = step.Name,
                    State = StepState.Waiting
                };
            }
            
            await UpdateWorkflowStatusAsync(status);
            
            // Group steps by dependencies
            var readySteps = workflow.Steps
                .Where(s => !s.Dependencies.Any() || s.Dependencies.All(d => stepResults.ContainsKey(d)))
                .ToList();
            
            while (readySteps.Any() && !cancellationToken.IsCancellationRequested)
            {
                // Limit concurrency
                var stepsToRun = readySteps.Take(maxConcurrentSteps - tasks.Count).ToList();
                readySteps.RemoveRange(0, stepsToRun.Count);
                
                foreach (var step in stepsToRun)
                {
                    // Create a local copy of the step for the closure
                    var localStep = step;
                    
                    // Update status with current step
                    var stepStatus = status.StepStatuses[localStep.Id];
                    stepStatus.State = StepState.Running;
                    stepStatus.StartTime = DateTime.UtcNow;
                    
                    await UpdateWorkflowStatusAsync(status);
                    
                    _logger.Log(LogLevel.Debug, $"Executing step in parallel: {localStep.Name} (ID: {localStep.Id})");
                    
                    // Create task for this step
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            // Execute step
                            var stepResult = await ExecuteStepAsync(localStep, context, cancellationToken);
                            
                            // Update step status
                            stepStatus.State = stepResult.Success ? StepState.Completed : StepState.Failed;
                            stepStatus.EndTime = DateTime.UtcNow;
                            stepStatus.DurationMs = (long)((stepStatus.EndTime.Value - stepStatus.StartTime.Value).TotalMilliseconds);
                            stepStatus.ErrorMessage = stepResult.Success ? null : stepResult.Message;
                            
                            WorkflowError error = null;
                            
                            if (!stepResult.Success)
                            {
                                // Add error
                                error = new WorkflowError
                                {
                                    Code = "STEP_FAILED",
                                    Message = stepResult.Message,
                                    StepId = localStep.Id,
                                    StepName = localStep.Name,
                                    Timestamp = DateTime.UtcNow,
                                    Severity = localStep.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error
                                };
                            }
                            
                            return (localStep.Id, stepResult, error);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error executing parallel step '{localStep.Name}': {ex.Message}", ex);
                            
                            // Update step status
                            stepStatus.State = StepState.Failed;
                            stepStatus.EndTime = DateTime.UtcNow;
                            stepStatus.DurationMs = (long)((stepStatus.EndTime.Value - stepStatus.StartTime.Value).TotalMilliseconds);
                            stepStatus.ErrorMessage = ex.Message;
                            
                            // Create error
                            var error = new WorkflowError
                            {
                                Code = "STEP_ERROR",
                                Message = ex.Message,
                                StepId = localStep.Id,
                                StepName = localStep.Name,
                                Timestamp = DateTime.UtcNow,
                                Severity = localStep.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error,
                                Details = new Dictionary<string, object>
                                {
                                    ["ExceptionType"] = ex.GetType().Name,
                                    ["StackTrace"] = ex.StackTrace
                                }
                            };
                            
                            return (localStep.Id, new ActionResult { Success = false, Message = ex.Message }, error);
                        }
                    }, cancellationToken);
                    
                    tasks.Add(task);
                }
                
                // Wait for any task to complete
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);
                
                // Process completed task
                var (stepId, stepResult, error) = await completedTask;
                
                // Store the result
                stepResults[stepId] = stepResult;
                
                // Handle errors
                if (error != null)
                {
                    errors.Add(error);
                    
                    // Update workflow status
                    status.FailedSteps++;
                    
                    // Check if we should cancel the workflow
                    var step = workflow.Steps.First(s => s.Id == stepId);
                    if (step.IsCritical && workflow.ErrorHandlingMode == ErrorHandlingMode.StopWorkflow)
                    {
                        // Cancel all remaining tasks
                        cancellationToken.ThrowIfCancellationRequested();
                        result.Success = false;
                        result.Message = $"Critical step '{step.Name}' failed: {error.Message}";
                        break;
                    }
                }
                
                // Update status
                completedSteps++;
                status.CompletedSteps = completedSteps;
                status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                await UpdateWorkflowStatusAsync(status);
                
                // Find new ready steps
                readySteps.AddRange(workflow.Steps
                    .Where(s => !status.StepStatuses[s.Id].StartTime.HasValue) // Not started yet
                    .Where(s => s.Dependencies.All(d => stepResults.ContainsKey(d))) // All dependencies satisfied
                    .ToList());
            }
            
            // Wait for any remaining tasks to complete
            if (tasks.Count > 0)
            {
                var remainingResults = await Task.WhenAll(tasks);
                
                foreach (var (stepId, stepResult, error) in remainingResults)
                {
                    stepResults[stepId] = stepResult;
                    
                    if (error != null)
                    {
                        errors.Add(error);
                        status.FailedSteps++;
                    }
                    
                    completedSteps++;
                }
                
                status.CompletedSteps = completedSteps;
                status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                await UpdateWorkflowStatusAsync(status);
            }
            
            // Finalize result
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)((result.EndTime - result.StartTime).TotalMilliseconds);
            result.StepResults = new Dictionary<string, ActionResult>(stepResults);
            result.Errors = errors.ToList();
            
            if (errors.Count > 0 && string.IsNullOrEmpty(result.Message))
            {
                result.Success = false;
                result.Message = $"Workflow completed with {errors.Count} errors";
            }
            
            if (result.Success && string.IsNullOrEmpty(result.Message))
            {
                result.Message = "Workflow executed successfully";
            }
            
            return result;
        }
        
        private async Task<WorkflowResult> ExecuteConditionalWorkflowAsync(
            WorkflowDefinition workflow, 
            WorkflowContext context,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var result = new WorkflowResult
            {
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Name,
                Success = true,
                StartTime = DateTime.UtcNow
            };
            
            var stepResults = new Dictionary<string, ActionResult>();
            var errors = new List<WorkflowError>();
            int completedSteps = 0;
            
            foreach (var step in workflow.Steps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Workflow execution was cancelled", cancellationToken);
                }
                
                // Check if step should be executed
                bool shouldExecute = await ShouldExecuteStepAsync(step, stepResults, context);
                
                // Create step status
                var stepStatus = new StepStatus
                {
                    StepId = step.Id,
                    StepName = step.Name,
                    State = shouldExecute ? StepState.Running : StepState.Skipped,
                    StartTime = shouldExecute ? DateTime.UtcNow : (DateTime?)null
                };
                
                status.StepStatuses[step.Id] = stepStatus;
                
                if (!shouldExecute)
                {
                    _logger.Log(LogLevel.Debug, $"Skipping step: {step.Name} (ID: {step.Id}) - condition not met");
                    
                    // Update status
                    completedSteps++;
                    status.CompletedSteps = completedSteps;
                    status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                    
                    await UpdateWorkflowStatusAsync(status);
                    continue;
                }
                
                // Update status with current step
                status.CurrentStepId = step.Id;
                status.CurrentStepName = step.Name;
                await UpdateWorkflowStatusAsync(status);
                
                _logger.Log(LogLevel.Debug, $"Executing conditional step: {step.Name} (ID: {step.Id})");
                
                try
                {
                    // Execute step
                    var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
                    
                    // Record result
                    stepResults[step.Id] = stepResult;
                    
                    // Update step status
                    stepStatus.State = stepResult.Success ? StepState.Completed : StepState.Failed;
                    stepStatus.EndTime = DateTime.UtcNow;
                    stepStatus.DurationMs = (long)((stepStatus.EndTime.Value - stepStatus.StartTime.Value).TotalMilliseconds);
                    stepStatus.ErrorMessage = stepResult.Success ? null : stepResult.Message;
                    
                    // Update workflow status
                    completedSteps++;
                    status.CompletedSteps = completedSteps;
                    status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                    
                    if (!stepResult.Success)
                    {
                        status.FailedSteps++;
                        
                        // Add error
                        var error = new WorkflowError
                        {
                            Code = "STEP_FAILED",
                            Message = stepResult.Message,
                            StepId = step.Id,
                            StepName = step.Name,
                            Timestamp = DateTime.UtcNow,
                            Severity = step.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error
                        };
                        
                        errors.Add(error);
                        
                        // Handle step failure according to workflow error handling mode
                        if (step.IsCritical && workflow.ErrorHandlingMode == ErrorHandlingMode.StopWorkflow)
                        {
                            result.Success = false;
                            result.Message = $"Critical step '{step.Name}' failed: {stepResult.Message}";
                            break;
                        }
                        
                        // Check if we should continue on failure
                        if (!step.ContinueOnFailure && workflow.ErrorHandlingMode != ErrorHandlingMode.IgnoreErrors)
                        {
                            result.Success = false;
                            result.Message = $"Step '{step.Name}' failed: {stepResult.Message}";
                            break;
                        }
                    }
                    
                    await UpdateWorkflowStatusAsync(status);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing step '{step.Name}': {ex.Message}", ex);
                    
                    // Update step status
                    stepStatus.State = StepState.Failed;
                    stepStatus.EndTime = DateTime.UtcNow;
                    stepStatus.DurationMs = (long)((stepStatus.EndTime.Value - stepStatus.StartTime.Value).TotalMilliseconds);
                    stepStatus.ErrorMessage = ex.Message;
                    
                    // Update workflow status
                    completedSteps++;
                    status.CompletedSteps = completedSteps;
                    status.ProgressPercentage = (int)((double)completedSteps / workflow.Steps.Count * 100);
                    status.FailedSteps++;
                    
                    // Add error
                    var error = new WorkflowError
                    {
                        Code = "STEP_ERROR",
                        Message = ex.Message,
                        StepId = step.Id,
                        StepName = step.Name,
                        Timestamp = DateTime.UtcNow,
                        Severity = step.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error,
                        Details = new Dictionary<string, object>
                        {
                            ["ExceptionType"] = ex.GetType().Name,
                            ["StackTrace"] = ex.StackTrace
                        }
                    };
                    
                    errors.Add(error);
                    
                    // Handle error according to workflow error handling mode
                    if (step.IsCritical && workflow.ErrorHandlingMode == ErrorHandlingMode.StopWorkflow)
                    {
                        result.Success = false;
                        result.Message = $"Critical step '{step.Name}' failed with error: {ex.Message}";
                        break;
                    }
                    
                    // Check if we should continue on failure
                    if (!step.ContinueOnFailure && workflow.ErrorHandlingMode != ErrorHandlingMode.IgnoreErrors)
                    {
                        result.Success = false;
                        result.Message = $"Step '{step.Name}' failed with error: {ex.Message}";
                        break;
                    }
                    
                    await UpdateWorkflowStatusAsync(status);
                }
            }
            
            // Finalize result
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)((result.EndTime - result.StartTime).TotalMilliseconds);
            result.StepResults = stepResults;
            result.Errors = errors;
            
            if (errors.Count > 0 && string.IsNullOrEmpty(result.Message))
            {
                result.Success = false;
                result.Message = $"Workflow completed with {errors.Count} errors";
            }
            
            if (result.Success && string.IsNullOrEmpty(result.Message))
            {
                result.Message = "Workflow executed successfully";
            }
            
            return result;
        }
        
        private async Task<WorkflowResult> ExecuteHierarchicalWorkflowAsync(
            WorkflowDefinition workflow, 
            WorkflowContext context,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var result = new WorkflowResult
            {
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Name,
                Success = true,
                StartTime = DateTime.UtcNow
            };
            
            var stepResults = new Dictionary<string, ActionResult>();
            var errors = new List<WorkflowError>();
            
            // Find root steps (no dependencies)
            var rootSteps = workflow.Steps.Where(s => !s.Dependencies.Any()).ToList();
            
            // Initialize step counters
            int totalExecutedSteps = 0;
            int failedSteps = 0;
            
            // Initialize all step statuses
            foreach (var step in workflow.Steps)
            {
                status.StepStatuses[step.Id] = new StepStatus
                {
                    StepId = step.Id,
                    StepName = step.Name,
                    State = StepState.NotStarted
                };
            }
            
            await UpdateWorkflowStatusAsync(status);
            
            // Execute root steps
            foreach (var rootStep in rootSteps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Workflow execution was cancelled", cancellationToken);
                }
                
                var (success, failed) = await ExecuteHierarchicalStepAsync(
                    rootStep, 
                    workflow.Steps, 
                    stepResults, 
                    errors,
                    context, 
                    status,
                    workflow.ErrorHandlingMode, 
                    cancellationToken);
                
                totalExecutedSteps += success + failed;
                failedSteps += failed;
                
                // Check if we should continue
                if (failed > 0 && workflow.ErrorHandlingMode == ErrorHandlingMode.StopWorkflow)
                {
                    result.Success = false;
                    result.Message = "Workflow stopped due to failed steps";
                    break;
                }
            }
            
            // Update status
            status.CompletedSteps = totalExecutedSteps;
            status.FailedSteps = failedSteps;
            status.ProgressPercentage = (int)((double)totalExecutedSteps / workflow.Steps.Count * 100);
            await UpdateWorkflowStatusAsync(status);
            
            // Finalize result
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)((result.EndTime - result.StartTime).TotalMilliseconds);
            result.StepResults = stepResults;
            result.Errors = errors;
            
            if (failedSteps > 0)
            {
                result.Success = false;
                result.Message = $"Workflow completed with {failedSteps} failed steps";
            }
            else
            {
                result.Success = true;
                result.Message = "Workflow executed successfully";
            }
            
            return result;
        }
        
        private async Task<(int SuccessfulSteps, int FailedSteps)> ExecuteHierarchicalStepAsync(
            WorkflowStep step,
            List<WorkflowStep> allSteps,
            Dictionary<string, ActionResult> stepResults,
            List<WorkflowError> errors,
            WorkflowContext context,
            WorkflowStatus status,
            ErrorHandlingMode errorHandlingMode,
            CancellationToken cancellationToken)
        {
            int successfulSteps = 0;
            int failedSteps = 0;
            
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Workflow execution was cancelled", cancellationToken);
            }
            
            // Check dependencies
            foreach (var dependencyId in step.Dependencies)
            {
                if (!stepResults.TryGetValue(dependencyId, out var dependencyResult) || !dependencyResult.Success)
                {
                    // Dependency not satisfied, skip this step
                    _logger.Log(LogLevel.Debug, $"Skipping step {step.Name} due to unsatisfied dependency {dependencyId}");
                    
                    // Update step status
                    var stepStatus = status.StepStatuses[step.Id];
                    stepStatus.State = StepState.Skipped;
                    await UpdateWorkflowStatusAsync(status);
                    
                    return (0, 0);
                }
            }
            
            // Update status with current step
            status.CurrentStepId = step.Id;
            status.CurrentStepName = step.Name;
            
            // Create step status
            var currentStepStatus = status.StepStatuses[step.Id];
            currentStepStatus.State = StepState.Running;
            currentStepStatus.StartTime = DateTime.UtcNow;
            
            await UpdateWorkflowStatusAsync(status);
            
            _logger.Log(LogLevel.Debug, $"Executing hierarchical step: {step.Name} (ID: {step.Id})");
            
            try
            {
                // Execute step
                var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
                
                // Record result
                stepResults[step.Id] = stepResult;
                
                // Update step status
                currentStepStatus.State = stepResult.Success ? StepState.Completed : StepState.Failed;
                currentStepStatus.EndTime = DateTime.UtcNow;
                currentStepStatus.DurationMs = (long)((currentStepStatus.EndTime.Value - currentStepStatus.StartTime.Value).TotalMilliseconds);
                currentStepStatus.ErrorMessage = stepResult.Success ? null : stepResult.Message;
                
                await UpdateWorkflowStatusAsync(status);
                
                if (stepResult.Success)
                {
                    successfulSteps++;
                    
                    // Find child steps (steps that depend on this one)
                    var childSteps = allSteps.Where(s => s.Dependencies.Contains(step.Id)).ToList();
                    
                    // Execute child steps
                    foreach (var childStep in childSteps)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("Workflow execution was cancelled", cancellationToken);
                        }
                        
                        var (childSuccess, childFailed) = await ExecuteHierarchicalStepAsync(
                            childStep, 
                            allSteps, 
                            stepResults, 
                            errors,
                            context, 
                            status,
                            errorHandlingMode, 
                            cancellationToken);
                        
                        successfulSteps += childSuccess;
                        failedSteps += childFailed;
                    }
                }
                else
                {
                    failedSteps++;
                    
                    // Add error
                    var error = new WorkflowError
                    {
                        Code = "STEP_FAILED",
                        Message = stepResult.Message,
                        StepId = step.Id,
                        StepName = step.Name,
                        Timestamp = DateTime.UtcNow,
                        Severity = step.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error
                    };
                    
                    errors.Add(error);
                    
                    // Handle step failure according to error handling mode
                    if (!step.ContinueOnFailure && errorHandlingMode != ErrorHandlingMode.IgnoreErrors)
                    {
                        // Don't execute child steps if this step failed
                        return (successfulSteps, failedSteps);
                    }
                    else
                    {
                        // Execute child steps even if this step failed
                        var childSteps = allSteps.Where(s => s.Dependencies.Contains(step.Id)).ToList();
                        
                        foreach (var childStep in childSteps)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException("Workflow execution was cancelled", cancellationToken);
                            }
                            
                            var (childSuccess, childFailed) = await ExecuteHierarchicalStepAsync(
                                childStep, 
                                allSteps, 
                                stepResults, 
                                errors,
                                context, 
                                status,
                                errorHandlingMode, 
                                cancellationToken);
                            
                            successfulSteps += childSuccess;
                            failedSteps += childFailed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing hierarchical step '{step.Name}': {ex.Message}", ex);
                
                // Update step status
                currentStepStatus.State = StepState.Failed;
                currentStepStatus.EndTime = DateTime.UtcNow;
                currentStepStatus.DurationMs = (long)((currentStepStatus.EndTime.Value - currentStepStatus.StartTime.Value).TotalMilliseconds);
                currentStepStatus.ErrorMessage = ex.Message;
                
                await UpdateWorkflowStatusAsync(status);
                
                failedSteps++;
                
                // Add error
                var error = new WorkflowError
                {
                    Code = "STEP_ERROR",
                    Message = ex.Message,
                    StepId = step.Id,
                    StepName = step.Name,
                    Timestamp = DateTime.UtcNow,
                    Severity = step.IsCritical ? ErrorSeverity.Critical : ErrorSeverity.Error,
                    Details = new Dictionary<string, object>
                    {
                        ["ExceptionType"] = ex.GetType().Name,
                        ["StackTrace"] = ex.StackTrace
                    }
                };
                
                errors.Add(error);
            }
            
            return (successfulSteps, failedSteps);
        }
        
        private Task<ActionResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken)
        {
            // Placeholder for actual step execution
            // In a real implementation, this would use the agent registry to find an agent
            // and execute the step using that agent
            
            // Simulating async work
            return Task.FromResult(new ActionResult
            {
                Success = true,
                Message = $"Executed step {step.Name} successfully",
                Data = new Dictionary<string, object>
                {
                    ["StepId"] = step.Id,
                    ["StepName"] = step.Name,
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Parameters"] = step.Parameters
                }
            });
        }
        
        private async Task<bool> ShouldExecuteStepAsync(WorkflowStep step, Dictionary<string, ActionResult> results, WorkflowContext context)
        {
            // If no condition, execute if dependencies are satisfied
            if (string.IsNullOrEmpty(step.Condition))
            {
                return step.Dependencies.All(d => results.TryGetValue(d, out var result) && result.Success);
            }
            
            // Check dependencies
            if (!step.Dependencies.All(d => results.ContainsKey(d)))
            {
                return false;
            }
            
            // Evaluate condition
            try
            {
                // Simple condition parsing
                // In a real implementation, this would use a proper expression evaluator
                var condition = step.Condition.ToLowerInvariant();
                
                if (condition.Contains("=="))
                {
                    var parts = condition.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        var leftPart = parts[0].Trim();
                        var rightPart = parts[1].Trim();
                        
                        // Check dependency result conditions
                        if (leftPart.Contains("."))
                        {
                            var pathParts = leftPart.Split('.');
                            if (pathParts.Length >= 2)
                            {
                                var dependencyName = pathParts[0];
                                var propertyName = pathParts[1];
                                
                                if (results.TryGetValue(dependencyName, out var result))
                                {
                                    if (propertyName.Equals("success", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return result.Success == bool.Parse(rightPart);
                                    }
                                }
                            }
                        }
                        
                        // Check context variables
                        if (leftPart.StartsWith("context."))
                        {
                            var variableName = leftPart.Substring(8);
                            if (context.Variables.TryGetValue(variableName, out var value))
                            {
                                return value.ToString().Equals(rightPart, StringComparison.OrdinalIgnoreCase);
                            }
                        }
                    }
                }
                
                _logger.Log(LogLevel.Warning, $"Could not evaluate condition: {step.Condition}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error evaluating condition '{step.Condition}': {ex.Message}", ex);
                return false;
            }
        }
        
        private async Task UpdateWorkflowStatusAsync(WorkflowStatus status)
        {
            // Update local status
            _workflowStatuses[status.WorkflowId] = status;
            
            // Publish status update
            await _messageBus.PublishAsync("workflow.status.updated", status);
        }
    }
}
