using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Aigent.Monitoring;
using Aigent.Monitoring.Logging;
using Aigent.Monitoring.Metrics;

namespace Aigent.Monitoring.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddMonitoring_RegistersConsoleLogger_WhenUseConsoleLoggerIsTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMonitoring(useConsoleLogger: true);
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var logger = serviceProvider.GetService<Aigent.Monitoring.Logging.ILogger>();
            Assert.NotNull(logger);
            Assert.IsType<ConsoleLogger>(logger);
        }
        
        [Fact]
        public void AddMonitoring_RegistersInMemoryMetricsCollector_ByDefault()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMonitoring();
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var collector = serviceProvider.GetService<Aigent.Monitoring.Metrics.IMetricsCollector>();
            Assert.NotNull(collector);
            Assert.IsType<InMemoryMetricsCollector>(collector);
        }
        
        [Fact]
        public void AddMonitoring_RegistersBasicMetricsCollector_WhenRequested()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMonitoring(metricsCollectorType: MetricsCollectorType.Basic);
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var collector = serviceProvider.GetService<Aigent.Monitoring.Metrics.IMetricsCollector>();
            Assert.NotNull(collector);
            Assert.IsType<BasicMetricsCollector>(collector);
        }
        
        [Fact]
        public void AddMonitoring_RegistersLegacyInterfaces()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMonitoring();
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var legacyLogger = serviceProvider.GetService<Aigent.Monitoring.ILogger>();
            var legacyCollector = serviceProvider.GetService<Aigent.Monitoring.IMetricsCollector>();
            
            Assert.NotNull(legacyLogger);
            Assert.NotNull(legacyCollector);
        }
    }
}
