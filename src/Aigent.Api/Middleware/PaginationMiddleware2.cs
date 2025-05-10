using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Aigent.Api.Models;

namespace Aigent.Api.Middleware
{
    /// <summary>
    /// Middleware for adding pagination headers to responses
    /// </summary>
    public class PaginationMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the PaginationMiddleware class
        /// </summary>
        /// <param name="next">Next middleware in the pipeline</param>
        public PaginationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">HTTP context</param>
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                if (context.Items.TryGetValue("PaginationMetadata", out var metadata) && 
                    metadata is PaginationMetadata paginationMetadata)
                {
                    var json = JsonSerializer.Serialize(paginationMetadata);
                    context.Response.Headers.Add("X-Pagination", json);
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for PaginationMiddleware
    /// </summary>
    public static class PaginationMiddlewareExtensions
    {
        /// <summary>
        /// Adds pagination middleware to the application
        /// </summary>
        /// <param name="builder">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UsePagination(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PaginationMiddleware>();
        }
    }
}
