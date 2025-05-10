// Performance Benchmarking Utilities
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Exporters;
using System.Collections.Concurrent;

namespace AgentSystem.Benchmarks
{
    // Benchmark Configuration
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class AgentBenchmarks
    {
        private IAgent _reactiveAgent;
        private IAgent _deliberativeAgent;
        private IAgent _hybridAgent;
        private IOrchestrator _orchestrator;
        private EnvironmentState _simpleState;
        private EnvironmentState _complexState;
        private WorkflowDefinition _simpleWorkflow;
        private WorkflowDefinition _complexWorkflow;

        [GlobalSetup]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            services.AddAgentSystem(new ConfigurationBuilder().Build());
            
            var serviceProvider = services.BuildServiceProvider();
            var builder = serviceProvider.GetRequiredService<IAgentBuilder>();
            
            // Create agents
            _reactiveAgent = builder
                .WithConfiguration(new AgentConfiguration 
                { 
                    Name = "BenchmarkReactive", 
                    Type = AgentType.Reactive 
                })
                .Build();
                
            _deliberativeAgent = builder
                .WithConfiguration(new AgentConfiguration 
                { 
                    Name = "BenchmarkDeliberative", 
                    Type = AgentType.Deliberative 
                })
                .Build();
                
            _hybridAgent = builder
                .WithConfiguration(new AgentConfiguration 
                { 
                    Name = "BenchmarkHybrid", 
                    Type = AgentType.Hybrid 
                })
                .Build();
            
            _orchestrator = serviceProvider.GetRequiredService<IOrchestrator>();
            
            // Create test states
            _simpleState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "simple query"
                }
            };
            
            _complexState = new EnvironmentState
            {
                Properties = new Dictionary<string, object>
                {
                    ["input"] = "complex query",
                    ["context"] = new string('x', 1000),
                    ["data"] = Enumerable.Range(0, 100).ToDictionary(i => $"key{i}", i => (object)i)
                }
            };
            
            // Create workflows
            _simpleWorkflow = new WorkflowDefinition
            {
                Name = "SimpleWorkflow",
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep { Name = "Step1", RequiredAgentType = AgentType.Reactive }
                }
            };
            
            _complexWorkflow = new WorkflowDefinition
            {
                Name = "ComplexWorkflow",
                Type = WorkflowType.Parallel,
                Steps = Enumerable.Range(0, 10).Select(i => new WorkflowStep
                {
                    Name = $"Step{i}",
                    RequiredAgentType = i % 2 == 0 ? AgentType.Reactive : AgentType.Deliberative
                }).ToList()
            };
            
            // Initialize agents
            await _reactiveAgent.Initialize();
            await _deliberativeAgent.Initialize();
            await _hybridAgent.Initialize();
            
            await _orchestrator.RegisterAgent(_reactiveAgent);
            await _orchestrator.RegisterAgent(_deliberativeAgent);
            await _orchestrator.RegisterAgent(_hybridAgent);
        }

        [Benchmark(Baseline = true)]
        public async Task<IAction> ReactiveAgent_SimpleDecision()
        {
            return await _reactiveAgent.DecideAction(_simpleState);
        }

        [Benchmark]
        public async Task<IAction> ReactiveAgent_ComplexDecision()
        {
            return await _reactiveAgent.DecideAction(_complexState);
        }

        [Benchmark]
        public async Task<IAction> DeliberativeAgent_SimpleDecision()
        {
            return await _deliberativeAgent.DecideAction(_simpleState);
        }

        [Benchmark]
        public async Task<IAction> DeliberativeAgent_ComplexDecision()
        {
            return await _deliberativeAgent.DecideAction(_complexState);
        }

        [Benchmark]
        public async Task<IAction> HybridAgent_SimpleDecision()
        {
            return await _hybridAgent.DecideAction(_simpleState);
        }

        [Benchmark]
        public async Task<IAction> HybridAgent_ComplexDecision()
        {
            return await _hybridAgent.DecideAction(_complexState);
        }

        [Benchmark]
        public async Task<WorkflowResult> Orchestrator_SimpleWorkflow()
        {
            return await _orchestrator.ExecuteWorkflow(_simpleWorkflow);
        }

        [Benchmark]
        public async Task<WorkflowResult> Orchestrator_ComplexWorkflow()
        {
            return await _orchestrator.ExecuteWorkflow(_complexWorkflow);
        }
    }

    // Memory benchmarks
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class MemoryBenchmarks
    {
        private IMemoryService _inMemoryService;
        private IMemoryService _redisService;
        private IMemoryService _sqlService;
        private string _testKey = "benchmark_key";
        private object _testData;

        [GlobalSetup]
        public void Setup()
        {
            _inMemoryService = new ConcurrentMemoryService();
            _redisService = new RedisMemoryService("localhost:6379");
            _sqlService = new SqlMemoryService("Server=localhost;Database=AgentSystem;");
            
            _testData = new
            {
                Id = Guid.NewGuid(),
                Name = "Test Data",
                Values = Enumerable.Range(0, 100).ToList(),
                Timestamp = DateTime.UtcNow
            };
            
            _inMemoryService.Initialize("bench").Wait();
            _redisService.Initialize("bench").Wait();
            _sqlService.Initialize("bench").Wait();
        }

        [Benchmark(Baseline = true)]
        public async Task InMemory_StoreAndRetrieve()
        {
            await _inMemoryService.StoreContext(_testKey, _testData);
            var result = await _inMemoryService.RetrieveContext<object>(_testKey);
        }

        [Benchmark]
        public async Task Redis_StoreAndRetrieve()
        {
            await _redisService.StoreContext(_testKey, _testData);
            var result = await _redisService.RetrieveContext<object>(_testKey);
        }

        [Benchmark]
        public async Task SQL_StoreAndRetrieve()
        {
            await _sqlService.StoreContext(_testKey, _testData);
            var result = await _sqlService.RetrieveContext<object>(_testKey);
        }
    }

    // Message bus benchmarks
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60, warmupCount: 3, iterationCount: 10)]
    public class MessageBusBenchmarks
    {
        private IMessageBus _messageBus;
        private List<string> _topics;
        private object _testMessage;
        private int _messageCount = 1000;

        [GlobalSetup]
        public void Setup()
        {
            var logger = new Mock<ILogger>().Object;
            _messageBus = new InMemoryMessageBus(logger);
            
            _topics = Enumerable.Range(0, 10).Select(i => $"topic_{i}").ToList();
            _testMessage = new { Id = Guid.NewGuid(), Data = "Test" };
            
            // Subscribe to topics
            foreach (var topic in _topics)
            {
                _messageBus.Subscribe(topic, msg => { });
            }
        }

        [Benchmark]
        public async Task PublishSingleMessage()
        {
            await _messageBus.PublishAsync(_topics[0], _testMessage);
        }

        [Benchmark]
        public async Task PublishToMultipleTopics()
        {
            var tasks = _topics.Select(topic => 
                _messageBus.PublishAsync(topic, _testMessage));
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task PublishManyMessages()
        {
            var tasks = Enumerable.Range(0, _messageCount)
                .Select(i => _messageBus.PublishAsync(_topics[i % _topics.Count], _testMessage));
            await Task.WhenAll(tasks);
        }
    }

    // Concurrent operations benchmark
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    public class ConcurrencyBenchmarks
    {
        private IAgent _agent;
        private IOrchestrator _orchestrator;
        private ConcurrentDictionary<string, IAgent> _agents;
        private List<EnvironmentState> _states;

        [Params(1, 10, 100)]
        public int ConcurrencyLevel { get; set; }

        [GlobalSetup]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            services.AddAgentSystem(new ConfigurationBuilder().Build());
            var serviceProvider = services.BuildServiceProvider();
            
            var builder = serviceProvider.GetRequiredService<IAgentBuilder>();
            _orchestrator = serviceProvider.GetRequiredService<IOrchestrator>();
            
            _agents = new ConcurrentDictionary<string, IAgent>();
            
            // Create multiple agents
            for (int i = 0; i < ConcurrencyLevel; i++)
            {
                var agent = builder
                    .WithConfiguration(new AgentConfiguration 
                    { 
                        Name = $"ConcurrentAgent{i}", 
                        Type = AgentType.Reactive 
                    })
                    .Build();
                    
                await agent.Initialize();
                _agents[agent.Id] = agent;
                await _orchestrator.RegisterAgent(agent);
            }
            
            // Create test states
            _states = Enumerable.Range(0, ConcurrencyLevel)
                .Select(i => new EnvironmentState
                {
                    Properties = new Dictionary<string, object>
                    {
                        ["id"] = i,
                        ["input"] = $"query_{i}"
                    }
                }).ToList();
        }

        [Benchmark]
        public async Task ConcurrentDecisions()
        {
            var tasks = _agents.Values.Zip(_states, async (agent, state) => 
                await agent.DecideAction(state));
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task ConcurrentWorkflows()
        {
            var tasks = Enumerable.Range(0, ConcurrencyLevel).Select(i =>
            {
                var workflow = new WorkflowDefinition
                {
                    Name = $"ConcurrentWorkflow{i}",
                    Type = WorkflowType.Sequential,
                    Steps = new List<WorkflowStep>
                    {
                        new WorkflowStep 
                        { 
                            Name = "Step1", 
                            RequiredAgentType = AgentType.Reactive 
                        }
                    }
                };
                return _orchestrator.ExecuteWorkflow(workflow);
            });
            await Task.WhenAll(tasks);
        }
    }

    // Benchmark configuration
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.Median);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.P95);
            AddColumn(StatisticColumn.P99);
            AddExporter(HtmlExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(CsvExporter.Default);
        }
    }

    // Benchmark runner
    public class BenchmarkRunner
    {
        public static async Task RunBenchmarks()
        {
            var config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(10))
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddDiagnoser(ThreadingDiagnoser.Default)
                .AddExporter(HtmlExporter.Default)
                .AddExporter(PlainExporter.Default);

            var summary = BenchmarkRunner.Run<AgentBenchmarks>(config);
            Console.WriteLine("Agent benchmarks completed. Results saved to BenchmarkDotNet.Artifacts");

            summary = BenchmarkRunner.Run<MemoryBenchmarks>(config);
            Console.WriteLine("Memory benchmarks completed. Results saved to BenchmarkDotNet.Artifacts");

            summary = BenchmarkRunner.Run<MessageBusBenchmarks>(config);
            Console.WriteLine("Message bus benchmarks completed. Results saved to BenchmarkDotNet.Artifacts");

            summary = BenchmarkRunner.Run<ConcurrencyBenchmarks>(config);
            Console.WriteLine("Concurrency benchmarks completed. Results saved to BenchmarkDotNet.Artifacts");
        }
    }

    // Performance monitoring and profiling
    public class PerformanceMonitor
    {
        private readonly IMetricsCollector _metrics;
        private readonly ILogger _logger;
        private readonly Timer _timer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;

        public PerformanceMonitor(IMetricsCollector metrics, ILogger logger)
        {
            _metrics = metrics;
            _logger = logger;
            
            _cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            _memoryCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
            
            _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void CollectMetrics(object state)
        {
            try
            {
                var cpuUsage = _cpuCounter.NextValue();
                var memoryUsage = _memoryCounter.NextValue() / (1024 * 1024); // MB
                
                _metrics.RecordMetric("system.cpu_usage", cpuUsage);
                _metrics.RecordMetric("system.memory_usage", memoryUsage);
                
                // GC metrics
                for (int i = 0; i <= GC.MaxGeneration; i++)
                {
                    _metrics.RecordMetric($"gc.collection_count.gen{i}", GC.CollectionCount(i));
                }
                
                // Thread pool metrics
                ThreadPool.GetAvailableThreads(out int workerThreads, out int completionThreads);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionThreads);
                
                _metrics.RecordMetric("threadpool.available_worker_threads", workerThreads);
                _metrics.RecordMetric("threadpool.available_completion_threads", completionThreads);
                _metrics.RecordMetric("threadpool.busy_worker_threads", maxWorkerThreads - workerThreads);
                _metrics.RecordMetric("threadpool.busy_completion_threads", maxCompletionThreads - completionThreads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting performance metrics");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
        }
    }

    // Load testing utility
    public class LoadTester
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;

        public LoadTester(IOrchestrator orchestrator, ILogger logger, IMetricsCollector metrics)
        {
            _orchestrator = orchestrator;
            _logger = logger;
            _metrics = metrics;
        }

        public async Task RunLoadTest(LoadTestConfiguration config)
        {
            _logger.LogInformation($"Starting load test: {config.Name}");
            
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<LoadTestResult>>();
            var semaphore = new SemaphoreSlim(config.MaxConcurrency);
            
            using var cts = new CancellationTokenSource(config.Duration);
            
            for (int i = 0; i < config.TotalRequests; i++)
            {
                if (cts.Token.IsCancellationRequested)
                    break;
                    
                await semaphore.WaitAsync();
                
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await ExecuteSingleRequest(config, i);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
                
                if (config.RampUp)
                {
                    var delay = CalculateRampUpDelay(i, config);
                    await Task.Delay(delay);
                }
            }
            
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            var summary = AnalyzeResults(results, stopwatch.Elapsed);
            await GenerateReport(config, summary);
            
            _logger.LogInformation($"Load test completed: {summary}");
        }

        private async Task<LoadTestResult> ExecuteSingleRequest(LoadTestConfiguration config, int requestId)
        {
            var result = new LoadTestResult { RequestId = requestId };
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var workflow = CreateTestWorkflow(config, requestId);
                var workflowResult = await _orchestrator.ExecuteWorkflow(workflow);
                
                result.Success = workflowResult.Success;
                result.Duration = stopwatch.Elapsed;
                
                _metrics.RecordMetric("loadtest.request_duration", result.Duration.TotalMilliseconds,
                    new Dictionary<string, string> { ["test"] = config.Name });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.Duration = stopwatch.Elapsed;
                
                _metrics.RecordMetric("loadtest.request_error", 1,
                    new Dictionary<string, string> { ["test"] = config.Name, ["error"] = ex.GetType().Name });
            }
            
            return result;
        }

        private WorkflowDefinition CreateTestWorkflow(LoadTestConfiguration config, int requestId)
        {
            return new WorkflowDefinition
            {
                Name = $"LoadTest_{config.Name}_{requestId}",
                Type = config.WorkflowType,
                Steps = Enumerable.Range(0, config.StepsPerWorkflow).Select(i => new WorkflowStep
                {
                    Name = $"Step_{i}",
                    RequiredAgentType = config.AgentTypes[i % config.AgentTypes.Count],
                    Parameters = new Dictionary<string, object>
                    {
                        ["request_id"] = requestId,
                        ["step_id"] = i,
                        ["payload"] = GeneratePayload(config.PayloadSize)
                    }
                }).ToList()
            };
        }

        private string GeneratePayload(int size)
        {
            return new string('x', size);
        }

        private int CalculateRampUpDelay(int requestIndex, LoadTestConfiguration config)
        {
            var progress = (double)requestIndex / config.TotalRequests;
            var targetRps = config.MaxRequestsPerSecond * progress;
            return (int)(1000 / Math.Max(1, targetRps));
        }

        private LoadTestSummary AnalyzeResults(LoadTestResult[] results, TimeSpan totalDuration)
        {
            var summary = new LoadTestSummary
            {
                TotalRequests = results.Length,
                SuccessfulRequests = results.Count(r => r.Success),
                FailedRequests = results.Count(r => !r.Success),
                TotalDuration = totalDuration,
                AverageResponseTime = TimeSpan.FromMilliseconds(results.Average(r => r.Duration.TotalMilliseconds)),
                MinResponseTime = TimeSpan.FromMilliseconds(results.Min(r => r.Duration.TotalMilliseconds)),
                MaxResponseTime = TimeSpan.FromMilliseconds(results.Max(r => r.Duration.TotalMilliseconds)),
                P95ResponseTime = TimeSpan.FromMilliseconds(Percentile(results.Select(r => r.Duration.TotalMilliseconds).ToList(), 95)),
                P99ResponseTime = TimeSpan.FromMilliseconds(Percentile(results.Select(r => r.Duration.TotalMilliseconds).ToList(), 99)),
                RequestsPerSecond = results.Length / totalDuration.TotalSeconds
            };
            
            return summary;
        }

        private double Percentile(List<double> values, int percentile)
        {
            values.Sort();
            var index = (percentile / 100.0) * values.Count;
            if (index % 1 == 0)
                return values[(int)index - 1];
            else
                return (values[(int)Math.Floor(index) - 1] + values[(int)Math.Ceiling(index) - 1]) / 2;
        }

        private async Task GenerateReport(LoadTestConfiguration config, LoadTestSummary summary)
        {
            var report = $@"
# Load Test Report: {config.Name}

## Configuration
- Total Requests: {config.TotalRequests}
- Duration: {config.Duration}
- Max Concurrency: {config.MaxConcurrency}
- Steps per Workflow: {config.StepsPerWorkflow}
- Workflow Type: {config.WorkflowType}
- Payload Size: {config.PayloadSize} bytes

## Results
- Total Requests: {summary.TotalRequests}
- Successful Requests: {summary.SuccessfulRequests}
- Failed Requests: {summary.FailedRequests}
- Success Rate: {(double)summary.SuccessfulRequests / summary.TotalRequests:P}
- Requests per Second: {summary.RequestsPerSecond:F2}

## Response Times
- Average: {summary.AverageResponseTime.TotalMilliseconds:F2}ms
- Minimum: {summary.MinResponseTime.TotalMilliseconds:F2}ms
- Maximum: {summary.MaxResponseTime.TotalMilliseconds:F2}ms
- P95: {summary.P95ResponseTime.TotalMilliseconds:F2}ms
- P99: {summary.P99ResponseTime.TotalMilliseconds:F2}ms

## Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
";
            
            await File.WriteAllTextAsync($"loadtest_{config.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.md", report);
        }
    }

    // Configuration classes
    public class LoadTestConfiguration
    {
        public string Name { get; set; }
        public int TotalRequests { get; set; }
        public TimeSpan Duration { get; set; }
        public int MaxConcurrency { get; set; }
        public int MaxRequestsPerSecond { get; set; }
        public int StepsPerWorkflow { get; set; }
        public WorkflowType WorkflowType { get; set; }
        public List<AgentType> AgentTypes { get; set; }
        public int PayloadSize { get; set; }
        public bool RampUp { get; set; }
    }

    public class LoadTestResult
    {
        public int RequestId { get; set; }
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string Error { get; set; }
    }

    public class LoadTestSummary
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
        public TimeSpan P95ResponseTime { get; set; }
        public TimeSpan P99ResponseTime { get; set; }
        public double RequestsPerSecond { get; set; }
    }
}
