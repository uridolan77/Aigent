using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Monitoring.Logging;

namespace Aigent.Monitoring.Metrics
{
    /// <summary>
    /// In-memory implementation of IMetricsCollector
    /// </summary>
    public class InMemoryMetricsCollector : IMetricsCollector
    {
        private readonly ConcurrentDictionary<string, List<MetricEntry>> _metrics = new();
        private readonly ConcurrentDictionary<string, DateTime> _operations = new();
        private readonly Logging.ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the InMemoryMetricsCollector class
        /// </summary>
        /// <param name="logger">Logger for recording metrics activities</param>
        public InMemoryMetricsCollector(Logging.ILogger logger = null)
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
            var entry = new MetricEntry
            {
                Timestamp = DateTime.UtcNow,
                Value = value,
                Tags = tags ?? new Dictionary<string, string>()
            };

            _metrics.AddOrUpdate(
                name,
                new List<MetricEntry> { entry },
                (_, entries) =>
                {
                    lock (entries)
                    {
                        entries.Add(entry);
                        return entries;
                    }
                });

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

            foreach (var metricPair in _metrics)
            {
                var name = metricPair.Key;
                List<MetricEntry> entriesCopy;

                lock (metricPair.Value)
                {
                    entriesCopy = metricPair.Value.ToList();
                }

                var relevantEntries = entriesCopy
                    .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                    .ToList();

                if (relevantEntries.Any())
                {
                    var values = relevantEntries.Select(e => e.Value).ToList();
                    var metricData = new MetricData
                    {
                        Name = name,
                        Count = values.Count,
                        Sum = values.Sum(),
                        Min = values.Min(),
                        Max = values.Max()
                    };

                    // Process tags
                    foreach (var entry in relevantEntries.Where(e => e.Tags?.Any() == true))
                    {
                        foreach (var tag in entry.Tags)
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

            return Task.FromResult(summary);
        }

        /// <summary>
        /// Starts timing an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        public void StartOperation(string operationName)
        {
            _operations[operationName] = DateTime.UtcNow;
            _logger?.Log(LogLevel.Debug, $"Started operation: {operationName}");
        }

        /// <summary>
        /// Ends timing an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        public void EndOperation(string operationName)
        {
            if (_operations.TryRemove(operationName, out var startTime))
            {
                var duration = DateTime.UtcNow - startTime;
                RecordMetric($"operation.{operationName}.duration_ms", duration.TotalMilliseconds);
                _logger?.Log(LogLevel.Debug, $"Ended operation: {operationName}, duration: {duration.TotalMilliseconds}ms");
            }
            else
            {
                _logger?.LogWarning($"Attempted to end non-existent operation: {operationName}");
            }
        }

        /// <summary>
        /// Entry for a recorded metric
        /// </summary>
        private class MetricEntry
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
