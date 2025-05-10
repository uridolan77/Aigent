# Aigent - Generic Agential System

Aigent is a robust, comprehensive generic agential system designed for building intelligent agents with various capabilities. The system provides a flexible architecture for creating reactive, deliberative, and hybrid agents that can work together to solve complex problems.

## Features

- **Multiple Agent Types**: Reactive, Deliberative, Hybrid, Learning, Utility-Based, and BDI agents
- **Memory Management**: Short-term and long-term memory with various storage options
- **Safety Framework**: Guardrails, ethical constraints, and action validation
- **Communication System**: Message bus for inter-agent communication
- **Orchestration**: Workflow execution for coordinating multiple agents
- **Monitoring**: Performance tracking and observability
- **Configuration**: Flexible configuration system with builder pattern

## Architecture

The system is organized into the following namespaces:

```
Aigent/
├── Core/               # Core interfaces and base classes
├── Memory/             # Persistence implementations
├── Safety/             # Security and ethics framework
├── Communication/      # Message bus and inter-agent messaging
├── Orchestration/      # Multi-agent coordination
├── Monitoring/         # Metrics and observability
├── Configuration/      # Configuration and agent building
└── Examples/           # Example implementations
```

## Agent Types

### Reactive Agent

Reactive agents respond directly to stimuli based on predefined rules. They are fast and efficient but lack planning capabilities.

```csharp
var reactiveConfig = new AgentConfiguration
{
    Name = "ReactiveBot",
    Type = AgentType.Reactive
};

var reactiveAgent = builder
    .WithConfiguration(reactiveConfig)
    .WithMemory<ConcurrentMemoryService>()
    .Build();
```

### Deliberative Agent

Deliberative agents plan and reason before acting. They can consider multiple options and select the best one.

```csharp
var deliberativeConfig = new AgentConfiguration
{
    Name = "DeliberativeBot",
    Type = AgentType.Deliberative
};

var deliberativeAgent = builder
    .WithConfiguration(deliberativeConfig)
    .WithMemory<SqlMemoryService>()
    .Build();
```

### Hybrid Agent

Hybrid agents combine reactive and deliberative approaches. They can respond quickly when needed but also plan when time permits.

```csharp
var hybridConfig = new AgentConfiguration
{
    Name = "HybridBot",
    Type = AgentType.Hybrid,
    Settings = new Dictionary<string, object>
    {
        ["reactiveThreshold"] = 0.7
    }
};

var hybridAgent = builder
    .WithConfiguration(hybridConfig)
    .WithMemory<RedisMemoryService>()
    .Build();
```

### Neural Network Agent

Neural network agents use machine learning for decision making. They can learn from experience and improve over time.

```csharp
var nnConfig = new AgentConfiguration
{
    Name = "NeuralNetBot",
    Type = AgentType.Learning
};

var nnAgent = builder
    .WithConfiguration(nnConfig)
    .WithMemory<ConcurrentMemoryService>()
    .WithMLModel<TensorFlowModel>()
    .Build();
```

### BDI Agent

BDI (Belief-Desire-Intention) agents model mental attitudes. They maintain beliefs about the world, generate desires based on those beliefs, and form intentions to achieve those desires.

```csharp
var bdiConfig = new AgentConfiguration
{
    Name = "BDIBot",
    Type = AgentType.BDI
};

var bdiAgent = builder
    .WithConfiguration(bdiConfig)
    .WithMemory<DocumentDbMemoryService>()
    .Build();
```

### Utility-Based Agent

Utility-based agents make decisions by evaluating the utility of each possible action and selecting the one with the highest utility. They can adapt their utility functions through learning.

```csharp
var utilityConfig = new AgentConfiguration
{
    Name = "UtilityBot",
    Type = AgentType.UtilityBased
};

var utilityAgent = builder
    .WithConfiguration(utilityConfig)
    .WithMemory<ConcurrentMemoryService>()
    .Build();
```

## Memory Services

The system provides several memory service implementations:

- **ConcurrentMemoryService**: Thread-safe in-memory storage
- **RedisMemoryService**: Redis-based distributed storage
- **SqlMemoryService**: SQL database storage
- **DocumentDbMemoryService**: MongoDB-based document database storage

## Safety Framework

The safety framework ensures that agents act within defined constraints:

- **EnhancedSafetyValidator**: Validates actions against guardrails
- **EthicalConstraintGuardrail**: Enforces ethical guidelines
- **NlpEthicsEngine**: Uses NLP to evaluate ethical implications

## Orchestration

The orchestration system coordinates multiple agents:

- **EnhancedOrchestrator**: Manages agent registration and task assignment
- **WorkflowDefinition**: Defines a sequence of steps for agents to execute
- **WorkflowTypes**: Sequential, Parallel, Conditional, and Hierarchical

