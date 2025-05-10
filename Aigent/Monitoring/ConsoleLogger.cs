using System;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Console implementation of ILogger
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _minimumLevel;
        private readonly bool _includeTimestamps;

        /// <summary>
        /// Initializes a new instance of the ConsoleLogger class
        /// </summary>
        /// <param name="minimumLevel">Minimum log level to display</param>
        /// <param name="includeTimestamps">Whether to include timestamps in log messages</param>
        public ConsoleLogger(LogLevel minimumLevel = LogLevel.Information, bool includeTimestamps = true)
        {
            _minimumLevel = minimumLevel;
            _includeTimestamps = includeTimestamps;
        }

        /// <summary>
        /// Logs a message with the specified log level
        /// </summary>
        /// <param name="level">Severity level of the log</param>
        /// <param name="message">Message to log</param>
        public void Log(LogLevel level, string message)
        {
            if (level < _minimumLevel)
            {
                return;
            }

            var timestamp = _includeTimestamps ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] " : "";
            var logLevel = $"[{level}] ";
            var formattedMessage = $"{timestamp}{logLevel}{message}";

            switch (level)
            {
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }

            Console.WriteLine(formattedMessage);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an error message with exception details
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception that occurred</param>
        public void LogError(string message, Exception exception)
        {
            if (LogLevel.Error < _minimumLevel)
            {
                return;
            }

            var timestamp = _includeTimestamps ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] " : "";
            var logLevel = $"[{LogLevel.Error}] ";
            var formattedMessage = $"{timestamp}{logLevel}{message}";
            var exceptionDetails = $"Exception: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(formattedMessage);
            Console.WriteLine(exceptionDetails);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);
        }
    }
}
