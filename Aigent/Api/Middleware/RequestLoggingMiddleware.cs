using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Aigent.Monitoring;
using Aigent.Api.Analytics;

namespace Aigent.Api.Middleware
{
    /// <summary>
    /// Middleware for logging requests
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private readonly IApiAnalyticsService _analytics;

        /// <summary>
        /// Initializes a new instance of the RequestLoggingMiddleware class
        /// </summary>
        /// <param name="next">Next middleware in the pipeline</param>
        /// <param name="logger">Logger for recording requests</param>
        /// <param name="metrics">Metrics collector for monitoring request performance</param>
        /// <param name="analytics">API analytics service</param>
        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger logger,
            IMetricsCollector metrics = null,
            IApiAnalyticsService analytics = null)
        {
            _next = next;
            _logger = logger;
            _metrics = metrics;
            _analytics = analytics;
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">HTTP context</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var start = DateTime.UtcNow;
            var requestId = Guid.NewGuid().ToString();

            _metrics?.StartOperation($"request_{requestId}");

            try
            {
                _logger.Log(LogLevel.Information, $"Request {requestId} started: {context.Request.Method} {context.Request.Path}");

                await _next(context);

                var elapsed = DateTime.UtcNow - start;
                _logger.Log(LogLevel.Information, $"Request {requestId} completed: {context.Response.StatusCode} in {elapsed.TotalMilliseconds}ms");

                _metrics?.RecordMetric("api.request.count", 1.0);
                _metrics?.RecordMetric("api.request.duration_ms", elapsed.TotalMilliseconds);
                _metrics?.RecordMetric($"api.status.{context.Response.StatusCode}", 1.0);

                // Record request in analytics
                _analytics?.RecordRequest(context, elapsed.TotalMilliseconds);
            }
            catch
            {
                var elapsed = DateTime.UtcNow - start;
                _logger.Log(LogLevel.Error, $"Request {requestId} failed in {elapsed.TotalMilliseconds}ms");

                _metrics?.RecordMetric("api.request.error_count", 1.0);

                throw;
            }
            finally
            {
                _metrics?.EndOperation($"request_{requestId}");
            }
        }
    }
}
