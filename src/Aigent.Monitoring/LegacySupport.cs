using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Logger interface for backward compatibility
    /// </summary>
    public interface ILogger : Logging.ILogger
    {
    }

    /// <summary>
    /// Re-export LogLevel enum for backward compatibility
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed debug information
        /// </summary>
        Debug = Logging.LogLevel.Debug,

        /// <summary>
        /// Interesting events
        /// </summary>
        Information = Logging.LogLevel.Information,

        /// <summary>
        /// Non-critical issues
        /// </summary>
        Warning = Logging.LogLevel.Warning,

        /// <summary>
        /// Critical issues
        /// </summary>
        Error = Logging.LogLevel.Error,

        /// <summary>
        /// Fatal issues that cause the application to crash
        /// </summary>
        Critical = Logging.LogLevel.Critical
    }

    /// <summary>
    /// Console logger for backward compatibility
    /// </summary>
    public class ConsoleLogger : Logging.ConsoleLogger, ILogger
    {
        /// <summary>
        /// Initializes a new instance of the ConsoleLogger class
        /// </summary>
        /// <param name="minimumLevel">Minimum log level to display</param>
        /// <param name="includeTimestamps">Whether to include timestamps in log messages</param>
        public ConsoleLogger(LogLevel minimumLevel = LogLevel.Information, bool includeTimestamps = true)
            : base((Logging.LogLevel)minimumLevel, includeTimestamps)
        {
        }
    }

    /// <summary>
    /// Metrics collector interface for backward compatibility
    /// </summary>
    public interface IMetricsCollector : Metrics.IMetricsCollector
    {
    }

    /// <summary>
    /// Re-export MetricsSummary class for backward compatibility
    /// </summary>
    public class MetricsSummary : Metrics.MetricsSummary
    {
    }

    /// <summary>
    /// Re-export MetricData class for backward compatibility
    /// </summary>
    public class MetricData : Metrics.MetricData
    {
    }

    /// <summary>
    /// Basic metrics collector for backward compatibility
    /// </summary>
    public class BasicMetricsCollector : Metrics.BasicMetricsCollector, IMetricsCollector
    {
        /// <summary>
        /// Initializes a new instance of the BasicMetricsCollector class
        /// </summary>
        /// <param name="logger">Logger</param>
        public BasicMetricsCollector(ILogger logger = null)
            : base(logger)
        {
        }
    }

    /// <summary>
    /// In-memory metrics collector for backward compatibility
    /// </summary>
    public class InMemoryMetricsCollector : Metrics.InMemoryMetricsCollector, IMetricsCollector
    {
        /// <summary>
        /// Initializes a new instance of the InMemoryMetricsCollector class
        /// </summary>
        /// <param name="logger">Logger for recording metrics activities</param>
        public InMemoryMetricsCollector(ILogger logger = null)
            : base(logger)
        {
        }
    }
}
