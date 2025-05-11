# Aigent.Orchestration

This project contains the orchestration functionality for the Aigent system. It coordinates the work of multiple agents, executes workflows, and manages communication between agents.

## Project Structure

```
Aigent.Orchestration/
├── Interfaces/            # Interface definitions
│   ├── IOrchestrator.cs      # Main orchestrator interface
│   ├── IOrchestratorFactory.cs  # Factory for creating orchestrators
│   └── IWorkflowEngine.cs    # Interface for workflow execution engines
├── Models/               # Data models
│   ├── WorkflowContext.cs     # Context for workflow execution
│   ├── WorkflowDefinition.cs   # Definition of a workflow
│   ├── WorkflowResult.cs      # Result of workflow execution
│   ├── WorkflowStatus.cs      # Status of a workflow
│   ├── WorkflowStep.cs        # Definition of a workflow step
│   ├── OrchestratorConfiguration.cs  # Configuration for orchestrators
│   └── WorkflowEngineConfiguration.cs  # Configuration for workflow engines
├── Orchestrators/        # Orchestrator implementations
│   ├── StandardOrchestrator.cs    # Standard implementation of IOrchestrator
│   └── OrchestratorFactory.cs     # Factory for creating orchestrators
├── Engines/              # Workflow engine implementations
│   └── StandardWorkflowEngine.cs   # Standard implementation of IWorkflowEngine
├── Compatibility/        # Backward compatibility support
│   └── LegacySupport.cs          # Adapters for older interfaces
├── DependencyInjection.cs  # Extensions for registering services
├── README.md             # Project documentation
└── REFACTORING.md        # Refactoring documentation
```

## Key Features

- Agent registration and coordination
- Multi-step workflow execution
- Support for different workflow types:
  - Sequential: Steps executed in order
  - Parallel: Steps executed concurrently
  - Conditional: Steps executed based on conditions
  - Hierarchical: Steps organized in a dependency hierarchy
- Advanced error handling
- Workflow status tracking
- Customizable configuration
- Backward compatibility with existing code

## Usage Examples

### Registering Orchestration Services

```csharp
// In Startup.cs or Program.cs
services.AddOrchestration(options =>
{
    options.MaxConcurrentWorkflows = 20;
    options.DefaultWorkflowTimeoutSeconds = 600;
    options.EnableMetrics = true;
});
```

### Creating and Executing a Workflow

```csharp
// Get orchestrator from DI
var orchestrator = serviceProvider.GetRequiredService<IOrchestrator>();

// Register agents
await orchestrator.RegisterAgentAsync(agent1);
await orchestrator.RegisterAgentAsync(agent2);

// Create a workflow
var workflow = new WorkflowDefinition
{
    Name = "Data Processing Workflow",
    Description = "Processes and analyzes data from multiple sources",
    Type = WorkflowType.Sequential,
    Steps = new List<WorkflowStep>
    {
        new WorkflowStep
        {
            Name = "Data Collection",
            RequiredAgentType = AgentType.DataCollector,
            Parameters = new Dictionary<string, object>
            {
                ["dataSource"] = "api://example.com/data"
            }
        },
        new WorkflowStep
        {
            Name = "Data Analysis",
            RequiredAgentType = AgentType.Analyzer,
            Dependencies = new List<string> { "Data Collection" },
            Parameters = new Dictionary<string, object>
            {
                ["analysisType"] = "statistical"
            }
        }
    }
};

// Create execution context
var context = WorkflowContext.Default();

// Execute the workflow
var result = await orchestrator.ExecuteWorkflowAsync(workflow, context);

// Check the result
if (result.Success)
{
    Console.WriteLine($"Workflow completed successfully: {result.Message}");
    foreach (var step in result.StepResults)
    {
        Console.WriteLine($"Step {step.Key}: {step.Value.Message}");
    }
}
else
{
    Console.WriteLine($"Workflow failed: {result.Message}");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error in step {error.StepName}: {error.Message}");
    }
}
```

## Backward Compatibility

The refactored code maintains backward compatibility with the original interfaces through adapters in the `Compatibility` namespace. These adapters allow existing code to continue working without modification while internally using the new implementation.

To use legacy interfaces with the new implementation:

```csharp
// Register legacy support
services.AddLegacyOrchestrationSupport();

// Use the old interface
var legacyOrchestrator = serviceProvider.GetRequiredService<Aigent.Orchestration.IOrchestrator>();
var result = await legacyOrchestrator.ExecuteWorkflow(legacyWorkflow);
```
