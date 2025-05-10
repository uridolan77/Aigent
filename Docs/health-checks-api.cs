// Health Checks and API Implementation
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace AgentSystem.Api
{
    // Startup configuration for API
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add Agent System
            services.AddAgentSystem(_configuration);
            
            // Add API controllers
            services.AddControllers();
            
            // Add Swagger/OpenAPI
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Agent System API", 
                    Version = "v1",
                    Description = "API for interacting with the Agent System",
                    Contact = new OpenApiContact
                    {
                        Name = "Agent System Team",
                        Email = "support@agentsystem.dev"
                    }
                });
                
                // Add security definition
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            
            // Add health checks
            services.AddHealthChecks()
                .AddCheck<AgentSystemHealthCheck>("agent_system")
                .AddCheck<MemoryHealthCheck>("memory_service")
                .AddCheck<OrchestrationHealthCheck>("orchestration")
                .AddRedis(_configuration["AgentSystem:Redis:ConnectionString"], name: "redis")
                .AddSqlServer(_configuration["AgentSystem:SQL:ConnectionString"], name: "sql")
                .AddCheck<MessageBusHealthCheck>("message_bus");
            
            // Add health checks UI
            services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(15);
                setup.MaximumHistoryEntriesPerEndpoint(50);
            })
            .AddInMemoryStorage();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agent System API v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                // Health check endpoints
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = check => !check.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                
                // Health check UI
                endpoints.MapHealthChecksUI(options => options.UIPath = "/health-ui");
            });
        }
    }

    // API Controllers
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly IOrchestrator _orchestrator;
        private readonly IAgentBuilder _agentBuilder;
        private readonly ILogger<AgentsController> _logger;

        public AgentsController(
            IOrchestrator orchestrator,
            IAgentBuilder agentBuilder,
            ILogger<AgentsController> logger)
        {
            _orchestrator = orchestrator;
            _agentBuilder = agentBuilder;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgentInfo>>> GetAgents()
        {
            // Get registered agents
            var agents = await _orchestrator.GetRegisteredAgents();
            return Ok(agents.Select(a => new AgentInfo
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type.ToString(),
                Capabilities = a.Capabilities
            }));
        }

        [HttpPost]
        public async Task<ActionResult<AgentInfo>> CreateAgent([FromBody] CreateAgentRequest request)
        {
            try
            {
                var config = new AgentConfiguration
                {
                    Name = request.Name,
                    Type = Enum.Parse<AgentType>(request.Type),
                    Settings = request.Settings
                };

                var agent = _agentBuilder
                    .WithConfiguration(config)
                    .Build();

                await _orchestrator.RegisterAgent(agent);
                await agent.Initialize();

                return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, new AgentInfo
                {
                    Id = agent.Id,
                    Name = agent.Name,
                    Type = agent.Type.ToString(),
                    Capabilities = agent.Capabilities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AgentInfo>> GetAgent(string id)
        {
            var agent = await _orchestrator.GetAgent(id);
            if (agent == null)
                return NotFound();

            return Ok(new AgentInfo
            {
                Id = agent.Id,
                Name = agent.Name,
                Type = agent.Type.ToString(),
                Capabilities = agent.Capabilities
            });
        }

        [HttpPost("{id}/decide")]
        public async Task<ActionResult<ActionResponse>> DecideAction(string id, [FromBody] DecideActionRequest request)
        {
            var agent = await _orchestrator.GetAgent(id);
            if (agent == null)
                return NotFound();

            var state = new EnvironmentState
            {
                Properties = request.State
            };

            var action = await agent.DecideAction(state);
            
            return Ok(new ActionResponse
            {
                ActionType = action.ActionType,
                Parameters = action.Parameters
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgent(string id)
        {
            var agent = await _orchestrator.GetAgent(id);
            if (agent == null)
                return NotFound();

            await _orchestrator.UnregisterAgent(id);
            await agent.Shutdown();
            
            return NoContent();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowsController : ControllerBase
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ILogger<WorkflowsController> _logger;

        public WorkflowsController(IOrchestrator orchestrator, ILogger<WorkflowsController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<WorkflowResult>> ExecuteWorkflow([FromBody] WorkflowDefinition workflow)
        {
            try
            {
                var result = await _orchestrator.ExecuteWorkflow(workflow);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsCollector _metrics;

        public MetricsController(IMetricsCollector metrics)
        {
            _metrics = metrics;
        }

        [HttpGet]
        public async Task<ActionResult<MetricsSummary>> GetMetrics([FromQuery] int duration = 300)
        {
            var summary = await _metrics.GetSummary(TimeSpan.FromSeconds(duration));
            return Ok(summary);
        }

        [HttpPost]
        public IActionResult RecordMetric([FromBody] RecordMetricRequest request)
        {
            _metrics.RecordMetric(request.Name, request.Value, request.Tags);
            return NoContent();
        }
    }

    // Request and Response DTOs
    public class CreateAgentRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }

    public class AgentInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public AgentCapabilities Capabilities { get; set; }
    }

    public class DecideActionRequest
    {
        public Dictionary<string, object> State { get; set; }
    }

    public class ActionResponse
    {
        public string ActionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class RecordMetricRequest
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    // Health Check Implementations
    public class AgentSystemHealthCheck : IHealthCheck
    {
        private readonly IOrchestrator _orchestrator;

        public AgentSystemHealthCheck(IOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var agents = await _orchestrator.GetRegisteredAgents();
                if (agents.Any())
                {
                    return HealthCheckResult.Healthy($"{agents.Count} agents registered");
                }
                return HealthCheckResult.Degraded("No agents registered");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Agent system error", ex);
            }
        }
    }

    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly IMemoryService _memoryService;

        public MemoryHealthCheck(IMemoryService memoryService)
        {
            _memoryService = memoryService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Test memory service
                await _memoryService.StoreContext("health_check", DateTime.UtcNow);
                var result = await _memoryService.RetrieveContext<DateTime>("health_check");
                
                return HealthCheckResult.Healthy("Memory service operational");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Memory service error", ex);
            }
        }
    }

    public class OrchestrationHealthCheck : IHealthCheck
    {
        private readonly IOrchestrator _orchestrator;

        public OrchestrationHealthCheck(IOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Test orchestration
                var testWorkflow = new WorkflowDefinition
                {
                    Name = "Health Check Workflow",
                    Type = WorkflowType.Sequential,
                    Steps = new List<WorkflowStep>()
                };

                var result = await _orchestrator.ExecuteWorkflow(testWorkflow);
                return HealthCheckResult.Healthy("Orchestration operational");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Orchestration error", ex);
            }
        }
    }

    public class MessageBusHealthCheck : IHealthCheck
    {
        private readonly IMessageBus _messageBus;

        public MessageBusHealthCheck(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var received = false;
                var testTopic = "health_check_" + Guid.NewGuid();
                
                _messageBus.Subscribe(testTopic, (msg) => received = true);
                await _messageBus.PublishAsync(testTopic, "test");
                
                await Task.Delay(100);
                
                return received ? 
                    HealthCheckResult.Healthy("Message bus operational") :
                    HealthCheckResult.Unhealthy("Message bus not responding");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Message bus error", ex);
            }
        }
    }

    // GraphQL Support
    public class AgentQuery
    {
        private readonly IOrchestrator _orchestrator;

        public AgentQuery(IOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async Task<IEnumerable<IAgent>> GetAgents()
        {
            return await _orchestrator.GetRegisteredAgents();
        }

        public async Task<IAgent> GetAgent(string id)
        {
            return await _orchestrator.GetAgent(id);
        }
    }

    public class AgentMutation
    {
        private readonly IOrchestrator _orchestrator;
        private readonly IAgentBuilder _agentBuilder;

        public AgentMutation(IOrchestrator orchestrator, IAgentBuilder agentBuilder)
        {
            _orchestrator = orchestrator;
            _agentBuilder = agentBuilder;
        }

        public async Task<IAgent> CreateAgent(string name, AgentType type, Dictionary<string, object> settings)
        {
            var config = new AgentConfiguration
            {
                Name = name,
                Type = type,
                Settings = settings
            };

            var agent = _agentBuilder
                .WithConfiguration(config)
                .Build();

            await _orchestrator.RegisterAgent(agent);
            await agent.Initialize();

            return agent;
        }

        public async Task<bool> DeleteAgent(string id)
        {
            await _orchestrator.UnregisterAgent(id);
            return true;
        }
    }

    public class AgentSubscription
    {
        private readonly IMessageBus _messageBus;

        public AgentSubscription(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public IObservable<object> OnAgentMessage(string agentId)
        {
            return Observable.Create<object>(observer =>
            {
                void Handler(object message) => observer.OnNext(message);
                
                _messageBus.Subscribe($"agent.{agentId}.message", Handler);
                
                return () => _messageBus.Unsubscribe($"agent.{agentId}.message", Handler);
            });
        }
    }
}
