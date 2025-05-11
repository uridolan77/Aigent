using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Aigent.Monitoring;
using Aigent.Api.Interfaces;

namespace Aigent.Api.Services
{
    /// <summary>
    /// Implementation of IApiAnalyticsService
    /// </summary>
    public class ApiAnalyticsService : IApiAnalyticsService
    {
        private readonly ConcurrentQueue<RequestRecord> _requestRecords = new();
        private readonly ILogger _logger;
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30);

        /// <summary>
        /// Initializes a new instance of the ApiAnalyticsService class
        /// </summary>
        /// <param name="logger">Logger for recording analytics activities</param>
        public ApiAnalyticsService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Set up cleanup timer to remove old records
            _cleanupTimer = new Timer(CleanupOldRecords, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Records an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Request tracking ID</returns>
        public Task<string> RecordRequestAsync(HttpContext context)
        {
            var trackingId = Guid.NewGuid().ToString();
            context.Items["RequestStartTime"] = DateTime.UtcNow;
            context.Items["RequestTrackingId"] = trackingId;
            
            _logger.Log(LogLevel.Debug, $"Started tracking request: {trackingId}");
            
            return Task.FromResult(trackingId);
        }
        
        /// <summary>
        /// Records an API response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="trackingId">Request tracking ID</param>
        /// <param name="elapsedTime">Elapsed time in milliseconds</param>
        /// <returns>Completed tracking task</returns>
        public Task RecordResponseAsync(HttpContext context, string trackingId, long elapsedTime)
        {
            var record = new RequestRecord
            {
                TrackingId = trackingId,
                Timestamp = DateTime.UtcNow,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                StatusCode = context.Response.StatusCode,
                DurationMs = elapsedTime,
                UserId = context.User?.FindFirst("sub")?.Value,
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                ContentType = context.Response.ContentType,
                ContentLength = context.Response.ContentLength
            };
            
            _requestRecords.Enqueue(record);
            
            _logger.Log(LogLevel.Debug, $"Recorded API request: {record.Method} {record.Path} {record.StatusCode} {record.DurationMs}ms");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets API usage metrics for a time period
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>API usage metrics</returns>
        public Task<Dictionary<string, object>> GetUsageMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var relevantRecords = _requestRecords.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate).ToList();
            
            var metrics = new Dictionary<string, object>
            {
                ["totalRequests"] = relevantRecords.Count,
                ["successfulRequests"] = relevantRecords.Count(r => r.StatusCode >= 200 && r.StatusCode < 300),
                ["clientErrorRequests"] = relevantRecords.Count(r => r.StatusCode >= 400 && r.StatusCode < 500),
                ["serverErrorRequests"] = relevantRecords.Count(r => r.StatusCode >= 500),
                ["averageResponseTimeMs"] = relevantRecords.Any() ? relevantRecords.Average(r => r.DurationMs) : 0,
                ["uniqueUsers"] = relevantRecords.Where(r => r.UserId != null).Select(r => r.UserId).Distinct().Count(),
                ["uniqueIpAddresses"] = relevantRecords.Where(r => r.ClientIp != null).Select(r => r.ClientIp).Distinct().Count(),
                ["requestsByMethod"] = relevantRecords.GroupBy(r => r.Method)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["requestsByStatusCode"] = relevantRecords.GroupBy(r => r.StatusCode)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };
            
            return Task.FromResult(metrics);
        }
        
