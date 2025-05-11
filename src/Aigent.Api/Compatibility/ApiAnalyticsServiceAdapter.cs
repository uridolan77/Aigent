using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Aigent.Api.Interfaces;

namespace Aigent.Api.Compatibility
{
    /// <summary>
    /// Adapter for IApiAnalyticsService to legacy Analytics.IApiAnalyticsService
    /// </summary>
    public class ApiAnalyticsServiceAdapter : Analytics.IApiAnalyticsService
    {
        private readonly IApiAnalyticsService _apiAnalyticsService;

        /// <summary>
        /// Initializes a new instance of the ApiAnalyticsServiceAdapter class
        /// </summary>
        /// <param name="apiAnalyticsService">API analytics service</param>
        public ApiAnalyticsServiceAdapter(IApiAnalyticsService apiAnalyticsService)
        {
            _apiAnalyticsService = apiAnalyticsService ?? throw new ArgumentNullException(nameof(apiAnalyticsService));
        }

        /// <summary>
        /// Records an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="durationMs">Duration of the request in milliseconds</param>
        public void RecordRequest(HttpContext context, double durationMs)
        {
            var trackingId = Guid.NewGuid().ToString();
            _apiAnalyticsService.RecordResponseAsync(context, trackingId, (long)durationMs).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Gets API usage statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>API usage statistics</returns>
        public Task<Analytics.ApiUsageStatistics> GetUsageStatisticsAsync(TimeSpan timeSpan)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate - timeSpan;
            
            var metricsTask = _apiAnalyticsService.GetUsageMetricsAsync(startDate, endDate);
            var metrics = metricsTask.GetAwaiter().GetResult();
            
            return Task.FromResult(new Analytics.ApiUsageStatistics
            {
                TotalRequests = (int)metrics["totalRequests"],
                SuccessfulRequests = (int)metrics["successfulRequests"],
                ClientErrorRequests = (int)metrics["clientErrorRequests"],
                ServerErrorRequests = (int)metrics["serverErrorRequests"],
                AverageResponseTimeMs = (double)metrics["averageResponseTimeMs"],
                UniqueUsers = (int)metrics["uniqueUsers"],
                UniqueIpAddresses = (int)metrics["uniqueIpAddresses"]
            });
        }
        
        /// <summary>
        /// Gets endpoint statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>Endpoint statistics</returns>
        public Task<System.Collections.Generic.List<Analytics.EndpointStatistics>> GetEndpointStatisticsAsync(TimeSpan timeSpan)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate - timeSpan;
            
            // This is a simplified implementation
            var metricsTask = _apiAnalyticsService.GetPerformanceMetricsAsync(startDate, endDate);
            var metrics = metricsTask.GetAwaiter().GetResult();
            
            var slowestEndpoints = metrics["slowestEndpoints"] as System.Collections.Generic.List<dynamic>;
            var result = new System.Collections.Generic.List<Analytics.EndpointStatistics>();
            
            foreach (var endpoint in slowestEndpoints)
            {
                var parts = endpoint.Endpoint.ToString().Split(' ');
                result.Add(new Analytics.EndpointStatistics
                {
                    Method = parts[0],
                    Path = parts[1],
                    RequestCount = endpoint.RequestCount,
                    SuccessRate = endpoint.SuccessRate,
                    AverageResponseTimeMs = endpoint.AverageResponseTimeMs,
                    LastRequested = DateTime.UtcNow // We don't have this information
                });
            }
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Gets user statistics
        /// </summary>
        /// <param name="timeSpan">Time span to get statistics for</param>
        /// <returns>User statistics</returns>
        public Task<System.Collections.Generic.List<Analytics.UserStatistics>> GetUserStatisticsAsync(TimeSpan timeSpan)
        {
            // This is a simplified implementation that returns empty results
            // In a real implementation, we would extract this data from metrics
            return Task.FromResult(new System.Collections.Generic.List<Analytics.UserStatistics>());
        }
    }
}
