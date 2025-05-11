using System;
using Microsoft.Extensions.DependencyInjection;
using Aigent.Monitoring.Logging;
using Aigent.Monitoring.Metrics;

namespace Aigent.Monitoring
{
    /// <summary>
    /// Extension methods for registering monitoring services
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds monitoring services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="useConsoleLogger">Whether to use the console logger</param>
        /// <param name="minimumLogLevel">Minimum log level to display</param>
        /// <param name="metricsCollectorType">Type of metrics collector to use</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddMonitoring(
            this IServiceCollection services,
            bool useConsoleLogger = true,
            LogLevel minimumLogLevel = LogLevel.Information,
            MetricsCollectorType metricsCollectorType = MetricsCollectorType.InMemory)
        {
            // Register logger
            if (useConsoleLogger)
            {
                services.AddSingleton<ILogger>(new ConsoleLogger(minimumLogLevel));
            }

            // Register metrics collector
            switch (metricsCollectorType)
            {
                case MetricsCollectorType.Basic:
                    services.AddSingleton<IMetricsCollector>(provider =>
                        new BasicMetricsCollector(provider.GetService<ILogger>()));
                    break;
                case MetricsCollectorType.InMemory:
                default:
                    services.AddSingleton<IMetricsCollector>(provider =>
                        new InMemoryMetricsCollector(provider.GetService<ILogger>()));
                    break;
            }

            // Legacy registrations
            services.AddSingleton<Aigent.Monitoring.ILogger>(provider =>
                provider.GetRequiredService<ILogger>());
            services.AddSingleton<Aigent.Monitoring.IMetricsCollector>(provider =>
                provider.GetRequiredService<IMetricsCollector>());

            return services;
        }
    }

    /// <summary>
    /// Types of metrics collectors
    /// </summary>
    public enum MetricsCollectorType
    {
        /// <summary>
        /// Basic metrics collector
        /// </summary>
        Basic,

        /// <summary>
        /// In-memory metrics collector
        /// </summary>
        InMemory
    }
}
