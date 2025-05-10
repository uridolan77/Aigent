using System;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Core;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;
using Aigent.Orchestration;

namespace Aigent.Configuration
{
    /// <summary>
    /// Extension methods for IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Aigent services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Configuration for Aigent</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddAigent(this IServiceCollection services, IConfiguration configuration)
        {
            // Core services
            services.AddSingleton<ILogger, ConsoleLogger>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();

            // Memory services
            var memoryType = configuration["Aigent:MemoryType"];
            switch (memoryType)
            {
                case "Redis":
                    services.AddSingleton<ILongTermMemory>(sp =>
                        new RedisMemoryService(
                            configuration["Aigent:Redis:ConnectionString"],
                            sp.GetService<ILogger>(),
                            sp.GetService<IMetricsCollector>()));
                    break;
                case "SQL":
                    services.AddSingleton<ILongTermMemory>(sp =>
                        new SqlMemoryService(
                            configuration["Aigent:SQL:ConnectionString"],
                            sp.GetService<ILogger>(),
                            sp.GetService<IMetricsCollector>()));
                    break;
                case "DocumentDb":
                    services.AddSingleton<ILongTermMemory>(sp =>
                        new DocumentDbMemoryService(
                            configuration["Aigent:DocumentDb:ConnectionString"],
                            configuration["Aigent:DocumentDb:DatabaseName"] ?? "AigentMemory",
                            configuration["Aigent:DocumentDb:CollectionName"] ?? "AgentContext",
                            sp.GetService<ILogger>(),
                            sp.GetService<IMetricsCollector>()));
                    break;
                default:
                    services.AddSingleton<ILongTermMemory, ConcurrentMemoryService>();
                    break;
            }

            services.AddSingleton<IShortTermMemory, ConcurrentMemoryService>();

            // Safety and ethics
            services.AddSingleton<ISafetyValidator, EnhancedSafetyValidator>();
            services.AddSingleton<IEthicsEngine, NlpEthicsEngine>();
            services.AddSingleton<INlpService, MockNlpService>(); // Replace with real implementation

            // Orchestration
            services.AddSingleton<IOrchestrator, EnhancedOrchestrator>();

            // Agent builder
            services.AddTransient<IAgentBuilder, EnhancedAgentBuilder>();

            return services;
        }
    }

    /// <summary>
    /// Mock NLP service for demonstration purposes
    /// </summary>
    public class MockNlpService : INlpService
    {
        /// <summary>
        /// Analyzes the intent of a text
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Analysis result</returns>
        public Task<IntentAnalysisResult> AnalyzeIntent(string text)
        {
            // Mock implementation
            return Task.FromResult(new IntentAnalysisResult
            {
                IsHarmful = text.Contains("harmful", StringComparison.OrdinalIgnoreCase) ||
                           text.Contains("dangerous", StringComparison.OrdinalIgnoreCase),
                EthicalScore = text.Contains("help", StringComparison.OrdinalIgnoreCase) ? 0.9 : 0.5,
                DetectedIntents = new List<string> { "general_query" }
            });
        }
    }

    /// <summary>
    /// Interface for NLP services
    /// </summary>
    public interface INlpService
    {
        /// <summary>
        /// Analyzes the intent of a text
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Analysis result</returns>
        Task<IntentAnalysisResult> AnalyzeIntent(string text);
    }

    /// <summary>
    /// Result of intent analysis
    /// </summary>
    public class IntentAnalysisResult
    {
        /// <summary>
        /// Whether the intent is harmful
        /// </summary>
        public bool IsHarmful { get; set; }

        /// <summary>
        /// Ethical score of the intent (0.0 to 1.0)
        /// </summary>
        public double EthicalScore { get; set; }

        /// <summary>
        /// Detected intents
        /// </summary>
        public List<string> DetectedIntents { get; set; } = new();
    }

    /// <summary>
    /// NLP-based ethics engine
    /// </summary>
    public class NlpEthicsEngine : IEthicsEngine
    {
        private readonly INlpService _nlpService;

        /// <summary>
        /// Initializes a new instance of the NlpEthicsEngine class
        /// </summary>
        /// <param name="nlpService">NLP service for intent analysis</param>
        public NlpEthicsEngine(INlpService nlpService)
        {
            _nlpService = nlpService ?? throw new ArgumentNullException(nameof(nlpService));
        }

        /// <summary>
        /// Validates parameters against ethical guidelines
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>Whether the parameters are ethically valid</returns>
        public async Task<bool> ValidateAsync(Dictionary<string, object> parameters)
        {
            // Extract text content from parameters
            var content = ExtractTextContent(parameters);

            // Analyze intent using NLP
            var intent = await _nlpService.AnalyzeIntent(content);

            // Check if intent is ethical
            return IsEthicalIntent(intent);
        }

        private string ExtractTextContent(Dictionary<string, object> parameters)
        {
            return string.Join(" ", parameters.Values
                .Where(v => v is string)
                .Cast<string>());
        }

        private bool IsEthicalIntent(IntentAnalysisResult intent)
        {
            // Check against ethical guidelines
            return !intent.IsHarmful && intent.EthicalScore > 0.7;
        }
    }
}
