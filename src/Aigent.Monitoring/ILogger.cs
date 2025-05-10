using System;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Log severity levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed debug information
        /// </summary>
        Debug,

        /// <summary>
        /// Interesting events
        /// </summary>
        Information,

        /// <summary>
        /// Non-critical issues
        /// </summary>
        Warning,

        /// <summary>
        /// Critical issues
        /// </summary>
        Error,

        /// <summary>
        /// Fatal issues that cause the application to crash
        /// </summary>
        Critical
    }

    /// <summary>
    /// Interface for logging services
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified log level
        /// </summary>
        /// <param name="level">Severity level of the log</param>
        /// <param name="message">Message to log</param>
        void Log(LogLevel level, string message);

        /// <summary>
        /// Logs an error message with exception details
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception that occurred</param>
        void LogError(string message, Exception exception);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        void LogWarning(string message);
    }
}
