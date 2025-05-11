# Aigent.Memory

This project contains memory services for the Aigent Generic Agential System.

## Overview

The Memory project provides implementations for storing agent state, context, and other data. It supports different storage backends and includes both short-term (in-memory) and long-term (persistent) memory services.

## Key Components

### Interfaces

- `IMemoryService`: Core interface for memory services
- `IShortTermMemory`: Interface for short-term, in-memory storage
- `ILongTermMemory`: Interface for long-term, persistent storage
- `IMemoryProvider`: Interface for storage backend implementations
- `IMemoryServiceFactory`: Factory for creating memory services

### Memory Services

- `ConcurrentMemoryService`: Thread-safe in-memory implementation using ConcurrentDictionary
- `LazyCacheMemoryService`: Memory service using LazyCache for efficient caching
- `MongoDbMemoryService`: MongoDB-based persistent storage implementation

### Memory Providers

- `ConcurrentMemoryProvider`: Thread-safe in-memory provider
- `LazyCacheProvider`: Provider using LazyCache
- `MongoDbProvider`: Provider using MongoDB for persistent storage

### Factories

- `MemoryServiceFactory`: Creates memory services based on configuration

## Usage

### Basic Usage

```csharp
// Create a memory service
var memoryService = new ConcurrentMemoryService("agent-123");

// Store a value
await memoryService.StoreAsync("greeting", "Hello, world!");

// Retrieve a value
var greeting = await memoryService.RetrieveAsync<string>("greeting");
```

### Using the Factory

```csharp
// Create a factory
var factory = new MemoryServiceFactory(new LazyCache.CachingService());

// Create a memory service
var memoryService = factory.CreateMemoryService("agent-123");

// Create a short-term memory service
var shortTermMemory = factory.CreateShortTermMemory("agent-123");

// Create a long-term memory service
var longTermMemory = factory.CreateLongTermMemory("agent-123");
```

### Dependency Injection

```csharp
// In Startup.cs or Program.cs
services.AddAigentMemory();

// Or with MongoDB
services.AddMongoDbMemory("mongodb://localhost:27017", "AigentDb", "AgentMemory");

// In a service
public class AgentService
{
    private readonly IMemoryServiceFactory _factory;
    
    public AgentService(IMemoryServiceFactory factory)
    {
        _factory = factory;
    }
    
    public async Task DoSomething(string agentId)
    {
        var memory = _factory.CreateMemoryService(agentId);
        await memory.StoreAsync("started", DateTime.UtcNow);
        
        // Use memory service...
    }
}
```

## Advanced Features

### Sectioned Memory

```csharp
// Store in sections
await memory.StoreSectionAsync("preferences", "theme", "dark");
await memory.StoreSectionAsync("preferences", "fontSize", 14);

// Retrieve from sections
var theme = await memory.RetrieveSectionAsync<string>("preferences", "theme");

// Get all keys in a section
var keys = await memory.GetSectionKeysAsync("preferences");

// Clear a section
await memory.ClearSectionAsync("preferences");
```

### Metadata (Long-Term Memory)

```csharp
var longTermMemory = factory.CreateLongTermMemory("agent-123");

// Store with metadata
var metadata = new Dictionary<string, object>
{
    ["importance"] = "high",
    ["category"] = "user-preference",
    ["created-by"] = "system"
};

await longTermMemory.StoreWithMetadataAsync("user-name", "John Doe", metadata);

// Get metadata
var storedMetadata = await longTermMemory.GetMetadataAsync("user-name");
```

### Search (Long-Term Memory)

```csharp
// Search for keys and values matching a query
var results = await longTermMemory.SearchAsync("user preferences", limit: 10);
```

## Compatibility

The project includes backward compatibility with the legacy `IMemoryService` interface:

```csharp
// Create a legacy adapter
var legacyService = LegacySupport.CreateLegacyAdapter(modernService);

// Create a legacy factory
var legacyFactory = LegacySupport.CreateLegacyFactoryAdapter(modernFactory);
```