        /// <summary>
        /// Gets API performance metrics for a time period
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>API performance metrics</returns>
        public Task<Dictionary<string, object>> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var relevantRecords = _requestRecords.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate).ToList();
            
            var endpoints = relevantRecords
                .GroupBy(r => new { r.Method, r.Path })
                .Select(g => new 
                {
                    Endpoint = $"{g.Key.Method} {g.Key.Path}",
                    RequestCount = g.Count(),
                    SuccessRate = g.Count(r => r.StatusCode >= 200 && r.StatusCode < 300) * 100.0 / g.Count(),
                    AverageResponseTimeMs = g.Average(r => r.DurationMs),
                    MaxResponseTimeMs = g.Max(r => r.DurationMs),
                    MinResponseTimeMs = g.Min(r => r.DurationMs)
                })
                .OrderByDescending(e => e.RequestCount)
                .Take(10)
                .ToList();
            
            var metrics = new Dictionary<string, object>
            {
                ["overallAverageResponseTimeMs"] = relevantRecords.Any() ? relevantRecords.Average(r => r.DurationMs) : 0,
                ["medianResponseTimeMs"] = GetMedian(relevantRecords.Select(r => r.DurationMs).ToList()),
                ["p95ResponseTimeMs"] = GetPercentile(relevantRecords.Select(r => r.DurationMs).ToList(), 95),
                ["p99ResponseTimeMs"] = GetPercentile(relevantRecords.Select(r => r.DurationMs).ToList(), 99),
                ["maxResponseTimeMs"] = relevantRecords.Any() ? relevantRecords.Max(r => r.DurationMs) : 0,
                ["minResponseTimeMs"] = relevantRecords.Any() ? relevantRecords.Min(r => r.DurationMs) : 0,
                ["slowestEndpoints"] = endpoints
            };
            
            return Task.FromResult(metrics);
        }
        
        /// <summary>
        /// Gets API error metrics for a time period
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>API error metrics</returns>
        public Task<Dictionary<string, object>> GetErrorMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var relevantRecords = _requestRecords.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate).ToList();
            
            var errorRecords = relevantRecords.Where(r => r.StatusCode >= 400).ToList();
            
            var errorsByEndpoint = errorRecords
                .GroupBy(r => new { r.Method, r.Path })
                .Select(g => new 
                {
                    Endpoint = $"{g.Key.Method} {g.Key.Path}",
                    ErrorCount = g.Count(),
                    ErrorRate = g.Count() * 100.0 / (relevantRecords.Count(r => r.Method == g.Key.Method && r.Path == g.Key.Path)),
                    StatusCodes = g.GroupBy(r => r.StatusCode)
                        .Select(sg => new 
                        {
                            StatusCode = sg.Key,
                            Count = sg.Count()
                        })
                        .OrderByDescending(s => s.Count)
                        .ToList()
                })
                .OrderByDescending(e => e.ErrorCount)
                .Take(10)
                .ToList();
            
            var metrics = new Dictionary<string, object>
            {
                ["totalErrors"] = errorRecords.Count,
                ["errorRate"] = relevantRecords.Any() ? errorRecords.Count * 100.0 / relevantRecords.Count : 0,
                ["clientErrors"] = errorRecords.Count(r => r.StatusCode >= 400 && r.StatusCode < 500),
                ["serverErrors"] = errorRecords.Count(r => r.StatusCode >= 500),
                ["errorsByStatusCode"] = errorRecords.GroupBy(r => r.StatusCode)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ["errorsByEndpoint"] = errorsByEndpoint
            };
            
            return Task.FromResult(metrics);
        }

        private void CleanupOldRecords(object state)
        {
            try
            {
                var cutoff = DateTime.UtcNow - _retentionPeriod;
                var recordsToKeep = new ConcurrentQueue<RequestRecord>();
                
                while (_requestRecords.TryDequeue(out var record))
                {
                    if (record.Timestamp >= cutoff)
                    {
                        recordsToKeep.Enqueue(record);
                    }
                }
                
                // Swap the queues
                var oldCount = _requestRecords.Count;
                while (recordsToKeep.TryDequeue(out var record))
                {
                    _requestRecords.Enqueue(record);
                }
                
                _logger.Log(LogLevel.Information, $"Cleaned up API analytics records: removed {oldCount - _requestRecords.Count} old records");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up API analytics records: {ex.Message}", ex);
            }
        }

        private static double GetMedian(List<double> values)
        {
            if (values == null || !values.Any())
                return 0;
                
            var sortedValues = values.OrderBy(v => v).ToList();
            var count = sortedValues.Count;
            
            if (count % 2 == 0)
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
                
            return sortedValues[count / 2];
        }

        private static double GetPercentile(List<double> values, int percentile)
        {
            if (values == null || !values.Any())
                return 0;
                
            var sortedValues = values.OrderBy(v => v).ToList();
            var count = sortedValues.Count;
            
            var index = (int)Math.Ceiling(percentile / 100.0 * count) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, count - 1))];
        }

        private class RequestRecord
        {
            public string TrackingId { get; set; }
            public DateTime Timestamp { get; set; }
            public string Method { get; set; }
            public string Path { get; set; }
            public string QueryString { get; set; }
            public int StatusCode { get; set; }
            public double DurationMs { get; set; }
            public string UserId { get; set; }
            public string ClientIp { get; set; }
            public string UserAgent { get; set; }
            public string ContentType { get; set; }
            public long? ContentLength { get; set; }
        }
    }
}
