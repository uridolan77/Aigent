# Aigent.Configuration.DI

Dependency injection extensions for the Aigent Generic Agential System.

## Components

- `ServiceCollectionExtensions` - Extension methods for registering Aigent services
- `ConfigurationAdapter` - Adapter for Microsoft.Extensions.Configuration to Aigent.Configuration.Core.IConfiguration

## Usage

```csharp
// In Program.cs or Startup.cs:

// Register all Aigent services with the specified configuration
services.AddAigent(configuration);

// Or just register configuration services
services.AddAigentConfiguration(configuration);
```

## Service Registration

The `AddAigent` method registers the following services:

- `ILogger` - Logging service
- `IMessageBus` - Message bus for agent communication
- `ISafetyValidator` - Safety validation service
- `IMetricsCollector` - Metrics collection service
- `IMemoryService` - Memory service based on configuration
- `IAgentBuilder` - Agent builder service
- `IAgentRegistry` - Agent registry service
- `IOrchestrator` - Orchestration service

Memory service is selected based on the "Aigent:MemoryType" configuration value:
- "Redis" - Uses Redis-based memory service
- "SQL" - Uses SQL-based memory service
- "DocumentDb" - Uses document database-based memory service
- Default - Uses in-memory LazyCache-based memory service
