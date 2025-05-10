using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Aigent.Api.Models
{
    /// <summary>
    /// Base class for query parameters
    /// </summary>
    public abstract class QueryParameters
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        [FromQuery(Name = "pageSize")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        /// <summary>
        /// Sort field
        /// </summary>
        [FromQuery(Name = "sortBy")]
        public string SortBy { get; set; }

        /// <summary>
        /// Sort direction (asc or desc)
        /// </summary>
        [FromQuery(Name = "sortDirection")]
        public string SortDirection { get; set; } = "asc";
    }

    /// <summary>
    /// Query parameters for agents
    /// </summary>
    public class AgentQueryParameters : QueryParameters
    {
        /// <summary>
        /// Filter by agent name
        /// </summary>
        [FromQuery(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Filter by agent type
        /// </summary>
        [FromQuery(Name = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Filter by agent status
        /// </summary>
        [FromQuery(Name = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Filter by supported action type
        /// </summary>
        [FromQuery(Name = "actionType")]
        public string ActionType { get; set; }
    }

    /// <summary>
    /// Pagination metadata
    /// </summary>
    public class PaginationMetadata
    {
        /// <summary>
        /// Total count of items
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Current page
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPrevious => CurrentPage > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNext => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Paginated list of items
    /// </summary>
    /// <typeparam name="T">Type of the items</typeparam>
    public class PagedList<T>
    {
        /// <summary>
        /// Items in the current page
        /// </summary>
        public List<T> Items { get; }

        /// <summary>
        /// Pagination metadata
        /// </summary>
        public PaginationMetadata Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the PagedList class
        /// </summary>
        /// <param name="items">Items in the current page</param>
        /// <param name="totalCount">Total count of items</param>
        /// <param name="pageNumber">Current page number</param>
        /// <param name="pageSize">Page size</param>
        public PagedList(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            Metadata = new PaginationMetadata
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Creates a paged list from a collection
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged list</returns>
        public static PagedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
