using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Configuration;
using Aigent.Monitoring;

namespace Aigent.Examples
{
    /// <summary>
    /// Example demonstrating advanced agent types
    /// </summary>
    public static class AdvancedAgentsExample
    {
        /// <summary>
        /// Runs the example
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Run(string[] args)
        {
            Console.WriteLine("Advanced Agent Types Example");
            Console.WriteLine("===========================");

            // Set up dependency injection
            var services = new ServiceCollection();

            // Add logging
            services.AddSingleton<ILogger, ConsoleLogger>();

            // Add Aigent services
            var configuration = new JsonConfiguration("appsettings.json");
            services.AddAigent(configuration);

            var serviceProvider = services.BuildServiceProvider();

            // Get the agent builder
            var builder = serviceProvider.GetRequiredService<IAgentBuilder>();
            var logger = serviceProvider.GetRequiredService<ILogger>();

            // Create a hybrid agent
            logger.Log(LogLevel.Information, "Creating hybrid agent...");
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
                .WithMemory<LazyCacheMemoryService>()
                .Build();

            await hybridAgent.Initialize();

            // Create a BDI agent
            logger.Log(LogLevel.Information, "Creating BDI agent...");
            var bdiConfig = new AgentConfiguration
            {
                Name = "BDIBot",
                Type = AgentType.BDI
            };

            var bdiAgent = builder
                .WithConfiguration(bdiConfig)
                .WithMemory<LazyCacheMemoryService>()
                .Build();

            await bdiAgent.Initialize();

            // Create a utility-based agent
            logger.Log(LogLevel.Information, "Creating utility-based agent...");
            var utilityConfig = new AgentConfiguration
            {
                Name = "UtilityBot",
                Type = AgentType.UtilityBased
            };

            var utilityAgent = builder
                .WithConfiguration(utilityConfig)
                .WithMemory<LazyCacheMemoryService>()
                .Build();

            await utilityAgent.Initialize();

            // Test the hybrid agent
            Console.WriteLine("\nTesting Hybrid Agent");
            Console.WriteLine("-------------------");
            await TestAgent(hybridAgent, "Hello, how are you?");
            await TestAgent(hybridAgent, "I need help with planning my day");

            // Test the BDI agent
            Console.WriteLine("\nTesting BDI Agent");
            Console.WriteLine("----------------");
            await TestAgent(bdiAgent, "Hello, how are you?");
            await TestAgent(bdiAgent, "I need help with planning my day");

            // Test the utility-based agent
            Console.WriteLine("\nTesting Utility-Based Agent");
            Console.WriteLine("--------------------------");
            await TestAgent(utilityAgent, "Hello, how are you?");
            await TestAgent(utilityAgent, "I need help with planning my day");

            // Clean up
            await hybridAgent.Shutdown();
            await bdiAgent.Shutdown();
            await utilityAgent.Shutdown();

            Console.WriteLine("\nExample completed.");
        }

        private static async Task TestAgent(IAgent agent, string input)
        {
            Console.WriteLine($"\nInput: {input}");

            var state = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = input
                },
                Timestamp = DateTime.UtcNow
            };

            var action = await agent.DecideAction(state);
            Console.WriteLine($"Action: {action.ActionType}");

            var result = await action.Execute();
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Message: {result.Message}");

            if (result.Data.TryGetValue("text", out var text))
            {
                Console.WriteLine($"Text: {text}");
            }

            // Learn from the result
            await agent.Learn(state, action, result);
        }
    }
}
