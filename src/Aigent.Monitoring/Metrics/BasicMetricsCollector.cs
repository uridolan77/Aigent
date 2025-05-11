using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Monitoring.Logging;

namespace Aigent.Monitoring.Metrics
{
    /// <summary>
    /// Basic metrics collector implementation
    /// </summary>
    public class BasicMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, List<MetricRecord>> _metrics = new();
        private readonly Dictionary<string, Stopwatch> _operations = new();
        private readonly Logging.ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the BasicMetricsCollector class
        /// </summary>
        /// <param name="logger">Logger</param>
        public BasicMetricsCollector(Logging.ILogger logger = null)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Records a metric value
        /// </summary>
        /// <param name="name">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        public void RecordMetric(string name, double value)
        {
            RecordMetric(name, value, null);
        }

        /// <summary>
        /// Records a metric value with tags
        /// </summary>
        /// <param name="name">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        /// <param name="tags">Tags for the metric</param>
        public void RecordMetric(string name, double value, Dictionary<string, string> tags)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }
            
            var record = new MetricRecord
            {
                Timestamp = DateTime.UtcNow,
                Value = value,
                Tags = tags
            };

            lock (_metrics)
            {
                if (!_metrics.TryGetValue(name, out var records))
                {
                    records = new List<MetricRecord>();
                    _metrics[name] = records;
                }
                
                records.Add(record);
            }
            
            _logger?.Log(LogLevel.Debug, $"Recorded metric: {name} = {value}");
        }

        /// <summary>
        /// Gets a summary of metrics for a specified duration
        /// </summary>
        /// <param name="duration">Duration to summarize</param>
        /// <returns>Summary of metrics</returns>
        public Task<MetricsSummary> GetSummary(TimeSpan duration)
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime - duration;
            
            var summary = new MetricsSummary
            {
                StartTime = startTime,
                EndTime = endTime
            };

            lock (_metrics)
            {
                foreach (var entry in _metrics)
                {
                    var name = entry.Key;
                    var records = entry.Value;
                    
                    var relevantRecords = records
                        .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                        .ToList();
                    
                    if (relevantRecords.Any())
                    {
                        var values = relevantRecords.Select(r => r.Value).ToList();
                        var metricData = new MetricData
                        {
                            Name = name,
                            Count = values.Count,
                            Sum = values.Sum(),
                            Min = values.Min(),
                            Max = values.Max()
                        };
                        
                        // Process tags
                        foreach (var record in relevantRecords.Where(r => r.Tags?.Any() == true))
                        {
                            foreach (var tag in record.Tags)
                            {
                                if (!metricData.Tags.TryGetValue(tag.Key, out var tagValues))
                                {
                                    tagValues = new List<string>();
                                    metricData.Tags[tag.Key] = tagValues;
                                }

                                if (!tagValues.Contains(tag.Value))
                                {
                                    tagValues.Add(tag.Value);
                                }
                            }
                        }
                        
                        summary.Metrics[name] = metricData;
                    }
                }
            }
            
            return Task.FromResult(summary);
        }
        
        /// <summary>
        /// Starts timing an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        public void StartOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
            }
            
            lock (_operations)
            {
                if (_operations.TryGetValue(operationName, out var stopwatch))
                {
                    stopwatch.Restart();
                }
                else
                {
                    _operations[operationName] = Stopwatch.StartNew();
                }
            }
            
            _logger?.Log(LogLevel.Debug, $"Started operation: {operationName}");
        }
        
        /// <summary>
        /// Ends timing an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        public void EndOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
            }
            
            double elapsed = 0;
            
            lock (_operations)
            {
                if (_operations.TryGetValue(operationName, out var stopwatch))
                {
                    stopwatch.Stop();
                    elapsed = stopwatch.Elapsed.TotalMilliseconds;
                }
                else
                {
                    _logger?.Log(LogLevel.Warning, $"Attempted to end non-existent operation: {operationName}");
                    return;
                }
            }
            
            RecordMetric($"operation.{operationName}.duration_ms", elapsed);
            _logger?.Log(LogLevel.Debug, $"Ended operation: {operationName} in {elapsed}ms");
        }

        /// <summary>
        /// Class to store a metric record with timestamp and tags
        /// </summary>
        private class MetricRecord
        {
            /// <summary>
            /// When the metric was recorded
            /// </summary>
            public DateTime Timestamp { get; set; }
            
            /// <summary>
            /// Value of the metric
            /// </summary>
            public double Value { get; set; }
            
            /// <summary>
            /// Tags for the metric
            /// </summary>
            public Dictionary<string, string> Tags { get; set; }
        }
    }
}
