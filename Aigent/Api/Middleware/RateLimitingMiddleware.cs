using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Aigent.Monitoring;

namespace Aigent.Api.Middleware
{
    /// <summary>
    /// Middleware for rate limiting requests
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
        /// <param name="logger">Logger for recording rate limiting activities</param>
        /// <param name="configuration">Configuration for rate limiting</param>
        public RateLimitingMiddleware(RequestDelegate next, ILogger logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">HTTP context</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Get client identifier (use API key or IP address)
            var clientId = GetClientIdentifier(context);
            
            // Get rate limit for the endpoint
            var endpoint = context.Request.Path.Value?.ToLowerInvariant();
            var rateLimit = GetRateLimit(endpoint);
            
            if (rateLimit > 0)
            {
                var key = $"{clientId}:{endpoint}";
                var rateLimitInfo = _rateLimits.GetOrAdd(key, _ => new RateLimitInfo());
                
                // Check if rate limit is exceeded
                if (IsRateLimitExceeded(rateLimitInfo, rateLimit))
                {
                    _logger.Log(LogLevel.Warning, $"Rate limit exceeded for client {clientId} on endpoint {endpoint}");
                    
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers.Add("Retry-After", "60");
                    
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }
                
                // Add rate limit headers
                AddRateLimitHeaders(context, rateLimitInfo, rateLimit);
            }
            
            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Try to get API key from header
            if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
            {
                return apiKey;
            }
            
            // Fall back to IP address
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private int GetRateLimit(string endpoint)
        {
            // Get rate limit from configuration based on endpoint
            if (endpoint != null)
            {
                // Check for specific endpoint configuration
                var specificLimit = _configuration[$"Aigent:Api:RateLimits:{endpoint}"];
                if (!string.IsNullOrEmpty(specificLimit) && int.TryParse(specificLimit, out var limit))
                {
                    return limit;
                }
                
                // Check for endpoint pattern configuration
                foreach (var section in _configuration.GetSection("Aigent:Api:RateLimits").GetChildren())
                {
                    var pattern = section.Key;
                    if (pattern.EndsWith("*") && endpoint.StartsWith(pattern.TrimEnd('*')))
                    {
                        if (int.TryParse(section.Value, out var patternLimit))
                        {
                            return patternLimit;
                        }
                    }
                }
            }
            
            // Get default rate limit
            var defaultLimit = _configuration["Aigent:Api:RateLimits:Default"];
            if (!string.IsNullOrEmpty(defaultLimit) && int.TryParse(defaultLimit, out var defaultLimitValue))
            {
                return defaultLimitValue;
            }
            
            // Default to no rate limit
            return 0;
        }

        private bool IsRateLimitExceeded(RateLimitInfo rateLimitInfo, int rateLimit)
        {
            var now = DateTime.UtcNow;
            
            // Reset counter if window has passed
            if ((now - rateLimitInfo.WindowStart).TotalMinutes >= 1)
            {
                rateLimitInfo.WindowStart = now;
                rateLimitInfo.Counter = 0;
            }
            
            // Increment counter
            rateLimitInfo.Counter++;
            
            // Check if limit is exceeded
            return rateLimitInfo.Counter > rateLimit;
        }

        private void AddRateLimitHeaders(HttpContext context, RateLimitInfo rateLimitInfo, int rateLimit)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Add("X-RateLimit-Limit", rateLimit.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", Math.Max(0, rateLimit - rateLimitInfo.Counter).ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", ((int)(rateLimitInfo.WindowStart.AddMinutes(1) - DateTime.UtcNow).TotalSeconds).ToString());
                
                return Task.CompletedTask;
            });
        }

        private class RateLimitInfo
        {
            public DateTime WindowStart { get; set; } = DateTime.UtcNow;
            public int Counter { get; set; }
        }
    }

    /// <summary>
    /// Extension methods for RateLimitingMiddleware
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        /// <summary>
        /// Adds rate limiting middleware to the application
        /// </summary>
        /// <param name="builder">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
