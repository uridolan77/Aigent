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
    /// Example demonstrating basic agent functionality
    /// </summary>
    public class BasicAgentExample
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the BasicAgentExample class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        public BasicAgentExample(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Runs the example
        /// </summary>
        public async Task Run()
        {
            Console.WriteLine("Basic Agent Example");
            Console.WriteLine("------------------");

            // Get the agent builder
            var builder = _serviceProvider.GetRequiredService<IAgentBuilder>();
            var logger = _serviceProvider.GetRequiredService<ILogger>();

            // Create a reactive agent
            logger.Log(LogLevel.Information, "Creating reactive agent...");
            var reactiveConfig = new AgentConfiguration
            {
                Name = "ReactiveBot",
                Type = AgentType.Reactive
            };

            var reactiveAgent = builder
                .WithConfiguration(reactiveConfig)
                .WithMemory<LazyCacheMemoryService>()
                .Build();

            await reactiveAgent.Initialize();

            // Create a deliberative agent
            logger.Log(LogLevel.Information, "Creating deliberative agent...");
            var deliberativeConfig = new AgentConfiguration
            {
                Name = "DeliberativeBot",
                Type = AgentType.Deliberative
            };

            var deliberativeAgent = builder
                .WithConfiguration(deliberativeConfig)
                .WithMemory<LazyCacheMemoryService>()
                .Build();

            await deliberativeAgent.Initialize();

            // Test the reactive agent
            Console.WriteLine("\nTesting Reactive Agent");
            Console.WriteLine("---------------------");
            await TestAgent(reactiveAgent, "Hello, how are you?");
            await TestAgent(reactiveAgent, "I need help with planning my day");

            // Test the deliberative agent
            Console.WriteLine("\nTesting Deliberative Agent");
            Console.WriteLine("-------------------------");
            await TestAgent(deliberativeAgent, "Hello, how are you?");
            await TestAgent(deliberativeAgent, "I need help with planning my day");

            // Clean up
            await reactiveAgent.Shutdown();
            await deliberativeAgent.Shutdown();

            Console.WriteLine("\nExample completed.");
        }

        private async Task TestAgent(IAgent agent, string input)
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
