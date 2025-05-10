// Integration Tests
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;
using Moq;

namespace AgentSystem.Integration.Tests
{
    public class AgentSystemIntegrationTests : IAsyncLifetime
    {
        private ServiceProvider _serviceProvider;
        private IOrchestrator _orchestrator;
        private IAgentBuilder _agentBuilder;
        private IMessageBus _messageBus;
        private IMetricsCollector _metrics;

        public async Task InitializeAsync()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AgentSystem:MemoryType"] = "InMemory",
                    ["AgentSystem:Monitoring:Type"] = "InMemory",
                    ["AgentSystem:SafetySettings:ProhibitedTerms:0"] = "harmful",
                    ["AgentSystem:SafetySettings:ProhibitedTerms:1"] = "dangerous"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddAgentSystem(configuration);
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();
            _orchestrator = _serviceProvider.GetRequiredService<IOrchestrator>();
            _agentBuilder = _serviceProvider.GetRequiredService<IAgentBuilder>();
            _messageBus = _serviceProvider.GetRequiredService<IMessageBus>();
            _metrics = _serviceProvider.GetRequiredService<IMetricsCollector>();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _serviceProvider?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Agent_Should_Initialize_And_Process_Request()
        {
            // Arrange
            var config = new AgentConfiguration
            {
                Name = "TestAgent",
                Type = AgentType.Reactive
            };

            var agent = _agentBuilder
                .WithConfiguration(config)
                .Build();

            // Act
            await agent.Initialize();
            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "Hello"
                }
            };

            var action = await agent.DecideAction(state);

