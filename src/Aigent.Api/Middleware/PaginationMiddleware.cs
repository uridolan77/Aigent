using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Aigent.Api.Middleware
{
    /// <summary>
    /// Middleware for pagination
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
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }
        
        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Only process GET requests
            if (context.Request.Method == "GET")
            {
                // Default pagination values
                var defaultPage = 1;
                var defaultPageSize = 10;
                var maxPageSize = 100;
                
                // Get pagination parameters from query string
                if (!int.TryParse(context.Request.Query["page"], out var page))
                {
                    page = defaultPage;
                }
                
                if (!int.TryParse(context.Request.Query["pageSize"], out var pageSize))
                {
                    pageSize = defaultPageSize;
                }
                
                // Ensure page and pageSize are valid
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, maxPageSize);
                
                // Add pagination parameters to the request
                context.Items["Pagination:Page"] = page;
                context.Items["Pagination:PageSize"] = pageSize;
                
                // Add pagination headers to the response
                context.Response.OnStarting(() =>
                {
                    if (context.Items.TryGetValue("Pagination:TotalCount", out var totalCountObj) && 
                        totalCountObj is int totalCount)
                    {
                        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                        
                        context.Response.Headers.Add("X-Pagination-Page", page.ToString());
                        context.Response.Headers.Add("X-Pagination-PageSize", pageSize.ToString());
                        context.Response.Headers.Add("X-Pagination-TotalCount", totalCount.ToString());
                        context.Response.Headers.Add("X-Pagination-TotalPages", totalPages.ToString());
                        
                        // Add links for navigation
                        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
                        var queryString = context.Request.QueryString.Value;
                        
                        // Remove existing pagination parameters
                        var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);
                        queryParams.Remove("page");
                        queryParams.Remove("pageSize");
                        
                        // Add updated pagination parameters
                        queryParams.Add("pageSize", pageSize.ToString());
                        
                        var links = new System.Text.StringBuilder();
                        
                        // First page
                        queryParams.Set("page", "1");
                        links.Append($"<{baseUrl}?{queryParams}>; rel=\"first\", ");
                        
                        // Previous page
                        if (page > 1)
                        {
                            queryParams.Set("page", (page - 1).ToString());
                            links.Append($"<{baseUrl}?{queryParams}>; rel=\"prev\", ");
                        }
                        
                        // Next page
                        if (page < totalPages)
                        {
                            queryParams.Set("page", (page + 1).ToString());
                            links.Append($"<{baseUrl}?{queryParams}>; rel=\"next\", ");
                        }
                        
                        // Last page
                        queryParams.Set("page", totalPages.ToString());
                        links.Append($"<{baseUrl}?{queryParams}>; rel=\"last\"");
                        
                        context.Response.Headers.Add("Link", links.ToString());
                    }
                    
                    return Task.CompletedTask;
                });
            }
            
            await _next(context);
        }
    }
}
