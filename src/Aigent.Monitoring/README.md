# Aigent.Monitoring

Monitoring and logging components for the Aigent Generic Agential System.

## Overview

The Aigent.Monitoring library provides:

1. **Logging** - Structured logging with different severity levels
2. **Metrics Collection** - Recording and summarizing metrics with tags

## Structure

- **Logging/** - Contains logging components
  - `ILogger` - Interface for logging services
  - `LogLevel` - Enum representing severity levels for logs
  - `ConsoleLogger` - Implementation of `ILogger` that writes logs to the console

- **Metrics/** - Contains metrics collection components
  - `IMetricsCollector` - Interface for metrics collection services
  - `MetricsSummary` - Class for summarizing metrics over a time period
  - `MetricData` - Class for storing metric data
  - `InMemoryMetricsCollector` - In-memory implementation of `IMetricsCollector`
  - `BasicMetricsCollector` - Basic implementation of `IMetricsCollector`

- **LegacySupport.cs** - Compatibility layer for legacy code
- **DependencyInjection.cs** - Extension methods for registering monitoring services

## Usage

### Dependency Injection

Use the extension methods to register all monitoring services:

```csharp
// Register monitoring services
services.AddMonitoring(
    useConsoleLogger: true,
    minimumLogLevel: LogLevel.Information,
    metricsCollectorType: MetricsCollectorType.InMemory);
```

### Logging

```csharp
// Get the logger from DI
var logger = serviceProvider.GetRequiredService<ILogger>();

// Log messages
logger.LogInformation("Application started");
logger.LogWarning("Resource usage is high");

try
{
    // Some operation
}
catch (Exception ex)
{
    logger.LogError("Operation failed", ex);
}
```

### Metrics Collection

```csharp
// Get the metrics collector from DI
var metrics = serviceProvider.GetRequiredService<IMetricsCollector>();

// Record metrics
metrics.RecordMetric("requests_per_second", 42);
metrics.RecordMetric("response_time_ms", 150, new Dictionary<string, string>
{
    { "endpoint", "api/users" },
    { "method", "GET" }
});

// Time operations
metrics.StartOperation("database_query");
// ... perform operation
metrics.EndOperation("database_query");

// Get summary
var summary = await metrics.GetSummary(TimeSpan.FromMinutes(5));
```

## Tests

Unit tests for the monitoring components are in the `tests/Aigent.Monitoring.Tests` directory.
