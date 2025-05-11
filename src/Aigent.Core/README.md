# Aigent.Core Project

This project contains the core components and interfaces for the Aigent Generic Agential System. It defines the fundamental abstractions and contracts that all other projects depend on.

## Project Structure

The project is organized into the following logical folders:

- **Agents**: Agent implementations including base classes and specific agent types
- **Actions**: Action implementations for agents to perform
- **Configuration**: Configuration-related classes and services
- **Interfaces**: Core interfaces that define the system's contracts
- **Models**: Data models used throughout the system
- **Registry**: Agent registry components for managing agents

## Key Components

### Interfaces

- `IAgent`: The core interface for all agents in the system
- `IAgentBuilder`: Interface for building agent instances
- `IAgentRegistry`: Interface for registering and retrieving agents
- `IAction`: Interface for actions that agents can perform
- `IConfigurationSection`: Interface for configuration sections
- `IMemoryService`: Interface for agent memory services
- `IGuardrail`: Interface for safety guardrails
- `IMLModel`: Interface for machine learning models

### Models

- `AgentConfiguration`: Configuration for agent instances
- `EnvironmentState`: Represents the current state of the environment
- `AgentType`: Enumeration of agent types

## Usage Examples

```csharp
// Create a new agent configuration
var config = new AgentConfiguration
{
    Name = "MyAgent",
    Type = AgentType.Reactive,
    Settings = new Dictionary<string, object>
    {
        { "ResponseTime", 500 },
        { "MaxActions", 10 }
    }
};

// Build an agent using the builder
var agent = agentBuilder
    .WithConfiguration(config)
    .WithMemory<ConcurrentMemoryService>()
    .WithGuardrail(new ContentSafetyGuardrail())
    .Build();

// Initialize and use the agent
await agent.Initialize();
var action = await agent.DecideAction(environmentState);
```

## Backward Compatibility

The project includes a `LegacySupport` class that provides backward compatibility with older code that uses previous versions of the interfaces and classes. This ensures that existing code continues to work while enabling the use of new enhanced APIs.

## Dependencies

This project has minimal dependencies, primarily:

- Microsoft.Extensions.DependencyInjection.Abstractions
- System.Text.Json
