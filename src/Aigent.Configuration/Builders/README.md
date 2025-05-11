# Aigent.Configuration.Builders

Agent builder components for the Aigent Generic Agential System.

## Components

- `IAgentBuilder` - Interface for agent builder services
- `EnhancedAgentBuilder` - Implementation of `IAgentBuilder` with advanced features

## Usage

```csharp
// Get the agent builder from DI
var agentBuilder = serviceProvider.GetRequiredService<IAgentBuilder>();

// Build a basic agent
var basicAgent = agentBuilder
    .WithConfiguration(new AgentConfiguration("BasicAgent", AgentType.Basic))
    .WithMemory<LazyCacheMemoryService>()
    .Build();

// Build an advanced agent with more features
var advancedAgent = agentBuilder
    .WithConfiguration(new AgentConfiguration("AdvancedAgent", AgentType.Advanced))
    .WithMemory<DocumentDbMemoryService>()
    .WithGuardrail(new ContentSafetyGuardrail())
    .WithMLModel<LanguageModel>()
    .WithRulesFromFile("rules.json")
    .WithRule("GreetUser", state => new GreetingAction())
    .WithOption("MaxTokens", 1024)
    .Build();
```
