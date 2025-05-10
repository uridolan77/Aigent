using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Orchestration;
using Aigent.Monitoring;

namespace Aigent.Examples
{
    /// <summary>
    /// Example demonstrating workflow orchestration
    /// </summary>
    public class WorkflowExample
    {
        private readonly IServiceProvider _serviceProvider;
        
        /// <summary>
        /// Initializes a new instance of the WorkflowExample class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        public WorkflowExample(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        
        /// <summary>
        /// Runs the example
        /// </summary>
        public async Task Run()
        {
            Console.WriteLine("Workflow Orchestration Example");
            Console.WriteLine("-----------------------------");
            
            // Get the orchestrator
            var orchestrator = _serviceProvider.GetRequiredService<IOrchestrator>();
            var logger = _serviceProvider.GetRequiredService<ILogger>();
            
            // Create a sequential workflow
            logger.Log(LogLevel.Information, "Creating sequential workflow...");
            var sequentialWorkflow = new WorkflowDefinition
            {
                Name = "SequentialWorkflow",
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "GreetingStep",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Hello"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "PlanningStep",
                        RequiredAgentType = AgentType.Deliberative,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "I need a plan for my day"
                        },
                        Dependencies = new List<string> { "GreetingStep" }
                    }
                }
            };
            
            // Execute the sequential workflow
            Console.WriteLine("\nExecuting Sequential Workflow");
            Console.WriteLine("----------------------------");
            var sequentialResult = await orchestrator.ExecuteWorkflow(sequentialWorkflow);
            
            Console.WriteLine($"Workflow success: {sequentialResult.Success}");
            Console.WriteLine($"Workflow message: {sequentialResult.Message}");
            
            foreach (var step in sequentialResult.Results)
            {
                Console.WriteLine($"\nStep: {step.Key}");
                Console.WriteLine($"Success: {step.Value.Success}");
                Console.WriteLine($"Message: {step.Value.Message}");
                
                if (step.Value.Data.TryGetValue("text", out var text))
                {
                    Console.WriteLine($"Text: {text}");
                }
            }
            
            // Create a parallel workflow
            logger.Log(LogLevel.Information, "Creating parallel workflow...");
            var parallelWorkflow = new WorkflowDefinition
            {
                Name = "ParallelWorkflow",
                Type = WorkflowType.Parallel,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "ReactiveStep",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Hello from parallel workflow"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "DeliberativeStep",
                        RequiredAgentType = AgentType.Deliberative,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Plan my day in parallel"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "BDIStep",
                        RequiredAgentType = AgentType.BDI,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Process this with BDI reasoning"
                        }
                    }
                }
            };
            
            // Execute the parallel workflow
            Console.WriteLine("\nExecuting Parallel Workflow");
            Console.WriteLine("--------------------------");
            var parallelResult = await orchestrator.ExecuteWorkflow(parallelWorkflow);
            
            Console.WriteLine($"Workflow success: {parallelResult.Success}");
            Console.WriteLine($"Workflow message: {parallelResult.Message}");
            
            foreach (var step in parallelResult.Results)
            {
                Console.WriteLine($"\nStep: {step.Key}");
                Console.WriteLine($"Success: {step.Value.Success}");
                Console.WriteLine($"Message: {step.Value.Message}");
                
                if (step.Value.Data.TryGetValue("text", out var text))
                {
                    Console.WriteLine($"Text: {text}");
                }
            }
            
            // Create a conditional workflow
            logger.Log(LogLevel.Information, "Creating conditional workflow...");
            var conditionalWorkflow = new WorkflowDefinition
            {
                Name = "ConditionalWorkflow",
                Type = WorkflowType.Conditional,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "EvaluationStep",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Evaluate this condition"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "TrueStep",
                        RequiredAgentType = AgentType.Deliberative,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Condition was true"
                        },
                        Dependencies = new List<string> { "EvaluationStep" },
                        Condition = "EvaluationStep.Success == true"
                    },
                    new WorkflowStep
                    {
                        Name = "FalseStep",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Condition was false"
                        },
                        Dependencies = new List<string> { "EvaluationStep" },
                        Condition = "EvaluationStep.Success == false"
                    }
                }
            };
            
            // Execute the conditional workflow
            Console.WriteLine("\nExecuting Conditional Workflow");
            Console.WriteLine("-----------------------------");
            var conditionalResult = await orchestrator.ExecuteWorkflow(conditionalWorkflow);
            
            Console.WriteLine($"Workflow success: {conditionalResult.Success}");
            Console.WriteLine($"Workflow message: {conditionalResult.Message}");
            
            foreach (var step in conditionalResult.Results)
            {
                Console.WriteLine($"\nStep: {step.Key}");
                Console.WriteLine($"Success: {step.Value.Success}");
                Console.WriteLine($"Message: {step.Value.Message}");
                
                if (step.Value.Data.TryGetValue("text", out var text))
                {
                    Console.WriteLine($"Text: {text}");
                }
            }
            
            Console.WriteLine("\nExample completed.");
        }
    }
}
