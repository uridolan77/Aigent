using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Interface for metrics collection services
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Records a metric value
        /// </summary>
        /// <param name="name">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        /// <param name="tags">Optional tags for the metric</param>
        void RecordMetric(string name, double value, Dictionary<string, string> tags = null);
        
        /// <summary>
        /// Gets a summary of metrics for a specified duration
        /// </summary>
        /// <param name="duration">Duration to summarize</param>
        /// <returns>Summary of metrics</returns>
        Task<MetricsSummary> GetSummary(TimeSpan duration);
        
        /// <summary>
        /// Starts timing an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        void StartOperation(string operationName);
        
        /// <summary>
        /// Ends timing an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        void EndOperation(string operationName);
    }

    /// <summary>
    /// Summary of metrics
    /// </summary>
    public class MetricsSummary
    {
        /// <summary>
        /// Metrics by name
        /// </summary>
        public Dictionary<string, MetricData> Metrics { get; set; } = new();
        
        /// <summary>
        /// Start time of the summary period
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// End time of the summary period
        /// </summary>
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// Data for a specific metric
    /// </summary>
    public class MetricData
    {
        /// <summary>
        /// Name of the metric
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Count of values
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Sum of values
        /// </summary>
        public double Sum { get; set; }
        
        /// <summary>
        /// Minimum value
        /// </summary>
        public double Min { get; set; }
        
        /// <summary>
        /// Maximum value
        /// </summary>
        public double Max { get; set; }
        
        /// <summary>
        /// Average value
        /// </summary>
        public double Average => Count > 0 ? Sum / Count : 0;
    }
}