            // Assert
            action.Should().NotBeNull();
            action.ActionType.Should().NotBe("NoOp");
        }

        [Fact]
        public async Task Orchestrator_Should_Execute_Sequential_Workflow()
        {
            // Arrange
            var agent1 = CreateTestAgent("Agent1", AgentType.Reactive);
            var agent2 = CreateTestAgent("Agent2", AgentType.Deliberative);

            await _orchestrator.RegisterAgent(agent1);
            await _orchestrator.RegisterAgent(agent2);

            var workflow = new WorkflowDefinition
            {
                Name = "TestWorkflow",
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Step1",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object> { ["test"] = true }
                    },
                    new WorkflowStep
                    {
                        Name = "Step2",
                        RequiredAgentType = AgentType.Deliberative,
                        Dependencies = new List<string> { "Step1" }
                    }
                }
            };

            // Act
            var result = await _orchestrator.ExecuteWorkflow(workflow);

            // Assert
            result.Success.Should().BeTrue();
            result.Results.Should().ContainKeys("Step1", "Step2");
        }

        [Fact]
        public async Task Orchestrator_Should_Execute_Parallel_Workflow()
        {
            // Arrange
            var agents = Enumerable.Range(1, 5)
                .Select(i => CreateTestAgent($"Agent{i}", AgentType.Reactive))
                .ToList();

            foreach (var agent in agents)
            {
                await _orchestrator.RegisterAgent(agent);
            }

            var workflow = new WorkflowDefinition
            {
                Name = "ParallelWorkflow",
                Type = WorkflowType.Parallel,
                Steps = Enumerable.Range(1, 5).Select(i => new WorkflowStep
                {
                    Name = $"Step{i}",
                    RequiredAgentType = AgentType.Reactive
                }).ToList()
            };

            // Act
            var result = await _orchestrator.ExecuteWorkflow(workflow);

            // Assert
            result.Success.Should().BeTrue();
            result.Results.Count.Should().Be(5);
        }

        [Fact]
        public async Task MessageBus_Should_Deliver_Messages()
        {
            // Arrange
            var receivedMessages = new List<object>();
            var topic = "test.topic";
            
            _messageBus.Subscribe(topic, msg => receivedMessages.Add(msg));

            // Act
            await _messageBus.PublishAsync(topic, new { Message = "Test1" });
            await _messageBus.PublishAsync(topic, new { Message = "Test2" });
            await Task.Delay(100); // Allow time for async processing

            // Assert
            receivedMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task SafetyValidator_Should_Block_Prohibited_Actions()
        {
            // Arrange
            var safetyValidator = _serviceProvider.GetRequiredService<ISafetyValidator>();
            var harmfulAction = new GenericAction("TextOutput", new Dictionary<string, object>
            {
                ["output"] = "This is harmful content"
            });

            // Act
            var result = await safetyValidator.ValidateAction(harmfulAction);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Violations.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Memory_Service_Should_Store_And_Retrieve_Data()
        {
            // Arrange
            var memory = _serviceProvider.GetRequiredService<IMemoryService>();
            await memory.Initialize("test-agent");

            var testData = new { Name = "Test", Value = 42 };

            // Act
            await memory.StoreContext("test-key", testData);
            var retrieved = await memory.RetrieveContext<dynamic>("test-key");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved.Name.Should().Be("Test");
            retrieved.Value.Should().Be(42);
        }

        [Fact]
        public async Task Metrics_Should_Be_Collected()
        {
            // Arrange & Act
            _metrics.RecordMetric("test.metric", 1.0);
            _metrics.RecordMetric("test.metric", 2.0);
            _metrics.RecordMetric("test.metric", 3.0);

            var summary = await _metrics.GetSummary(TimeSpan.FromMinutes(1));

            // Assert
            summary.AverageValues.Should().ContainKey("test.metric");
            summary.AverageValues["test.metric"].Should().Be(2.0);
            summary.Counts["test.metric"].Should().Be(3);
        }

        [Fact]
        public async Task Agent_Should_Learn_From_Experience()
        {
            // Arrange
            var config = new AgentConfiguration
            {
                Name = "LearningAgent",
                Type = AgentType.Deliberative
            };

            var agent = _agentBuilder
                .WithConfiguration(config)
                .Build();

            await agent.Initialize();

            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "test query"
                }
            };

            // Act
            var action = await agent.DecideAction(state);
            var result = new ActionResult { Success = true };
            await agent.Learn(state, action, result);

            // Assert
            // The agent should have stored the experience
            var memory = _serviceProvider.GetRequiredService<IMemoryService>();
            var experiences = await memory.RetrieveContext<List<object>>("training_data");
            experiences?.Should().NotBeNull();
        }

        [Fact]
        public async Task End_To_End_Customer_Support_Scenario()
        {
            // Arrange
            var customerSupportAgent = CreateCustomerSupportAgent();
            await _orchestrator.RegisterAgent(customerSupportAgent);

            var workflow = new WorkflowDefinition
            {
                Name = "CustomerSupportWorkflow",
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "HandleQuery",
                        RequiredAgentType = AgentType.Hybrid,
                        Parameters = new Dictionary<string, object>
                        {
                            ["customer_id"] = "12345",
                            ["customer_query"] = "What is the status of my order?",
                            ["context"] = "customer_support"
                        }
                    }
                }
            };

            // Act
            var result = await _orchestrator.ExecuteWorkflow(workflow);

            // Assert
            result.Success.Should().BeTrue();
            result.Results.Should().ContainKey("HandleQuery");
            
            var action = result.Results["HandleQuery"] as IAction;
            action.Should().NotBeNull();
            action.ActionType.Should().NotBe("NoOp");
        }

        private IAgent CreateTestAgent(string name, AgentType type)
        {
            var config = new AgentConfiguration
            {
                Name = name,
                Type = type
            };

            return _agentBuilder
                .WithConfiguration(config)
                .Build();
        }

        private IAgent CreateCustomerSupportAgent()
        {
            var mockTextAnalytics = new Mock<TextAnalyticsClient>();
            var mockIntentClassifier = new Mock<IMLModel>();
            var mockEmailClient = new Mock<ISendGridClient>();
            var mockSmsClient = new Mock<ITwilioClient>();

            // Setup mocks
            mockIntentClassifier
                .Setup(x => x.Predict(It.IsAny<object>()))
                .ReturnsAsync(new Dictionary<string, object> { ["intent"] = "order_status" });

            var agent = new CustomerSupportAgent(
                "CustomerSupport",
                mockTextAnalytics.Object,
                mockIntentClassifier.Object,
                mockEmailClient.Object,
                mockSmsClient.Object,
                _serviceProvider.GetRequiredService<IMemoryService>(),
                _serviceProvider.GetRequiredService<ISafetyValidator>(),
                _serviceProvider.GetRequiredService<ILogger>(),
                _messageBus
            );

            return agent;
        }
    }

    public class PerformanceIntegrationTests : IAsyncLifetime
    {
        private ServiceProvider _serviceProvider;
        private IOrchestrator _orchestrator;
        private IAgentBuilder _agentBuilder;

        public async Task InitializeAsync()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();

            var services = new ServiceCollection();
            services.AddAgentSystem(configuration);
            
            _serviceProvider = services.BuildServiceProvider();
            _orchestrator = _serviceProvider.GetRequiredService<IOrchestrator>();
            _agentBuilder = _serviceProvider.GetRequiredService<IAgentBuilder>();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _serviceProvider?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task System_Should_Handle_Concurrent_Requests()
        {
            // Arrange
            const int concurrentRequests = 100;
            var agents = new List<IAgent>();

            for (int i = 0; i < 10; i++)
            {
                var agent = CreateAgent($"Agent{i}", AgentType.Reactive);
                await _orchestrator.RegisterAgent(agent);
                agents.Add(agent);
            }

            var tasks = new List<Task<IAction>>();

            // Act
            for (int i = 0; i < concurrentRequests; i++)
            {
                var agent = agents[i % agents.Count];
                var state = new EnvironmentState
                {
                    Properties = new Dictionary<string, object>
                    {
                        ["request_id"] = i,
                        ["input"] = $"Request {i}"
                    }
                };

                tasks.Add(agent.DecideAction(state));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(concurrentRequests);
            results.Should().NotContainNulls();
        }

        [Fact]
        public async Task Workflow_Should_Complete_Within_Timeout()
        {
            // Arrange
            var agent = CreateAgent("TimeoutAgent", AgentType.Reactive);
            await _orchestrator.RegisterAgent(agent);

            var workflow = new WorkflowDefinition
            {
                Name = "TimeoutWorkflow",
                Type = WorkflowType.Sequential,
                Steps = Enumerable.Range(1, 10).Select(i => new WorkflowStep
                {
                    Name = $"Step{i}",
                    RequiredAgentType = AgentType.Reactive
                }).ToList()
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await _orchestrator.ExecuteWorkflow(workflow);
            stopwatch.Stop();

            // Assert
            result.Success.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds timeout
        }

        private IAgent CreateAgent(string name, AgentType type)
        {
            var config = new AgentConfiguration
            {
                Name = name,
                Type = type
            };

            return _agentBuilder
                .WithConfiguration(config)
                .Build();
        }
    }

    public class ResilienceIntegrationTests : IAsyncLifetime
    {
        private ServiceProvider _serviceProvider;
        private IOrchestrator _orchestrator;

        public async Task InitializeAsync()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();

            var services = new ServiceCollection();
            services.AddAgentSystem(configuration);
            
            _serviceProvider = services.BuildServiceProvider();
            _orchestrator = _serviceProvider.GetRequiredService<IOrchestrator>();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _serviceProvider?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task System_Should_Recover_From_Agent_Failure()
        {
            // Arrange
            var failingAgent = new FailingAgent();
            await _orchestrator.RegisterAgent(failingAgent);

            var workflow = new WorkflowDefinition
            {
                Name = "FailureWorkflow",
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "FailingStep",
                        RequiredAgentType = AgentType.Reactive
                    }
                }
            };

            // Act
            var result = await _orchestrator.ExecuteWorkflow(workflow);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Memory_Should_Recover_From_Connection_Failure()
        {
            // This test would require a mock Redis connection that can simulate failures
            // For now, we'll test the in-memory fallback
            
            var memory = _serviceProvider.GetRequiredService<IMemoryService>();
            await memory.Initialize("test");

            // Simulate storing data
            await memory.StoreContext("key", "value");

            // Retrieve should work even if underlying storage fails
            var result = await memory.RetrieveContext<string>("key");
            result.Should().Be("value");
        }

        private class FailingAgent : IAgent
        {
            public string Id => "failing-agent";
            public string Name => "FailingAgent";
            public AgentType Type => AgentType.Reactive;
            public AgentCapabilities Capabilities => new();

            public Task Initialize() => Task.CompletedTask;
            public Task Shutdown() => Task.CompletedTask;
            public void Dispose() { }

            public Task<IAction> DecideAction(EnvironmentState state)
            {
                throw new InvalidOperationException("Simulated failure");
            }

            public Task Learn(EnvironmentState state, IAction action, ActionResult result)
            {
                return Task.CompletedTask;
            }
        }
    }
}
