using System;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level
        /// </summary>
        Debug,
        
        /// <summary>
        /// Information level
        /// </summary>
        Information,
        
        /// <summary>
        /// Warning level
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error level
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical level
        /// </summary>
        Critical
    }
    
    /// <summary>
    /// Interface for loggers
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        void Log(LogLevel level, string message);
        
        /// <summary>
        /// Logs an error message with exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception</param>
        void LogError(string message, Exception exception);
    }
}
