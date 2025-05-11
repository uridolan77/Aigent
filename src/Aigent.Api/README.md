# Aigent.Api Project

This project provides the REST API and real-time communication interfaces for the Aigent Generic Agential System. It serves as the primary entry point for client applications to interact with the agent system.

## Project Structure

The API project is organized into the following logical folders:

- **Controllers**: REST API controllers for agent management, authentication, and other operations
- **Models**: Data transfer objects and request/response models
- **Interfaces**: Core interface definitions used by the API components
- **Services**: Service implementations for business logic
- **Middleware**: HTTP pipeline middleware components for cross-cutting concerns
- **Hubs**: SignalR hubs for real-time communication
- **Authentication**: Authentication and authorization components
- **Analytics**: API usage analytics and monitoring
- **Extensions**: Extension methods for service registration and configuration
- **Validators**: Input validation rules and helpers
- **Constants**: Constants used throughout the API

## Key Components

### Controllers

- **AgentsController**: CRUD operations for agent management
- **AuthController**: Authentication and authorization endpoints
- **DashboardController**: Metrics and monitoring endpoints
- **WorkflowsController**: Endpoints for agent workflow management

### Models

- **AgentModels**: Data transfer objects for agent operations
- **AuthModels**: Authentication request and response models
- **ApiResponse**: Standard API response wrapper for consistency

### Services

- **AgentRegistryService**: Service for managing agents
- **TokenService**: Service for JWT token generation and validation
- **UserService**: Service for user management
- **ApiAnalyticsService**: Service for API analytics

### Middleware

- **ErrorHandlingMiddleware**: Central error handling and logging
- **ApiAnalyticsMiddleware**: Request tracking and metrics collection
- **RateLimitingMiddleware**: Rate limiting to prevent abuse
- **PaginationMiddleware**: Adds pagination support to responses
- **RequestLoggingMiddleware**: Logs request and response details

## Usage

The API exposes a set of RESTful endpoints for managing agents and workflows in the Aigent system:

```http
# Authentication
POST /api/v1/auth/login
POST /api/v1/auth/refresh

# Agent Management
GET /api/v1/agents
GET /api/v1/agents/{id}
POST /api/v1/agents
DELETE /api/v1/agents/{id}
POST /api/v1/agents/{id}/actions

# Workflows
GET /api/v1/workflows
POST /api/v1/workflows
GET /api/v1/workflows/{id}
PUT /api/v1/workflows/{id}
DELETE /api/v1/workflows/{id}

# Dashboard / Metrics
GET /api/v1/dashboard/metrics
```

## Real-Time Communication

The API includes SignalR hubs for real-time communication with agents:

```csharp
// Connect to the agent hub
var connection = new HubConnectionBuilder()
    .WithUrl("https://api.example.com/hubs/agent")
    .WithAutomaticReconnect()
    .Build();

// Subscribe to agent events
connection.On<string, string>("AgentStatusChanged", (agentId, status) => {
    Console.WriteLine($"Agent {agentId} status changed to {status}");
});

await connection.StartAsync();
```

## Authentication

The API uses JWT Bearer tokens for authentication. To access protected endpoints, clients must include an `Authorization` header with a valid JWT token:

```http
GET /api/v1/agents
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Extensions and Customization

The API is designed to be extensible through middleware components and service registrations. To add new capabilities, follow these patterns:

1. Define interfaces in the `Interfaces` directory
2. Implement services in the `Services` directory
3. Register services in the `ServiceCollectionExtensions` class
4. Add API endpoints in appropriate controllers

## Dependencies

This project has dependencies on the following Aigent packages:

- Aigent.Core
- Aigent.Memory
- Aigent.Configuration
- Aigent.Monitoring
- Aigent.Safety
- Aigent.Communication
