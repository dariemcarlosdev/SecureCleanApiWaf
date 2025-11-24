namespace CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO wrapper for API responses that contain collections.
    /// Used for external APIs that wrap their data in a collection response structure.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class ApiCollectionResponseDto<T>
    {
        /// <summary>
        /// The collection of data items.
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// Total number of items in the collection.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Current page number (if paginated).
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page (if paginated).
        /// </summary>
        public int PageSize { get; set; }
    }
}
