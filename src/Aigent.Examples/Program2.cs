using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;
using Aigent.Orchestration;
using Aigent.Configuration;

namespace Aigent.Examples
{
    /// <summary>
    /// Example program demonstrating the Aigent system
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the example program
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Aigent - Generic Agential System Example");
            Console.WriteLine("----------------------------------------");
            
            // Set up dependency injection
            var serviceProvider = ConfigureServices();
            
            // Create agents
            var agents = await CreateAgents(serviceProvider);
            
            // Run example workflow
            await RunExampleWorkflow(serviceProvider, agents);
            
            // Clean up
            foreach (var agent in agents.Values)
            {
                await agent.Shutdown();
            }
            
            Console.WriteLine("Example completed. Press any key to exit.");
            Console.ReadKey();
        }

        private static ServiceProvider ConfigureServices()
        {
            // Create configuration
            var configuration = new JsonConfiguration("appsettings.json");
            
            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Add Aigent services
            services.AddAigent(configuration);
            
            return services.BuildServiceProvider();
        }

        private static async Task<Dictionary<string, IAgent>> CreateAgents(ServiceProvider serviceProvider)
        {
            var agents = new Dictionary<string, IAgent>();
            var builder = serviceProvider.GetRequiredService<IAgentBuilder>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            
            logger.Log(LogLevel.Information, "Creating agents...");
            
            // 1. Reactive Agent
            var reactiveConfig = new AgentConfiguration
            {
                Name = "ReactiveBot",
                Type = AgentType.Reactive,
                Settings = new Dictionary<string, object>()
            };
            
            var reactiveAgent = builder
                .WithConfiguration(reactiveConfig)
                .WithMemory<ConcurrentMemoryService>()
                .Build();
            
            await reactiveAgent.Initialize();
            agents["reactive"] = reactiveAgent;
            
            // 2. Deliberative Agent
            var deliberativeConfig = new AgentConfiguration
            {
                Name = "DeliberativeBot",
                Type = AgentType.Deliberative,
                Settings = new Dictionary<string, object>()
            };
            
            var deliberativeAgent = builder
                .WithConfiguration(deliberativeConfig)
                .WithMemory<ConcurrentMemoryService>()
                .Build();
            
            await deliberativeAgent.Initialize();
            agents["deliberative"] = deliberativeAgent;
            
            // 3. Hybrid Agent
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
                .WithMemory<ConcurrentMemoryService>()
                .Build();
            
            await hybridAgent.Initialize();
            agents["hybrid"] = hybridAgent;
            
            logger.Log(LogLevel.Information, $"Created {agents.Count} agents");
            
            return agents;
        }

        private static async Task RunExampleWorkflow(ServiceProvider serviceProvider, Dictionary<string, IAgent> agents)
        {
            var orchestrator = serviceProvider.GetRequiredService<IOrchestrator>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            
            // Register agents with orchestrator
            foreach (var agent in agents.Values)
            {
                await orchestrator.RegisterAgent(agent);
            }
            
            logger.Log(LogLevel.Information, "Running example workflow...");
            
            // Create a workflow
            var workflow = new WorkflowDefinition
            {
                Name = "ExampleWorkflow",
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
                    },
                    new WorkflowStep
                    {
                        Name = "UrgentStep",
                        RequiredAgentType = AgentType.Hybrid,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "This is urgent!",
                            ["deadline"] = DateTime.UtcNow.AddMinutes(1)
                        },
                        Dependencies = new List<string> { "PlanningStep" }
                    }
                }
            };
            
            // Execute the workflow
            var result = await orchestrator.ExecuteWorkflow(workflow);
            
            // Display results
            logger.Log(LogLevel.Information, $"Workflow completed. Success: {result.Success}");
            
            foreach (var stepResult in result.Results)
            {
                logger.Log(LogLevel.Information, $"Step: {stepResult.Key}");
                if (stepResult.Value is ActionResult actionResult)
                {
                    logger.Log(LogLevel.Information, $"  Success: {actionResult.Success}");
                    logger.Log(LogLevel.Information, $"  Message: {actionResult.Message}");
                    
                    if (actionResult.Data.TryGetValue("text", out var text))
                    {
                        logger.Log(LogLevel.Information, $"  Text: {text}");
                    }
                }
            }
            
            // Unregister agents
            foreach (var agent in agents.Values)
            {
                await orchestrator.UnregisterAgent(agent.Id);
            }
        }
    }

    /// <summary>
    /// Simple JSON configuration implementation
    /// </summary>
    public class JsonConfiguration : IConfiguration
    {
        private readonly Dictionary<string, object> _configuration = new();

        /// <summary>
        /// Initializes a new instance of the JsonConfiguration class
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        public JsonConfiguration(string filePath)
        {
            // In a real implementation, this would load from a JSON file
            // For this example, we'll use hardcoded values
            
            _configuration["Aigent:MemoryType"] = "InMemory";
            
            _configuration["Aigent:SafetySettings:ContentFilterEnabled"] = true;
            _configuration["Aigent:SafetySettings:ProhibitedTerms"] = new[] { "harmful", "dangerous", "illegal" };
            _configuration["Aigent:SafetySettings:EthicalGuidelines"] = new[] { "Be helpful", "Be honest", "Be harmless" };
            _configuration["Aigent:SafetySettings:RestrictedActionTypes"] = new[] { "DeleteFile", "ModifySystemSettings" };
            
            _configuration["Agents:ReactiveBot:Type"] = "Reactive";
            _configuration["Agents:ReactiveBot:Rules:GreetingRule:Condition"] = "input.Contains('Hello')";
            _configuration["Agents:ReactiveBot:Rules:GreetingRule:Action:Type"] = "TextOutput";
            _configuration["Agents:ReactiveBot:Rules:GreetingRule:Action:Parameters:text"] = "Hello! How can I help you?";
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public string this[string key] => _configuration.TryGetValue(key, out var value) ? value?.ToString() : null;

        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        public IConfigurationSection GetSection(string key)
        {
            return new JsonConfigurationSection(this, key);
        }
    }

    /// <summary>
    /// Simple JSON configuration section implementation
    /// </summary>
    public class JsonConfigurationSection : IConfigurationSection
    {
        private readonly IConfiguration _configuration;
        
        /// <summary>
        /// Key of the section
        /// </summary>
        public string Key { get; }
        
        /// <summary>
        /// Path of the section
        /// </summary>
        public string Path { get; }
        
        /// <summary>
        /// Value of the section
        /// </summary>
        public string Value => _configuration[Path];

        /// <summary>
        /// Initializes a new instance of the JsonConfigurationSection class
        /// </summary>
        /// <param name="configuration">Parent configuration</param>
        /// <param name="path">Path of the section</param>
        public JsonConfigurationSection(IConfiguration configuration, string path)
        {
            _configuration = configuration;
            Path = path;
            Key = path.Contains(':') ? path.Substring(path.LastIndexOf(':') + 1) : path;
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public string this[string key] => _configuration[$"{Path}:{key}"];

        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        public IConfigurationSection GetSection(string key)
        {
            return new JsonConfigurationSection(_configuration, $"{Path}:{key}");
        }

        /// <summary>
        /// Gets the children of the section
        /// </summary>
        /// <returns>The children of the section</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            // In a real implementation, this would return actual children
            return new List<IConfigurationSection>();
        }

        /// <summary>
        /// Gets a typed value from the section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns>The typed value</returns>
        public T Get<T>()
        {
            // In a real implementation, this would deserialize the value
            return default;
        }
    }
}
