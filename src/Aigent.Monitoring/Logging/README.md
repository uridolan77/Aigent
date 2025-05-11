# Aigent.Monitoring.Logging

This directory contains components for logging within the Aigent Generic Agential System.

## Components

- `ILogger`: Interface for logging services
- `LogLevel`: Enum representing severity levels for logs
- `ConsoleLogger`: Implementation of `ILogger` that writes logs to the console

## Usage

```csharp
// Create a console logger with minimum level
var logger = new ConsoleLogger(LogLevel.Debug);

// Log messages
logger.LogDebug("Detailed debugging information");
logger.LogInformation("General information");
logger.LogWarning("Warning about potential issues");
logger.LogError("Error information");
logger.LogCritical("Critical error information");

// Log exception details
try
{
    // Some operation
}
catch (Exception ex)
{
    logger.LogError("An error occurred", ex);
}
```

## Dependency Injection

You can register the logger in your service collection:

```csharp
services.AddSingleton<ILogger>(new ConsoleLogger(LogLevel.Information));
```

Or use the extension method:

```csharp
services.AddMonitoring(useConsoleLogger: true, minimumLogLevel: LogLevel.Information);
```
