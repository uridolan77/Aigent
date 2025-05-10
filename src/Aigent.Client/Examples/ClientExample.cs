using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Core;
using Aigent.Api.Models;

namespace Aigent.Client.Examples
{
    /// <summary>
    /// Example of using the Aigent client
    /// </summary>
    public class ClientExample
    {
        /// <summary>
        /// Runs the example
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Aigent Client Example");
            Console.WriteLine("--------------------");
            
            // Create client
            using var client = new AigentClient("https://localhost:5001");
            
            // Authenticate
            Console.WriteLine("Authenticating...");
            var authResult = await client.AuthenticateAsync("admin", "admin123");
            
            if (!authResult.Success)
            {
                Console.WriteLine($"Authentication failed: {authResult.Message}");
                return;
            }
            
            Console.WriteLine($"Authenticated as {authResult.User.Username}");
            
            // Create an agent
            Console.WriteLine("\nCreating agent...");
            var createAgentRequest = new CreateAgentRequest
            {
                Name = "ExampleAgent",
                Type = AgentType.Reactive,
                MemoryServiceType = "DocumentDb",
                Settings = new Dictionary<string, object>
                {
                    ["reactiveThreshold"] = 0.7
                }
            };
            
            var agent = await client.CreateAgentAsync(createAgentRequest);
            Console.WriteLine($"Created agent: {agent.Name} ({agent.Id})");
            
            // Subscribe to agent events
            Console.WriteLine("\nSubscribing to agent events...");
            var subscribed = await client.SubscribeToAgentEventsAsync(
                agent.Id,
                (agentId, status) => Console.WriteLine($"Agent {agentId} status changed to {status}"),
                (agentId, action, result) => Console.WriteLine($"Agent {agentId} performed action {action} with result: {result.Success}")
            );
            
            if (subscribed)
            {
                Console.WriteLine("Subscribed to agent events");
            }
            else
            {
                Console.WriteLine("Failed to subscribe to agent events");
            }
            
            // Perform an action
            Console.WriteLine("\nPerforming action...");
            var actionResult = await client.PerformActionAsync(
                agent.Id,
                "Hello, how can you help me?",
                new Dictionary<string, object>
                {
                    ["urgent"] = false
                }
            );
            
            Console.WriteLine($"Action type: {actionResult.ActionType}");
            Console.WriteLine($"Success: {actionResult.Result.Success}");
            Console.WriteLine($"Message: {actionResult.Result.Message}");
            
            if (actionResult.Result.Data.TryGetValue("text", out var text))
            {
                Console.WriteLine($"Text: {text}");
            }
            
            // Execute a workflow
            Console.WriteLine("\nExecuting workflow...");
            var workflowRequest = new CreateWorkflowRequest
            {
                Name = "ExampleWorkflow",
                Type = "Sequential",
                Steps = new List<WorkflowStepDto>
                {
                    new WorkflowStepDto
                    {
                        Name = "GreetingStep",
                        RequiredAgentType = AgentType.Reactive,
                        Parameters = new Dictionary<string, object>
                        {
                            ["input"] = "Hello"
                        }
                    },
                    new WorkflowStepDto
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
            
            var workflowResult = await client.ExecuteWorkflowAsync(workflowRequest);
            Console.WriteLine($"Workflow success: {workflowResult.Success}");
            
            foreach (var step in workflowResult.Results)
            {
                Console.WriteLine($"Step: {step.Key}");
            }
            
            // Get all agents with pagination and filtering
            Console.WriteLine("\nGetting agents...");
            var agents = await client.GetAgentsAsync(
                name: "Example",
                page: 1,
                pageSize: 10,
                sortBy: "name",
                sortDirection: "asc"
            );
            
            Console.WriteLine($"Found {agents.Count} agents");
            foreach (var a in agents)
            {
                Console.WriteLine($"- {a.Name} ({a.Id})");
            }
            
            // Delete the agent
            Console.WriteLine("\nDeleting agent...");
            var deleted = await client.DeleteAgentAsync(agent.Id);
            Console.WriteLine($"Agent deleted: {deleted}");
            
            // Unsubscribe from agent events
            Console.WriteLine("\nUnsubscribing from agent events...");
            var unsubscribed = await client.UnsubscribeFromAgentEventsAsync(agent.Id);
            Console.WriteLine($"Unsubscribed from agent events: {unsubscribed}");
            
            Console.WriteLine("\nExample completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
