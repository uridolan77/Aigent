# Aigent.Monitoring.Metrics

This directory contains components for metrics collection within the Aigent Generic Agential System.

## Components

- `IMetricsCollector`: Interface for metrics collection services
- `MetricsSummary`: Class for summarizing metrics over a time period
- `MetricData`: Class for storing metric data
- `InMemoryMetricsCollector`: In-memory implementation of `IMetricsCollector`
- `BasicMetricsCollector`: Basic implementation of `IMetricsCollector`

## Usage

```csharp
// Create a metrics collector
var logger = new ConsoleLogger(LogLevel.Debug);
var metricsCollector = new InMemoryMetricsCollector(logger);

// Record a metric
metricsCollector.RecordMetric("api_requests", 1);

// Record a metric with tags
metricsCollector.RecordMetric("api_response_time", 150, new Dictionary<string, string>
{
    { "endpoint", "users" },
    { "method", "GET" }
});

// Time an operation
metricsCollector.StartOperation("database_query");
// ... perform database query
metricsCollector.EndOperation("database_query");

// Get a summary of metrics for the last 5 minutes
TimeSpan duration = TimeSpan.FromMinutes(5);
MetricsSummary summary = await metricsCollector.GetSummary(duration);

// Process the summary
foreach (var metric in summary.Metrics)
{
    Console.WriteLine($"Metric: {metric.Key}");
    Console.WriteLine($"  Count: {metric.Value.Count}");
    Console.WriteLine($"  Average: {metric.Value.Average:F2}");
    Console.WriteLine($"  Min: {metric.Value.Min:F2}");
    Console.WriteLine($"  Max: {metric.Value.Max:F2}");
    
    // Process tags
    foreach (var tagKey in metric.Value.Tags.Keys)
    {
        Console.WriteLine($"  Tag: {tagKey} = {string.Join(", ", metric.Value.Tags[tagKey])}");
    }
}
```

## Dependency Injection

You can register the metrics collector in your service collection:

```csharp
services.AddSingleton<IMetricsCollector>(provider => 
    new InMemoryMetricsCollector(provider.GetService<ILogger>()));
```

Or use the extension method:

```csharp
services.AddMonitoring(
    useConsoleLogger: true, 
    minimumLogLevel: LogLevel.Information,
    metricsCollectorType: MetricsCollectorType.InMemory);
```
