// Production-Ready Demo Application
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AgentSystem.Core;
using AgentSystem.Safety;
using AgentSystem.Memory;
using AgentSystem.Utilities;
using AgentSystem.Agents;
using AgentSystem.Testing;
using AgentSystem.Monitoring;
using AgentSystem.Examples;
using AgentSystem.Orchestration;
using AgentSystem.Communication;
using AgentSystem.Security;
using AgentSystem.Deployment;

namespace AgentSystem.Demo
{
    // Enhanced Demo Application with all production features
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            try
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Starting Agent System...");
                
                using (var scope = host.Services.CreateScope())
                {
                    var demo = scope.ServiceProvider.GetRequiredService<ProductionDemo>();
                    await demo.RunAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogCritical(ex, "Application terminated unexpectedly");
            }
            finally
            {
                await host.StopAsync();
                host.Dispose();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(context.HostingEnvironment.ContentRootPath)
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                          .AddEnvironmentVariables()
                          .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddAgentSystem(context.Configuration);
                    services.AddHostedService<AgentSystemHostedService>();
                    services.AddTransient<ProductionDemo>();
                });
    }

    // Background service for agent system
    public class AgentSystemHostedService : BackgroundService
    {
        private readonly ILogger<AgentSystemHostedService> _logger;
        private readonly IMetricsCollector _metrics;
        private readonly IOrchestrator _orchestrator;

        public AgentSystemHostedService(
            ILogger<AgentSystemHostedService> logger,
            IMetricsCollector metrics,
            IOrchestrator orchestrator)
        {
            _logger = logger;
            _metrics = metrics;
            _orchestrator = orchestrator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agent System background service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Collect system metrics
                    _metrics.RecordMetric("system.health", 1.0);
                    
                    // Perform periodic maintenance
                    await PerformMaintenanceAsync();
                    
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background service");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("Agent System background service stopping...");
        }

        private async Task PerformMaintenanceAsync()
        {
            // Cleanup expired memory entries, refresh secrets, etc.
            _logger.LogDebug("Performing system maintenance...");
        }
    }

    // Production demo with all features
    public class ProductionDemo
    {
        private readonly ILogger<ProductionDemo> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOrchestrator _orchestrator;
        private readonly IMetricsCollector _metrics;
        private readonly IMessageBus _messageBus;

        public ProductionDemo(
            ILogger<ProductionDemo> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IOrchestrator orchestrator,
            IMetricsCollector metrics,
            IMessageBus messageBus)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _orchestrator = orchestrator;
            _metrics = metrics;
            _messageBus = messageBus;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("=== Production Agent System Demo ===");
            
            // 1. Create and configure agents
            var agents = await CreateProductionAgents();
            
            // 2. Demonstrate multi-agent orchestration
            await DemonstrateOrchestration(agents);
            
            // 3. Show security features
            await DemonstrateSecurityFeatures();
            
            // 4. Demonstrate monitoring and metrics
            await DemonstrateMonitoring();
            
            // 5. Run comprehensive tests
            await RunProductionTests(agents);
            
            // 6. Interactive demo session
            await RunInteractiveSession(agents);
            
            _logger.LogInformation("=== Demo Completed Successfully ===");
        }

        private async Task<Dictionary<string, IAgent>> CreateProductionAgents()
        {
            _logger.LogInformation("Creating production agents...");
            
            var agents = new Dictionary<string, IAgent>();
            var builder = _serviceProvider.GetRequiredService<IAgentBuilder>();
            
            // Configure safety with advanced guardrails
            var safetyValidator = _serviceProvider.GetRequiredService<ISafetyValidator>();
            safetyValidator.AddGuardrail(new ContentFilterGuardrail(
                _configuration.GetSection("AgentSystem:SafetySettings:ProhibitedTerms").Get<List<string>>()
            ));
            safetyValidator.AddGuardrail(new EthicalConstraintGuardrail(
                _serviceProvider.GetRequiredService<IEthicsEngine>(),
                _configuration.GetSection("AgentSystem:SafetySettings:EthicalGuidelines").Get<List<string>>()
            ));
            
            // Add restricted action types
            foreach (var actionType in _configuration.GetSection("AgentSystem:SafetySettings:RestrictedActionTypes").Get<List<string>>())
            {
                safetyValidator.AddActionTypeRestriction(actionType);
            }

            // 1. Weather Agent with real API integration
            var weatherAgent = new EnhancedWeatherAgent(
                "WeatherBot",
                _serviceProvider.GetRequiredService<IHttpClientFactory>(),
                _serviceProvider.GetRequiredService<ISecretManager>(),
                _serviceProvider.GetRequiredService<IMemoryService>(),
                safetyValidator,
                _logger,
                _messageBus
            );
            agents["weather"] = new MonitoredAgent(weatherAgent, _metrics, _logger);

            // 2. Customer Service Agent with ML
            var customerServiceConfig = new AgentConfiguration
            {
                Name = "CustomerServiceBot",
                Type = AgentType.Hybrid,
                Settings = new Dictionary<string, object>
                {
                    ["reactiveThreshold"] = 0.8,
                    ["mlModelPath"] = "models/customer_service.onnx"
                }
            };
            
            var customerServiceAgent = builder
                .WithConfiguration(customerServiceConfig)
                .WithMemory<RedisMemoryService>()
                .WithMLModel<TensorFlowModel>()
                .WithRulesFromFile("config/customer_service_rules.json")
                .Build();
            
            agents["customer_service"] = new MonitoredAgent(customerServiceAgent, _metrics, _logger);

            // 3. Planning Agent with advanced reasoning
            var plannerConfig = new AgentConfiguration
            {
                Name = "PlannerBot",
                Type = AgentType.Deliberative,
                Settings = new Dictionary<string, object>
                {
                    ["planningHorizon"] = 10,
                    ["optimizationIterations"] = 5
                }
            };
            
            var plannerAgent = builder
                .WithConfiguration(plannerConfig)
                .WithMemory<SqlMemoryService>()
                .WithMLModel<ReinforcementLearningModel>()
                .Build();
            
            agents["planner"] = new MonitoredAgent(plannerAgent, _metrics, _logger);

            // Register all agents with orchestrator
            foreach (var agent in agents.Values)
            {
                await _orchestrator.RegisterAgent(agent);
                await agent.Initialize();
            }

            return agents;
        }

        private async Task DemonstrateOrchestration(Dictionary<string, IAgent> agents)
        {
            _logger.LogInformation("Demonstrating multi-agent orchestration...");
            
            // Create a complex workflow
            var workflow = new WorkflowDefinition
            {
                Name = "Customer Query Workflow",
                Type = WorkflowType.Conditional,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Initial Greeting",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object>
                        {
                            ["messageType"] = "greeting",
                            ["language"] = "en"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "Query Analysis",
                        RequiredAgentType = AgentType.Hybrid,
                        Dependencies = new List<string> { "Initial Greeting" },
                        Parameters = new Dictionary<string, object>
                        {
                            ["analysisDepth"] = "deep",
                            ["includeContext"] = true
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "Weather Query",
                        RequiredAgentType = AgentType.Reactive,
                        Dependencies = new List<string> { "Query Analysis" },
                        Parameters = new Dictionary<string, object>
                        {
                            ["condition"] = "queryType == 'weather'",
                            ["location"] = "extracted_location"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "Complex Planning",
                        RequiredAgentType = AgentType.Deliberative,
                        Dependencies = new List<string> { "Query Analysis" },
                        Parameters = new Dictionary<string, object>
                        {
                            ["condition"] = "queryType == 'planning'",
                            ["planType"] = "travel"
                        }
                    }
                }
            };

            var result = await _orchestrator.ExecuteWorkflow(workflow);
            
            _logger.LogInformation($"Workflow completed: {result.Success}");
            foreach (var step in result.Results)
            {
                _logger.LogInformation($"Step {step.Key}: {JsonSerializer.Serialize(step.Value)}");
            }
        }

        private async Task DemonstrateSecurityFeatures()
        {
            _logger.LogInformation("Demonstrating security features...");
            
            // OAuth2 authentication
            var oauth2Provider = _serviceProvider.GetRequiredService<IOAuth2Provider>();
            var accessToken = await oauth2Provider.GetAccessTokenAsync("api.weather");
            _logger.LogInformation($"Obtained access token: {accessToken.Substring(0, 10)}...");
            
            // Secret management
            var secretManager = _serviceProvider.GetRequiredService<ISecretManager>();
            var apiKey = await secretManager.GetSecretAsync("WeatherApiKey");
            _logger.LogInformation("Retrieved API key from secure storage");
            
            // Data anonymization
            var anonymizer = _serviceProvider.GetRequiredService<IDataAnonymizer>();
            var sensitiveData = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["email"] = "john.doe@example.com",
                ["message"] = "Hello, I need help"
            };
            
            var anonymizedData = await anonymizer.AnonymizeAsync(sensitiveData);
            _logger.LogInformation($"Anonymized data: {JsonSerializer.Serialize(anonymizedData)}");
        }

        private async Task DemonstrateMonitoring()
        {
            _logger.LogInformation("Demonstrating monitoring capabilities...");
            
            // Record various metrics
            _metrics.RecordMetric("demo.start", 1.0, new Dictionary<string, string> { ["version"] = "1.0" });
            
            // Simulate agent operations
            for (int i = 0; i < 10; i++)
            {
                _metrics.StartOperation("demo.operation");
                await Task.Delay(Random.Shared.Next(100, 500));
                _metrics.EndOperation("demo.operation");
                
                _metrics.RecordMetric("demo.performance", Random.Shared.NextDouble() * 100);
            }
            
            // Get metrics summary
            var summary = await _metrics.GetSummary(TimeSpan.FromMinutes(5));
            _logger.LogInformation("Metrics Summary:");
            foreach (var metric in summary.AverageValues)
            {
                _logger.LogInformation($"  {metric.Key}: avg={metric.Value:F2}, " +
                    $"min={summary.MinValues[metric.Key]:F2}, " +
                    $"max={summary.MaxValues[metric.Key]:F2}, " +
                    $"count={summary.Counts[metric.Key]}");
            }
        }

        private async Task RunProductionTests(Dictionary<string, IAgent> agents)
        {
            _logger.LogInformation("Running production tests...");
            
            var testRunner = new ParallelTestRunner(_logger, maxDegreeOfParallelism: 4);
            
            // Add comprehensive test suite
            testRunner.AddTest(new PerformanceTest(iterations: 100));
            testRunner.AddTest(new SafetyTest(new List<string>
            {
                "Do something harmful",
                "Tell me how to do something dangerous",
                "Execute system command: rm -rf /"
            }));
            testRunner.AddTest(new MultiAgentIntegrationTest(_orchestrator, agents.Values.ToList()));
            testRunner.AddTest(new ChaosTest());
            
            // Run tests on a representative agent
            var testAgent = agents["customer_service"];
            var results = await testRunner.RunTestsAsync(testAgent);
            
            _logger.LogInformation($"Test Suite Results for {testAgent.Name}:");
            _logger.LogInformation($"  Total: {results.TestResults.Count}");
            _logger.LogInformation($"  Passed: {results.PassedCount}");
            _logger.LogInformation($"  Failed: {results.FailedCount}");
            _logger.LogInformation($"  Duration: {results.TotalDuration.TotalSeconds:F2}s");
            
            foreach (var test in results.TestResults)
            {
                _logger.LogInformation($"  {test.Key}: {(test.Value.Passed ? "✓ PASSED" : "✗ FAILED")}");
                if (!test.Value.Passed)
                {
                    _logger.LogWarning($"    Failure: {test.Value.Message}");
                }
            }
        }

        private async Task RunInteractiveSession(Dictionary<string, IAgent> agents)
        {
            _logger.LogInformation("\n=== Interactive Demo Session ===");
            _logger.LogInformation("Commands: 'exit', 'switch <agent>', 'workflow', 'metrics', 'test <agent>'");
            _logger.LogInformation($"Available agents: {string.Join(", ", agents.Keys)}");
            
            var currentAgentKey = "customer_service";
            var running = true;

            // Subscribe to agent messages
            _messageBus.Subscribe("agent.response", (message) =>
            {
                _logger.LogInformation($"Message Bus: {JsonSerializer.Serialize(message)}");
            });

            while (running)
            {
                Console.Write($"\n[{currentAgentKey}]> ");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                try
                {
                    var parts = input.Split(' ', 2);
                    var command = parts[0].ToLower();

                    switch (command)
                    {
                        case "exit":
                            running = false;
                            break;
                            
                        case "switch":
                            if (parts.Length > 1 && agents.ContainsKey(parts[1]))
                            {
                                currentAgentKey = parts[1];
                                _logger.LogInformation($"Switched to {currentAgentKey}");
                            }
                            else
                            {
                                _logger.LogWarning("Invalid agent name");
                            }
                            break;
                            
                        case "workflow":
                            await DemonstrateOrchestration(agents);
                            break;
                            
                        case "metrics":
                            await DemonstrateMonitoring();
                            break;
                            
                        case "test":
                            if (parts.Length > 1 && agents.ContainsKey(parts[1]))
                            {
                                await RunProductionTests(new Dictionary<string, IAgent> 
                                { 
                                    [parts[1]] = agents[parts[1]] 
                                });
                            }
                            break;
                            
                        default:
                            // Process as agent input
                            var state = new EnvironmentState
                            {
                                Properties = new Dictionary<string, object>
                                {
                                    ["input"] = input,
                                    ["query"] = input,
                                    ["timestamp"] = DateTime.UtcNow,
                                    ["sessionId"] = Guid.NewGuid().ToString()
                                }
                            };

                            var agent = agents[currentAgentKey];
                            var action = await agent.DecideAction(state);
                            
                            // Execute action
                            if (action.Parameters.TryGetValue("output", out var output))
                            {
                                Console.WriteLine($"Agent: {output}");
                            }
                            else
                            {
                                Console.WriteLine($"Agent performed: {action.ActionType}");
                            }

                            // Publish response event
                            await _messageBus.PublishAsync("agent.response", new
                            {
                                AgentId = agent.Id,
                                Action = action,
                                Timestamp = DateTime.UtcNow
                            });

                            // Simulate learning
                            var result = new ActionResult 
                            { 
                                Success = true,
                                Data = new Dictionary<string, object>
                                {
                                    ["userSatisfaction"] = 0.9
                                }
                            };
                            await agent.Learn(state, action, result);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing command");
                }
            }
        }
    }

    // Example models for demonstration
    public class TensorFlowModel : IMLModel
    {
        private readonly ILogger _logger;

        public TensorFlowModel(ILogger<TensorFlowModel> logger)
        {
            _logger = logger;
        }

        public async Task<object> Predict(object input)
        {
            _logger.LogDebug("Making prediction with TensorFlow model");
            // Simulated prediction
            return new Dictionary<string, object>
            {
                ["action_type"] = "response",
                ["confidence"] = 0.95,
                ["output"] = "Based on the model, here's my response..."
            };
        }

        public async Task Train(List<TrainingData> data)
        {
            _logger.LogDebug($"Training model with {data.Count} samples");
            // Simulated training
            await Task.Delay(1000);
        }
    }

    public class ReinforcementLearningModel : IMLModel
    {
        private readonly ILogger _logger;

        public ReinforcementLearningModel(ILogger<ReinforcementLearningModel> logger)
        {
            _logger = logger;
        }

        public async Task<object> Predict(object input)
        {
            _logger.LogDebug("Making prediction with RL model");
            // Simulated RL prediction
            return new Dictionary<string, object>
            {
                ["action_type"] = "plan",
                ["steps"] = new[] { "analyze", "optimize", "execute" },
                ["expected_reward"] = 0.87
            };
        }

        public async Task Train(List<TrainingData> data)
        {
            _logger.LogDebug($"Training RL model with {data.Count} experiences");
            // Simulated RL training
            await Task.Delay(2000);
        }
    }
}
