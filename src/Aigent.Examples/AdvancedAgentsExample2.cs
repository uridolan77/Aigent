using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;
using Aigent.Configuration;

namespace Aigent.Examples
{
    /// <summary>
    /// Example program demonstrating advanced agent types
    /// </summary>
    public class AdvancedAgentsExample
    {
        /// <summary>
        /// Entry point for the example program
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Aigent - Advanced Agent Types Example");
            Console.WriteLine("-------------------------------------");
            
            // Set up dependency injection
            var serviceProvider = ConfigureServices();
            
            // Create agents
            var agents = await CreateAgents(serviceProvider);
            
            // Run example scenarios
            await RunBDIAgentScenario(agents["bdi"]);
            await RunUtilityBasedAgentScenario(agents["utility"]);
            
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
            
            // 1. BDI Agent
            var bdiConfig = new AgentConfiguration
            {
                Name = "BDIBot",
                Type = AgentType.BDI,
                Settings = new Dictionary<string, object>()
            };
            
            var bdiAgent = builder
                .WithConfiguration(bdiConfig)
                .WithMemory<ConcurrentMemoryService>()
                .Build();
            
            await bdiAgent.Initialize();
            agents["bdi"] = bdiAgent;
            
            // 2. Utility-Based Agent
            var utilityConfig = new AgentConfiguration
            {
                Name = "UtilityBot",
                Type = AgentType.UtilityBased,
                Settings = new Dictionary<string, object>()
            };
            
            var utilityAgent = builder
                .WithConfiguration(utilityConfig)
                .WithMemory<ConcurrentMemoryService>()
                .Build();
            
            await utilityAgent.Initialize();
            agents["utility"] = utilityAgent;
            
            logger.Log(LogLevel.Information, $"Created {agents.Count} agents");
            
            return agents;
        }

        private static async Task RunBDIAgentScenario(IAgent agent)
        {
            Console.WriteLine("\nBDI Agent Scenario");
            Console.WriteLine("-----------------");
            
            // Scenario 1: Help request
            Console.WriteLine("\nScenario 1: Help request");
            var helpState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "I need help with planning my day"
                }
            };
            
            var helpAction = await agent.DecideAction(helpState);
            var helpResult = await helpAction.Execute();
            
            Console.WriteLine($"Action: {helpAction.ActionType}");
            Console.WriteLine($"Result: {helpResult.Message}");
            if (helpResult.Data.TryGetValue("text", out var helpText))
            {
                Console.WriteLine($"Text: {helpText}");
            }
            
            // Learn from the result
            await agent.Learn(helpState, helpAction, helpResult);
            
            // Scenario 2: Emergency request
            Console.WriteLine("\nScenario 2: Emergency request");
            var emergencyState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "This is an urgent matter that needs immediate attention!"
                }
            };
            
            var emergencyAction = await agent.DecideAction(emergencyState);
            var emergencyResult = await emergencyAction.Execute();
            
            Console.WriteLine($"Action: {emergencyAction.ActionType}");
            Console.WriteLine($"Result: {emergencyResult.Message}");
            if (emergencyResult.Data.TryGetValue("text", out var emergencyText))
            {
                Console.WriteLine($"Text: {emergencyText}");
            }
            
            // Learn from the result
            await agent.Learn(emergencyState, emergencyAction, emergencyResult);
        }

        private static async Task RunUtilityBasedAgentScenario(IAgent agent)
        {
            Console.WriteLine("\nUtility-Based Agent Scenario");
            Console.WriteLine("---------------------------");
            
            // Scenario 1: Simple request
            Console.WriteLine("\nScenario 1: Simple request");
            var simpleState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "What's the weather like today?",
                    ["user_satisfaction"] = 0.5,
                    ["task_completion"] = 0.0,
                    ["efficiency"] = 0.5
                }
            };
            
            var simpleAction = await agent.DecideAction(simpleState);
            var simpleResult = await simpleAction.Execute();
            
            Console.WriteLine($"Action: {simpleAction.ActionType}");
            Console.WriteLine($"Result: {simpleResult.Message}");
            if (simpleResult.Data.TryGetValue("text", out var simpleText))
            {
                Console.WriteLine($"Text: {simpleText}");
            }
            
            // Add satisfaction to the result
            simpleResult.Data["satisfaction"] = 0.8;
            
            // Learn from the result
            await agent.Learn(simpleState, simpleAction, simpleResult);
            
            // Scenario 2: Complex request with high utility
            Console.WriteLine("\nScenario 2: Complex request with high utility");
            var complexState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "I need a comprehensive plan for my project",
                    ["user_satisfaction"] = 0.7,
                    ["task_completion"] = 0.2,
                    ["efficiency"] = 0.6
                }
            };
            
            var complexAction = await agent.DecideAction(complexState);
            var complexResult = await complexAction.Execute();
            
            Console.WriteLine($"Action: {complexAction.ActionType}");
            Console.WriteLine($"Result: {complexResult.Message}");
            if (complexResult.Data.TryGetValue("text", out var complexText))
            {
                Console.WriteLine($"Text: {complexText}");
            }
            
            // Add satisfaction to the result
            complexResult.Data["satisfaction"] = 0.9;
            
            // Learn from the result
            await agent.Learn(complexState, complexAction, complexResult);
            
            // Scenario 3: After learning
            Console.WriteLine("\nScenario 3: After learning");
            var finalState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "Can you help me organize my schedule?",
                    ["user_satisfaction"] = 0.6,
                    ["task_completion"] = 0.1,
                    ["efficiency"] = 0.7
                }
            };
            
            var finalAction = await agent.DecideAction(finalState);
            var finalResult = await finalAction.Execute();
            
            Console.WriteLine($"Action: {finalAction.ActionType}");
            Console.WriteLine($"Result: {finalResult.Message}");
            if (finalResult.Data.TryGetValue("text", out var finalText))
            {
                Console.WriteLine($"Text: {finalText}");
            }
        }
    }
}