## Getting Started

1. Add the Aigent services to your application:

```csharp
services.AddAigent(configuration);
```

2. Create and configure agents:

```csharp
var builder = serviceProvider.GetRequiredService<IAgentBuilder>();
var agent = builder
    .WithConfiguration(config)
    .WithMemory<ConcurrentMemoryService>()
    .WithGuardrail(new EthicalConstraintGuardrail(ethicsEngine, guidelines))
    .Build();

await agent.Initialize();
```

3. Use the agents to perform tasks:

```csharp
var state = new EnvironmentState
{
    Properties = new Dictionary<string, object>
    {
        ["input"] = "Hello, I need help with planning my day"
    }
};

var action = await agent.DecideAction(state);
var result = await action.Execute();
```

4. Create and execute workflows:

```csharp
var workflow = new WorkflowDefinition
{
    Name = "PlanningWorkflow",
    Type = WorkflowType.Sequential,
    Steps = new List<WorkflowStep>
    {
        new WorkflowStep
        {
            Name = "GreetingStep",
            RequiredAgentType = AgentType.Reactive,
            Parameters = new Dictionary<string, object>
            {
                ["input"] = "Hello"
            }
        },
        new WorkflowStep
        {
            Name = "PlanningStep",
            RequiredAgentType = AgentType.Deliberative,
            Parameters = new Dictionary<string, object>
            {
                ["input"] = "I need a plan for my day"
            },
            Dependencies = new List<string> { "GreetingStep" }
        }
    }
};

var result = await orchestrator.ExecuteWorkflow(workflow);
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Web API

The system includes a RESTful Web API for remote agent access:

### Authentication

```
POST /api/v1/auth/login
```

Authenticates a user and returns a JWT token.

### Agent Management

```
GET /api/v1/agents
GET /api/v1/agents/{id}
POST /api/v1/agents
DELETE /api/v1/agents/{id}
POST /api/v1/agents/{id}/actions
```

Endpoints for creating, retrieving, and deleting agents, as well as performing actions with agents.

### Workflow Execution

```
POST /api/v1/workflows/execute
```

Executes a workflow involving multiple agents.

### Dashboard

```
GET /api/v1/dashboard/usage
GET /api/v1/dashboard/endpoints
GET /api/v1/dashboard/users
GET /api/v1/dashboard/metrics
```

Endpoints for retrieving API usage statistics and metrics.

### API Security

The API uses JWT authentication and role-based authorization:

- **AdminOnly**: Required for creating and deleting agents and accessing the dashboard
- **ReadOnly**: Required for retrieving agents and performing actions

### Advanced API Features

#### Pagination, Filtering, and Sorting

The API supports pagination, filtering, and sorting for collection endpoints:

```
GET /api/v1/agents?page=1&pageSize=10&name=MyAgent&type=Reactive&sortBy=name&sortDirection=asc
```

Pagination metadata is included in the `X-Pagination` response header.

#### Real-time Communication

The API includes a SignalR hub for real-time communication:

```
/hubs/agent
```

Clients can subscribe to agent events to receive real-time updates when agents change status or perform actions.

#### Rate Limiting

The API includes rate limiting to prevent abuse:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 60
```

Rate limits can be configured per endpoint.

### Client SDK

The system includes a C# client SDK for easier API consumption:

```csharp
// Create client
using var client = new AigentClient("https://localhost:5001");

// Authenticate
var authResult = await client.AuthenticateAsync("admin", "admin123");

// Create an agent
var agent = await client.CreateAgentAsync(new CreateAgentRequest
{
    Name = "MyAgent",
    Type = AgentType.Reactive,
    MemoryServiceType = "DocumentDb"
});

// Perform an action
var actionResult = await client.PerformActionAsync(
    agent.Id,
    "Hello, how can you help me?"
);

// Subscribe to agent events
await client.SubscribeToAgentEventsAsync(
    agent.Id,
    (agentId, status) => Console.WriteLine($"Agent {agentId} status: {status}"),
    (agentId, action, result) => Console.WriteLine($"Agent {agentId} action: {action}")
);
```

### Example API Usage

```bash
# Authenticate
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Create an agent
curl -X POST http://localhost:5000/api/v1/agents \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"name":"MyAgent","type":"Reactive","memoryServiceType":"DocumentDb"}'

# Perform an action
curl -X POST http://localhost:5000/api/v1/agents/{id}/actions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"input":"Hello, how can you help me?"}'

# Get API usage statistics
curl -X GET http://localhost:5000/api/v1/dashboard/usage \
  -H "Authorization: Bearer {token}"
```
