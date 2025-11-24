namespace CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO for external API item responses with strongly-typed structure.
    /// Used when working with known/predictable API response structures.
    /// </summary>
    /// <remarks>
    /// This DTO represents the expected structure from external APIs.
    /// Use this with AutoMapper for known API structures, or use dynamic mapping
    /// for unknown/varying API structures.
    /// </remarks>
    public class ApiItemDto
    {
        /// <summary>
        /// External item identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Item name or title.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Item description or details.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Item category or type.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Item price or cost.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Item rating or score.
        /// </summary>
        public double? Rating { get; set; }

        /// <summary>
        /// Item tags or keywords.
        /// </summary>
        public string[]? Tags { get; set; }

        /// <summary>
        /// Item status or availability.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// When the item was last updated in the source system.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
