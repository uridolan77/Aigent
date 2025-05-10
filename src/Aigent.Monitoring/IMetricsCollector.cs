namespace Aigent.Monitoring
{
    /// <summary>
    /// Interface for metrics collectors
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Records a metric
        /// </summary>
        /// <param name="name">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        void RecordMetric(string name, double value);
        
        /// <summary>
        /// Starts an operation
        /// </summary>
        /// <param name="name">Name of the operation</param>
        void StartOperation(string name);
        
        /// <summary>
        /// Ends an operation
        /// </summary>
        /// <param name="name">Name of the operation</param>
        void EndOperation(string name);
    }
}
