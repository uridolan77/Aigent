using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aigent.Monitoring;
using Microsoft.AspNetCore.Http;
using ILogger = Aigent.Monitoring.ILogger;
using LogLevel = Aigent.Monitoring.LogLevel;

namespace Aigent.Api.Middleware
{
    /// <summary>
    /// Middleware for API analytics
    /// </summary>
    public class ApiAnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;

        /// <summary>
        /// Initializes a new instance of the ApiAnalyticsMiddleware class
        /// </summary>
        /// <param name="next">Next middleware in the pipeline</param>
        /// <param name="logger">Logger</param>
        /// <param name="metrics">Metrics collector</param>
        public ApiAnalyticsMiddleware(RequestDelegate next, ILogger logger, IMetricsCollector metrics)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path.Value;
            var method = context.Request.Method;
            var endpoint = $"{method} {path}";

            _metrics.StartOperation(endpoint);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var duration = stopwatch.Elapsed.TotalMilliseconds;

                _metrics.EndOperation(endpoint);
                _metrics.RecordMetric($"api_request_duration_{statusCode}", duration);

                _logger.Log(LogLevel.Information, $"API Request: {endpoint} - Status: {statusCode} - Duration: {duration}ms");

                // Record metrics by status code category
                var category = statusCode / 100;
                _metrics.RecordMetric($"api_requests_{category}xx", 1);

                // Record metrics for specific endpoints
                if (path.StartsWith("/api/agents"))
                {
                    _metrics.RecordMetric("api_agent_requests", 1);
                }
                else if (path.StartsWith("/api/memory"))
                {
                    _metrics.RecordMetric("api_memory_requests", 1);
                }
                else if (path.StartsWith("/api/workflows"))
                {
                    _metrics.RecordMetric("api_workflow_requests", 1);
                }
            }
        }
    }
}
