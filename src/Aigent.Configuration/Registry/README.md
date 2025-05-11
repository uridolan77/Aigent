# Aigent.Configuration.Registry

Agent registry components for the Aigent Generic Agential System.

## Components

- `IAgentRegistry` - Interface for agent registry services
- `AgentRegistry` - Implementation of `IAgentRegistry`

## Usage

```csharp
// Get the agent registry from DI
var registry = serviceProvider.GetRequiredService<IAgentRegistry>();

// Register an agent
await registry.RegisterAgent(agent);

// Unregister an agent
await registry.UnregisterAgent(agent.Id);

// Get a specific agent
var agent = await registry.GetAgent("agent-123");

// Get agents with filtering and pagination
var agents = await registry.GetAgents(
    name: "MyAgent", 
    type: "Advanced",
    page: 1,
    pageSize: 10,
    sortBy: "name",
    sortDirection: "asc");
```
