namespace CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO for ApiDataItem responses in controllers and components.
    /// Used for API endpoints and UI components to return clean, structured data.
    /// </summary>
    public class ApiDataItemDto
    {
        /// <summary>
        /// Internal unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// External system identifier.
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Item name/title.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Item description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Source API URL.
        /// </summary>
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Last synchronization timestamp.
        /// </summary>
        public DateTime LastSyncedAt { get; set; }

        /// <summary>
        /// Data status (Active, Stale, Deleted).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if data is fresh.
        /// </summary>
        public bool IsFresh { get; set; }

        /// <summary>
        /// Age of the data since last sync.
        /// </summary>
        public TimeSpan Age { get; set; }

        /// <summary>
        /// Item category (from metadata).
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Item price (from metadata).
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Item rating (from metadata).
        /// </summary>
        public double? Rating { get; set; }

        /// <summary>
        /// Item tags (from metadata).
        /// </summary>
        public string[]? Tags { get; set; }
    }
}
