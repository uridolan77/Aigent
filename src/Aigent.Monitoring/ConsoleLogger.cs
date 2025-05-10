using System;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Console logger implementation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        public void Log(LogLevel level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var color = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
            
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            
            Console.WriteLine($"[{timestamp}] [{level}] {message}");
            
            Console.ForegroundColor = originalColor;
        }
        
        /// <summary>
        /// Logs an error message with exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception</param>
        public void LogError(string message, Exception exception)
        {
            Log(LogLevel.Error, message);
            
            if (exception != null)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                
                Console.WriteLine($"Exception: {exception.GetType().Name}");
                Console.WriteLine($"Message: {exception.Message}");
                Console.WriteLine($"StackTrace: {exception.StackTrace}");
                
                if (exception.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {exception.InnerException.GetType().Name}");
                    Console.WriteLine($"Inner Message: {exception.InnerException.Message}");
                }
                
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
