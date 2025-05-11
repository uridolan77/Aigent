using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Aigent.Monitoring.Metrics;
using Aigent.Monitoring.Logging;
using Moq;

namespace Aigent.Monitoring.Tests.Metrics
{
    public class BasicMetricsCollectorTests
    {
        [Fact]
        public async Task GetSummary_ReturnsCorrectMetricData()
        {
            // Arrange
            var logger = new Mock<ILogger>();
            var collector = new BasicMetricsCollector(logger.Object);
            
            collector.RecordMetric("test_metric", 10);
            collector.RecordMetric("test_metric", 20);
            collector.RecordMetric("test_metric", 30);
            
            // Act
            var summary = await collector.GetSummary(TimeSpan.FromMinutes(5));
            
            // Assert
            Assert.True(summary.Metrics.ContainsKey("test_metric"));
            var metric = summary.Metrics["test_metric"];
            Assert.Equal("test_metric", metric.Name);
            Assert.Equal(3, metric.Count);
            Assert.Equal(60, metric.Sum);
            Assert.Equal(10, metric.Min);
            Assert.Equal(30, metric.Max);
            Assert.Equal(20, metric.Average);
        }
        
        [Fact]
        public async Task GetSummary_WithTags_ProcessesTagsCorrectly()
        {
            // Arrange
            var collector = new BasicMetricsCollector();
            
            collector.RecordMetric("api_calls", 10, new Dictionary<string, string> { { "endpoint", "users" } });
            collector.RecordMetric("api_calls", 20, new Dictionary<string, string> { { "endpoint", "products" } });
            collector.RecordMetric("api_calls", 15, new Dictionary<string, string> { { "endpoint", "users" } });
            
            // Act
            var summary = await collector.GetSummary(TimeSpan.FromMinutes(5));
            
            // Assert
            Assert.True(summary.Metrics.ContainsKey("api_calls"));
            var metric = summary.Metrics["api_calls"];
            
            Assert.Equal(3, metric.Count);
            Assert.Equal(45, metric.Sum);
            
            Assert.True(metric.Tags.ContainsKey("endpoint"));
            Assert.Equal(2, metric.Tags["endpoint"].Count);
            Assert.Contains("users", metric.Tags["endpoint"]);
            Assert.Contains("products", metric.Tags["endpoint"]);
        }
        
        [Fact]
        public void StartOperation_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var collector = new BasicMetricsCollector();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => collector.StartOperation(null));
        }
        
        [Fact]
        public void EndOperation_WithNonExistentOperation_LogsWarning()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            var collector = new BasicMetricsCollector(loggerMock.Object);
            
            // Act
            collector.EndOperation("non_existent_operation");
            
            // Assert
            loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<string>()));
        }
        
        [Fact]
        public async Task StartAndEndOperation_RecordsOperationDuration()
        {
            // Arrange
            var collector = new BasicMetricsCollector();
            var operationName = "test_operation";
            
            // Act
            collector.StartOperation(operationName);
            await Task.Delay(10); // Wait a bit to ensure measurable duration
            collector.EndOperation(operationName);
            
            // Let's get the summary
            var summary = await collector.GetSummary(TimeSpan.FromMinutes(1));
            
            // Assert
            var metricName = $"operation.{operationName}.duration_ms";
            Assert.True(summary.Metrics.ContainsKey(metricName));
            Assert.Equal(1, summary.Metrics[metricName].Count);
            Assert.True(summary.Metrics[metricName].Min >= 10); // At least 10ms
        }
    }
}
