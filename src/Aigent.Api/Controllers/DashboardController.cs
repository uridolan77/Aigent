using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aigent.Api.Analytics;
using Aigent.Api.Models;
using Aigent.Monitoring;

namespace Aigent.Api.Controllers
{
    /// <summary>
    /// Controller for the API dashboard
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : ControllerBase
    {
        private readonly IApiAnalyticsService _analytics;
        private readonly IMetricsCollector _metrics;

        /// <summary>
        /// Initializes a new instance of the DashboardController class
        /// </summary>
        /// <param name="analytics">API analytics service</param>
        /// <param name="metrics">Metrics collector</param>
        public DashboardController(IApiAnalyticsService analytics, IMetricsCollector metrics)
        {
            _analytics = analytics;
            _metrics = metrics;
        }

        /// <summary>
        /// Gets API usage statistics
        /// </summary>
        /// <param name="timeSpan">Time span in hours (default: 24)</param>
        /// <returns>API usage statistics</returns>
        [HttpGet("usage")]
        public async Task<ActionResult<ApiResponse<ApiUsageStatistics>>> GetUsageStatistics([FromQuery] int timeSpan = 24)
        {
            var statistics = await _analytics.GetUsageStatisticsAsync(TimeSpan.FromHours(timeSpan));
            return Ok(ApiResponse<ApiUsageStatistics>.Ok(statistics));
        }

        /// <summary>
        /// Gets endpoint statistics
        /// </summary>
        /// <param name="timeSpan">Time span in hours (default: 24)</param>
        /// <returns>Endpoint statistics</returns>
        [HttpGet("endpoints")]
        public async Task<ActionResult<ApiResponse<EndpointStatistics[]>>> GetEndpointStatistics([FromQuery] int timeSpan = 24)
        {
            var statistics = await _analytics.GetEndpointStatisticsAsync(TimeSpan.FromHours(timeSpan));
            return Ok(ApiResponse<EndpointStatistics[]>.Ok(statistics.ToArray()));
        }

        /// <summary>
        /// Gets user statistics
        /// </summary>
        /// <param name="timeSpan">Time span in hours (default: 24)</param>
        /// <returns>User statistics</returns>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<UserStatistics[]>>> GetUserStatistics([FromQuery] int timeSpan = 24)
        {
            var statistics = await _analytics.GetUserStatisticsAsync(TimeSpan.FromHours(timeSpan));
            return Ok(ApiResponse<UserStatistics[]>.Ok(statistics.ToArray()));
        }

        /// <summary>
        /// Gets system metrics
        /// </summary>
        /// <param name="timeSpan">Time span in minutes (default: 60)</param>
        /// <returns>System metrics</returns>
        [HttpGet("metrics")]
        public async Task<ActionResult<ApiResponse<MetricsSummary>>> GetMetrics([FromQuery] int timeSpan = 60)
        {
            var metrics = await _metrics.GetSummary(TimeSpan.FromMinutes(timeSpan));
            return Ok(ApiResponse<MetricsSummary>.Ok(metrics));
        }
    }
}
