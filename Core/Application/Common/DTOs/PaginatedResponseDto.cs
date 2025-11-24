namespace CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs
{
    /// <summary>
    /// Generic DTO for paginated API responses.
    /// Used across all features that require pagination support.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    public class PaginatedResponseDto<T>
    {
        /// <summary>
        /// The items in the current page.
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// The current page number (1-based).
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// The total number of pages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates if there is a next page available.
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Indicates if there is a previous page available.
        /// </summary>
        public bool HasPreviousPage => Page > 1;
    }
}
