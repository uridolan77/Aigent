using System;
using System.Threading.Tasks;

namespace Aigent.Client.Examples
{
    /// <summary>
    /// Example demonstrating the client SDK
    /// </summary>
    public static class ClientExample
    {
        /// <summary>
        /// Runs the example
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Run(string[] args)
        {
            Console.WriteLine("Client SDK Example");
            Console.WriteLine("=================");

            // Create a client
            var client = new AigentClient("http://localhost:5000", "api-key");

            // List agents
            Console.WriteLine("\nListing agents...");
            var agents = await client.GetAgentsAsync();

            foreach (var agent in agents)
            {
                Console.WriteLine($"Agent: {agent.Name} ({agent.Id}) - Type: {agent.Type} - Status: {agent.Status}");
            }

            // Create an agent
            Console.WriteLine("\nCreating a new agent...");
            var agentConfig = new AgentConfiguration
            {
                Name = "ClientCreatedAgent",
                Type = "Reactive"
            };

            var newAgent = await client.CreateAgentAsync(agentConfig);
            Console.WriteLine($"Created agent: {newAgent.Name} ({newAgent.Id})");

            // Send a message to the agent
            Console.WriteLine("\nSending a message to the agent...");
            var response = await client.SendMessageAsync(newAgent.Id, "Hello, how are you?");
            Console.WriteLine($"Agent response: {response.Message}");

            // Delete the agent
            Console.WriteLine("\nDeleting the agent...");
            await client.DeleteAgentAsync(newAgent.Id);
            Console.WriteLine("Agent deleted");

            Console.WriteLine("\nExample completed.");
        }
    }

    /// <summary>
    /// Client for the Aigent API
    /// </summary>
    public class AigentClient
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;

        /// <summary>
        /// Initializes a new instance of the AigentClient class
        /// </summary>
        /// <param name="baseUrl">Base URL of the API</param>
        /// <param name="apiKey">API key</param>
        public AigentClient(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        /// <summary>
        /// Gets a list of agents
        /// </summary>
        /// <returns>List of agents</returns>
        public Task<Agent[]> GetAgentsAsync()
        {
            // Placeholder implementation
            return Task.FromResult(new[]
            {
                new Agent { Id = "1", Name = "Agent1", Type = "Reactive", Status = "Ready" },
                new Agent { Id = "2", Name = "Agent2", Type = "Deliberative", Status = "Ready" }
            });
        }

        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="configuration">Agent configuration</param>
        /// <returns>The created agent</returns>
        public Task<Agent> CreateAgentAsync(AgentConfiguration configuration)
        {
            // Placeholder implementation
            return Task.FromResult(new Agent
            {
                Id = Guid.NewGuid().ToString(),
                Name = configuration.Name,
                Type = configuration.Type,
                Status = "Ready"
            });
        }

        /// <summary>
        /// Sends a message to an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="message">Message to send</param>
        /// <returns>Response from the agent</returns>
        public Task<AgentResponse> SendMessageAsync(string agentId, string message)
        {
            // Placeholder implementation
            return Task.FromResult(new AgentResponse
            {
                Success = true,
                Message = $"I received your message: {message}",
                Data = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["text"] = $"I received your message: {message}"
                }
            });
        }

        /// <summary>
        /// Deletes an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        public Task DeleteAgentAsync(string agentId)
        {
            // Placeholder implementation
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Represents an agent
    /// </summary>
    public class Agent
    {
        /// <summary>
        /// ID of the agent
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the agent
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Status of the agent
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Configuration for an agent
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the agent
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Response from an agent
    /// </summary>
    public class AgentResponse
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message from the agent
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Additional data from the agent
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Data { get; set; }
    }
}
