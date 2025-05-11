using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Aigent.Api.Interfaces
{
    /// <summary>
    /// Interface for API analytics service
    /// </summary>
    public interface IApiAnalyticsService
    {
        /// <summary>
        /// Records an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Request tracking ID</returns>
        Task<string> RecordRequestAsync(HttpContext context);
        
        /// <summary>
        /// Records an API response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="trackingId">Request tracking ID</param>
        /// <param name="elapsedTime">Elapsed time in milliseconds</param>
        /// <returns>Completed tracking task</returns>
        Task RecordResponseAsync(HttpContext context, string trackingId, long elapsedTime);
        
        /// <summary>
        /// Gets API usage metrics for a time period
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>API usage metrics</returns>
        Task<Dictionary<string, object>> GetUsageMetricsAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets API performance metrics for a time period
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>API performance metrics</returns>
        Task<Dictionary<string, object>> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets API error metrics for a time period
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>API error metrics</returns>
        Task<Dictionary<string, object>> GetErrorMetricsAsync(DateTime startDate, DateTime endDate);
    }
}
