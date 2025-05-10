using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Basic metrics collector implementation
    /// </summary>
    public class BasicMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, double> _metrics = new();
        private readonly Dictionary<string, Stopwatch> _operations = new();
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the BasicMetricsCollector class
        /// </summary>
        /// <param name="logger">Logger</param>
        public BasicMetricsCollector(ILogger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Records a metric
        /// </summary>
        /// <param name="name">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        public void RecordMetric(string name, double value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }
            
            lock (_metrics)
            {
                _metrics[name] = value;
            }
            
            _logger?.Log(LogLevel.Debug, $"Recorded metric: {name} = {value}");
        }
        
        /// <summary>
        /// Starts an operation
        /// </summary>
        /// <param name="name">Name of the operation</param>
        public void StartOperation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Operation name cannot be null or empty", nameof(name));
            }
            
            lock (_operations)
            {
                if (_operations.TryGetValue(name, out var stopwatch))
                {
                    stopwatch.Restart();
                }
                else
                {
                    _operations[name] = Stopwatch.StartNew();
                }
            }
            
            _logger?.Log(LogLevel.Debug, $"Started operation: {name}");
        }
        
        /// <summary>
        /// Ends an operation
        /// </summary>
        /// <param name="name">Name of the operation</param>
        public void EndOperation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Operation name cannot be null or empty", nameof(name));
            }
            
            lock (_operations)
            {
                if (_operations.TryGetValue(name, out var stopwatch))
                {
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed.TotalMilliseconds;
                    
                    lock (_metrics)
                    {
                        _metrics[$"{name}_duration_ms"] = elapsed;
                    }
                    
                    _logger?.Log(LogLevel.Debug, $"Ended operation: {name} in {elapsed}ms");
                }
                else
                {
                    _logger?.Log(LogLevel.Warning, $"Attempted to end non-existent operation: {name}");
                }
            }
        }
    }
}
