# Aigent.Configuration

Configuration components for the Aigent Generic Agential System.

## Overview

The Aigent.Configuration library provides:

1. **Agent Builder** - Fluent interface for creating agent instances
2. **Configuration** - Configuration access and management
3. **Registry** - Agent registration and discovery
4. **Dependency Injection** - Service registration extensions

## Structure

- **Builders/** - Contains agent builder components
  - `IAgentBuilder` - Interface for agent builder services
  - `EnhancedAgentBuilder` - Implementation of `IAgentBuilder` with advanced features

- **Core/** - Contains core configuration components
  - `IConfiguration` - Interface for configuration access
  - `IConfigurationSection` - Interface for configuration sections
  - `ConfigurationSection` - Implementation of `IConfigurationSection`
  - `AgentConfiguration` - Configuration for an agent instance

- **DI/** - Contains dependency injection extensions
  - `ServiceCollectionExtensions` - Extensions for registering Aigent services

- **Registry/** - Contains agent registry components
  - `IAgentRegistry` - Interface for agent registry services
  - `AgentRegistry` - Implementation of `IAgentRegistry`

- **LegacySupport.cs** - Compatibility layer for legacy code

## Usage

### Dependency Injection

```csharp
// Register all Aigent services
services.AddAigent(configuration);

// Or just register configuration services
services.AddAigentConfiguration(configuration);
```

### Building Agents

```csharp
// Get the agent builder from DI
var agentBuilder = serviceProvider.GetRequiredService<IAgentBuilder>();

// Build an agent with fluent API
var agent = agentBuilder
    .WithConfiguration(new AgentConfiguration("MyAgent", AgentType.Advanced))
    .WithMemory<LazyCacheMemoryService>()
    .WithGuardrail(new ContentSafetyGuardrail())
    .WithRule("GreetUser", state => new GreetingAction())
    .Build();
```

### Agent Registry

```csharp
// Get the agent registry from DI
var registry = serviceProvider.GetRequiredService<IAgentRegistry>();

// Register an agent
await registry.RegisterAgent(agent);

// Get agents
var agents = await registry.GetAgents(
    name: "MyAgent", 
    type: "Advanced",
    page: 1,
    pageSize: 10,
    sortBy: "name",
    sortDirection: "asc");
```

### Configuration

```csharp
// Get configuration from DI
var config = serviceProvider.GetRequiredService<IConfiguration>();

// Get values
var connectionString = config.GetValue("ConnectionStrings:DefaultConnection");
var maxItems = config.GetValue<int>("Limits:MaxItems", 100);

// Get sections
var section = config.GetSection("MySection");
var sectionValue = section.Get<string>();
```
