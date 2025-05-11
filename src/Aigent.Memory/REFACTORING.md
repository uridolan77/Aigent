# Memory Project Refactoring

This document describes the refactoring of the Aigent.Memory project.

## Overview

The Memory project has been refactored to follow the patterns established in other Aigent projects (Core, Monitoring, Configuration). The refactoring includes:

1. Creating a logical folder structure
2. Enhancing interfaces and implementations
3. Adding support for different memory types
4. Ensuring backward compatibility

## Folder Structure

The project now has the following folder structure:

- **Interfaces/**: Memory-related interfaces
  - IMemoryService.cs
  - IMemoryProvider.cs
  - IShortTermMemory.cs
  - ILongTermMemory.cs
  - IMemoryServiceFactory.cs
- **Models/**: Memory-related data models
  - MemoryEntry.cs
  - MemoryServiceOptions.cs
- **Providers/**: Memory implementations
  - BaseMemoryService.cs
  - ConcurrentMemoryService.cs
  - LazyCacheMemoryService.cs
  - MongoDbMemoryService.cs
  - ConcurrentMemoryProvider.cs
  - LazyCacheProvider.cs
  - MongoDbProvider.cs
- **Compatibility/**: Backward compatibility
  - LegacyMemoryService.cs
- **Root**:
  - MemoryServiceFactory.cs
  - DependencyInjection.cs
  - LegacySupport.cs
  - README.md

## Enhanced Interfaces

The refactoring introduces enhanced interfaces with additional capabilities:

### IMemoryService

- All methods now use the `Async` suffix
- Added support for hierarchical memory with sections
- Added support for key patterns and filtering
- Added methods to store and retrieve agent state

### IMemoryProvider

- Represents the storage backend for memory services
- Abstracts implementation details of different storage types
- Makes it easier to add new storage backends

### IShortTermMemory

- Adds methods for managing expiration times
- Optimized for frequently accessed, temporary data

### ILongTermMemory

- Adds methods for metadata management
- Adds search capabilities
- Ensures persistence of data

## Implementation Improvements

The refactoring includes several implementation improvements:

- **BaseMemoryService**: Abstract base class that implements common functionality
- **Provider Pattern**: Separation of the storage mechanism from the service interface
- **Dependency Injection**: Better support for DI with extension methods
- **MongoDB Support**: Improved implementation of MongoDB-based long-term memory
- **Key Prefixing**: Automatic prefixing of keys with agent ID to prevent collisions

## Backward Compatibility

Backward compatibility is maintained through several mechanisms:

1. Original interfaces marked with `[Obsolete]` attribute
2. Legacy adapter classes that implement old interfaces using new implementations
3. `LegacySupport` class with helper methods for creating adapters

## Migration Guide

To migrate from the old interfaces to the new ones:

1. Replace `using Aigent.Memory` with `using Aigent.Memory.Interfaces`
2. Update method calls to use the `Async` suffix
3. Use the new factory methods for creating memory services
4. Take advantage of new features like sections and metadata

Example:

```csharp
// Old code
var memory = factory.CreateMemoryService("agent-123");
await memory.StoreContext("key", value);
var result = await memory.RetrieveContext<string>("key");

// New code
var memory = factory.CreateMemoryService("agent-123");
await memory.StoreAsync("key", value);
var result = await memory.RetrieveAsync<string>("key");

// New features
await memory.StoreSectionAsync("preferences", "theme", "dark");
await memory.StoreWithMetadataAsync("user", "John", new Dictionary<string, object> { ["role"] = "admin" });
```
