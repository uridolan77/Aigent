# Aigent.Configuration.Core

Core configuration components for the Aigent Generic Agential System.

## Components

- `IConfiguration` - Interface for configuration access
- `IConfigurationSection` - Interface for configuration sections
- `ConfigurationSection` - Implementation of `IConfigurationSection`
- `AgentConfiguration` - Configuration for an agent instance

## Usage

### Configuration Access

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

### Agent Configuration

```csharp
// Create a basic agent configuration
var config = new AgentConfiguration
{
    Name = "MyAgent",
    Type = AgentType.Basic,
    Description = "A simple agent for demonstration purposes",
    Enabled = true,
    Tags = new List<string> { "demo", "basic" }
};

// Add settings
config.Settings["MaxTokens"] = 1024;
config.Settings["Temperature"] = 0.7;
config.Settings["ModelName"] = "gpt-4";
```
