using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Aigent.Monitoring;

namespace Aigent.Api.Analytics
{
    /// <summary>
    /// Service for tracking API analytics
    /// </summary>
    public interface IApiAnalyticsService
    {
        /// <summary>
        /// Records an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="durationMs">Duration of the request in milliseconds</param>
        void RecordRequest(HttpContext context, double durationMs);
        
        /// <summary>
        /// Gets API usage statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>API usage statistics</returns>
        Task<ApiUsageStatistics> GetUsageStatisticsAsync(TimeSpan timeSpan);
        
        /// <summary>
        /// Gets endpoint statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>Endpoint statistics</returns>
        Task<List<EndpointStatistics>> GetEndpointStatisticsAsync(TimeSpan timeSpan);
        
        /// <summary>
        /// Gets user statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>User statistics</returns>
        Task<List<UserStatistics>> GetUserStatisticsAsync(TimeSpan timeSpan);
    }

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
            _logger = logger;
            
            // Set up cleanup timer to remove old records
            _cleanupTimer = new Timer(CleanupOldRecords, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Records an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="durationMs">Duration of the request in milliseconds</param>
        public void RecordRequest(HttpContext context, double durationMs)
        {
            var record = new RequestRecord
            {
                Timestamp = DateTime.UtcNow,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                StatusCode = context.Response.StatusCode,
                DurationMs = durationMs,
                UserId = context.User?.FindFirst("sub")?.Value,
                ClientIp = context.Connection.RemoteIpAddress?.ToString()
            };
            
            _requestRecords.Enqueue(record);
            _logger.Log(LogLevel.Debug, $"Recorded API request: {record.Method} {record.Path} {record.StatusCode} {record.DurationMs}ms");
        }

        /// <summary>
        /// Gets API usage statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>API usage statistics</returns>
        public Task<ApiUsageStatistics> GetUsageStatisticsAsync(TimeSpan timeSpan)
        {
            var cutoff = DateTime.UtcNow - timeSpan;
            var relevantRecords = _requestRecords.Where(r => r.Timestamp >= cutoff).ToList();
            
            var statistics = new ApiUsageStatistics
            {
                TotalRequests = relevantRecords.Count,
                SuccessfulRequests = relevantRecords.Count(r => r.StatusCode >= 200 && r.StatusCode < 300),
                ClientErrorRequests = relevantRecords.Count(r => r.StatusCode >= 400 && r.StatusCode < 500),
                ServerErrorRequests = relevantRecords.Count(r => r.StatusCode >= 500),
                AverageResponseTimeMs = relevantRecords.Any() ? relevantRecords.Average(r => r.DurationMs) : 0,
                UniqueUsers = relevantRecords.Where(r => r.UserId != null).Select(r => r.UserId).Distinct().Count(),
                UniqueIpAddresses = relevantRecords.Where(r => r.ClientIp != null).Select(r => r.ClientIp).Distinct().Count()
            };
            
            return Task.FromResult(statistics);
        }

        /// <summary>
        /// Gets endpoint statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>Endpoint statistics</returns>
        public Task<List<EndpointStatistics>> GetEndpointStatisticsAsync(TimeSpan timeSpan)
        {
            var cutoff = DateTime.UtcNow - timeSpan;
            var relevantRecords = _requestRecords.Where(r => r.Timestamp >= cutoff).ToList();
            
            var endpointGroups = relevantRecords
                .GroupBy(r => new { r.Method, r.Path })
                .Select(g => new EndpointStatistics
                {
                    Method = g.Key.Method,
                    Path = g.Key.Path,
                    RequestCount = g.Count(),
                    SuccessRate = g.Count(r => r.StatusCode >= 200 && r.StatusCode < 300) * 100.0 / g.Count(),
                    AverageResponseTimeMs = g.Average(r => r.DurationMs),
                    LastRequested = g.Max(r => r.Timestamp)
                })
                .OrderByDescending(s => s.RequestCount)
                .ToList();
            
            return Task.FromResult(endpointGroups);
        }

        /// <summary>
        /// Gets user statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>User statistics</returns>
        public Task<List<UserStatistics>> GetUserStatisticsAsync(TimeSpan timeSpan)
        {
            var cutoff = DateTime.UtcNow - timeSpan;
            var relevantRecords = _requestRecords.Where(r => r.Timestamp >= cutoff && r.UserId != null).ToList();
            
            var userGroups = relevantRecords
                .GroupBy(r => r.UserId)
                .Select(g => new UserStatistics
                {
                    UserId = g.Key,
                    RequestCount = g.Count(),
                    AverageResponseTimeMs = g.Average(r => r.DurationMs),
                    LastActive = g.Max(r => r.Timestamp),
                    MostUsedEndpoint = g.GroupBy(r => r.Path)
                        .OrderByDescending(eg => eg.Count())
                        .First().Key
                })
                .OrderByDescending(s => s.RequestCount)
                .ToList();
            
            return Task.FromResult(userGroups);
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
    }

    /// <summary>
    /// Record of an API request
    /// </summary>
    public class RequestRecord
    {
        /// <summary>
        /// When the request was made
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// HTTP method of the request
        /// </summary>
        public string Method { get; set; }
        
        /// <summary>
        /// Path of the request
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Status code of the response
        /// </summary>
        public int StatusCode { get; set; }
        
        /// <summary>
        /// Duration of the request in milliseconds
        /// </summary>
        public double DurationMs { get; set; }
        
        /// <summary>
        /// ID of the user who made the request
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// IP address of the client
        /// </summary>
        public string ClientIp { get; set; }
    }

    /// <summary>
    /// API usage statistics
    /// </summary>
    public class ApiUsageStatistics
    {
        /// <summary>
        /// Total number of requests
        /// </summary>
        public int TotalRequests { get; set; }
        
        /// <summary>
        /// Number of successful requests (2xx)
        /// </summary>
        public int SuccessfulRequests { get; set; }
        
        /// <summary>
        /// Number of client error requests (4xx)
        /// </summary>
        public int ClientErrorRequests { get; set; }
        
        /// <summary>
        /// Number of server error requests (5xx)
        /// </summary>
        public int ServerErrorRequests { get; set; }
        
        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }
        
        /// <summary>
        /// Number of unique users
        /// </summary>
        public int UniqueUsers { get; set; }
        
        /// <summary>
        /// Number of unique IP addresses
        /// </summary>
        public int UniqueIpAddresses { get; set; }
    }

    /// <summary>
    /// Endpoint statistics
    /// </summary>
    public class EndpointStatistics
    {
        /// <summary>
        /// HTTP method of the endpoint
        /// </summary>
        public string Method { get; set; }
        
        /// <summary>
        /// Path of the endpoint
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Number of requests to the endpoint
        /// </summary>
        public int RequestCount { get; set; }
        
        /// <summary>
        /// Success rate of requests to the endpoint (percentage)
        /// </summary>
        public double SuccessRate { get; set; }
        
        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }
        
        /// <summary>
        /// When the endpoint was last requested
        /// </summary>
        public DateTime LastRequested { get; set; }
    }

    /// <summary>
    /// User statistics
    /// </summary>
    public class UserStatistics
    {
        /// <summary>
        /// ID of the user
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Number of requests made by the user
        /// </summary>
        public int RequestCount { get; set; }
        
        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }
        
        /// <summary>
        /// When the user was last active
        /// </summary>
        public DateTime LastActive { get; set; }
        
        /// <summary>
        /// Most used endpoint by the user
        /// </summary>
        public string MostUsedEndpoint { get; set; }
    }
}
