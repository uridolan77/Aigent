using System;
using System.IO;
using Xunit;
using Aigent.Monitoring.Logging;

namespace Aigent.Monitoring.Tests.Logging
{
    public class ConsoleLoggerTests
    {
        [Fact]
        public void Log_WithDebugLevel_WritesToConsole()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
            
            var logger = new ConsoleLogger(LogLevel.Debug, includeTimestamps: false);
            
            // Act
            logger.LogDebug("Test debug message");
            
            // Assert
            var output = consoleOutput.ToString().Trim();
            Assert.Contains("[Debug] Test debug message", output);
        }
        
        [Fact]
        public void Log_WithLevelBelowMinimum_DoesNotWriteToConsole()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
            
            var logger = new ConsoleLogger(LogLevel.Warning, includeTimestamps: false);
            
            // Act
            logger.LogDebug("Test debug message");
            logger.LogInformation("Test info message");
            
            // Assert
            var output = consoleOutput.ToString();
            Assert.Equal("", output);
        }
        
        [Fact]
        public void LogError_WithException_WritesExceptionDetails()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
            
            var logger = new ConsoleLogger(LogLevel.Error, includeTimestamps: false);
            var exception = new InvalidOperationException("Test exception");
            
            // Act
            logger.LogError("An error occurred", exception);
              // Assert
            var output = consoleOutput.ToString();
            Assert.Contains("[Error] An error occurred", output);
            Assert.Contains("Exception: InvalidOperationException", output);
            Assert.Contains("Message: Test exception", output);
        }
        
        [Fact]
        public void LogCritical_WithException_WritesExceptionDetails()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
            
            var logger = new ConsoleLogger(LogLevel.Critical, includeTimestamps: false);
            var exception = new InvalidOperationException("Test critical exception");
            
            // Act
            logger.LogCritical("A critical error occurred", exception);
            
            // Assert
            var output = consoleOutput.ToString();
            Assert.Contains("[Critical] A critical error occurred", output);
            Assert.Contains("Critical Exception: InvalidOperationException", output);
            Assert.Contains("Message: Test critical exception", output);
        }
    }
}
