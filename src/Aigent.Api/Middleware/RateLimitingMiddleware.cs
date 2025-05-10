using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Aigent.Monitoring;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ILogger = Aigent.Monitoring.ILogger;
using LogLevel = Aigent.Monitoring.LogLevel;

namespace Aigent.Api.Middleware
{
    /// <summary>
    /// Middleware for rate limiting
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();

        /// <summary>
        /// Initializes a new instance of the RateLimitingMiddleware class
        /// </summary>
        /// <param name="next">Next middleware in the pipeline</param>
        /// <param name="logger">Logger</param>
        /// <param name="configuration">Configuration</param>
        public RateLimitingMiddleware(RequestDelegate next, ILogger logger, IConfiguration configuration)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.Value;
            var endpoint = $"{context.Request.Method} {path}";
            var clientId = context.User.Identity?.IsAuthenticated == true
                ? context.User.Identity.Name
                : clientIp;

            var key = $"{clientId}:{endpoint}";

            // Get rate limit settings
            var requestsPerMinute = GetRateLimit(path);

            if (requestsPerMinute > 0)
            {
                var rateLimitInfo = _rateLimits.GetOrAdd(key, _ => new RateLimitInfo());

                // Check if rate limit is exceeded
                if (IsRateLimitExceeded(rateLimitInfo, requestsPerMinute))
                {
                    _logger.Log(LogLevel.Warning, $"Rate limit exceeded for {clientId} on {endpoint}");

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "Rate limit exceeded",
                        retryAfter = (int)(rateLimitInfo.ResetTime - DateTime.UtcNow).TotalSeconds
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Add rate limit headers
                context.Response.Headers.Add("X-RateLimit-Limit", requestsPerMinute.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", (requestsPerMinute - rateLimitInfo.RequestCount).ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", ((int)(rateLimitInfo.ResetTime - DateTime.UtcNow).TotalSeconds).ToString());
            }

            await _next(context);
        }

        private int GetRateLimit(string path)
        {
            // Default rate limits
            var defaultLimit = _configuration.GetValue<int>("RateLimiting:DefaultLimit", 60);

            // Endpoint-specific rate limits
            if (path.StartsWith("/api/agents"))
            {
                return _configuration.GetValue<int>("RateLimiting:AgentsLimit", 30);
            }
            else if (path.StartsWith("/api/memory"))
            {
                return _configuration.GetValue<int>("RateLimiting:MemoryLimit", 20);
            }
            else if (path.StartsWith("/api/workflows"))
            {
                return _configuration.GetValue<int>("RateLimiting:WorkflowsLimit", 10);
            }

            return defaultLimit;
        }

        private bool IsRateLimitExceeded(RateLimitInfo rateLimitInfo, int requestsPerMinute)
        {
            var now = DateTime.UtcNow;

            // Reset counter if the time window has passed
            if (now > rateLimitInfo.ResetTime)
            {
                rateLimitInfo.RequestCount = 0;
                rateLimitInfo.ResetTime = now.AddMinutes(1);
            }

            // Increment request count
            rateLimitInfo.RequestCount++;

            // Check if limit is exceeded
            return rateLimitInfo.RequestCount > requestsPerMinute;
        }

        private class RateLimitInfo
        {
            public int RequestCount { get; set; }
            public DateTime ResetTime { get; set; } = DateTime.UtcNow.AddMinutes(1);
        }
    }
}
