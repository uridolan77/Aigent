using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aigent.Configuration;
using Aigent.Examples;

namespace Aigent.Examples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Aigent Examples");
            Console.WriteLine("==============");

            if (args.Length == 0)
            {
                Console.WriteLine("Please specify an example to run:");
                Console.WriteLine("  basic        - Basic agent example");
                Console.WriteLine("  advanced     - Advanced agent types example");
                Console.WriteLine("  workflow     - Workflow orchestration example");
                Console.WriteLine("  client       - Client SDK example");
                return;
            }

            var exampleName = args[0].ToLowerInvariant();

            try
            {
                switch (exampleName)
                {
                    case "basic":
                        await RunBasicExample();
                        break;
                    case "advanced":
                        await RunAdvancedExample();
                        break;
                    case "workflow":
                        await RunWorkflowExample();
                        break;
                    case "client":
                        await RunClientExample();
                        break;
                    default:
                        Console.WriteLine($"Unknown example: {exampleName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running example: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task RunBasicExample()
        {
            Console.WriteLine("Running Basic Agent Example...");

            // Set up dependency injection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add Aigent services
            var configuration = new JsonConfiguration("appsettings.json");
            services.AddAigent(configuration);

            var serviceProvider = services.BuildServiceProvider();

            // Run the example
            var example = new BasicAgentExample(serviceProvider);
            await example.Run();
        }

        private static async Task RunAdvancedExample()
        {
            Console.WriteLine("Running Advanced Agent Types Example...");

            // This example is implemented in AdvancedAgentsExample.cs
            await AdvancedAgentsExample.Run(Array.Empty<string>());
        }

        private static async Task RunWorkflowExample()
        {
            Console.WriteLine("Running Workflow Orchestration Example...");

            // Set up dependency injection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add Aigent services
            var configuration = new JsonConfiguration("appsettings.json");
            services.AddAigent(configuration);

            var serviceProvider = services.BuildServiceProvider();

            // Run the example
            var example = new WorkflowExample(serviceProvider);
            await example.Run();
        }

        private static async Task RunClientExample()
        {
            Console.WriteLine("Running Client SDK Example...");

            // This example is implemented in Client/Examples/ClientExample.cs
            await Aigent.Client.Examples.ClientExample.Run(Array.Empty<string>());
        }
    }
}
