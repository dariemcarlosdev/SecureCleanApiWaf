namespace SecureCleanApiWaf.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO for API data items statistics and monitoring information.
    /// Used for administrative dashboards, health checks, and monitoring endpoints.
    /// </summary>
    public class ApiDataStatisticsDto
    {
        /// <summary>
        /// Total number of active items.
        /// </summary>
        public int TotalActiveItems { get; set; }

        /// <summary>
        /// Total number of stale items needing refresh.
        /// </summary>
        public int TotalStaleItems { get; set; }

        /// <summary>
        /// Total number of deleted items.
        /// </summary>
        public int TotalDeletedItems { get; set; }

        /// <summary>
        /// Average age of all items (time since last sync).
        /// </summary>
        public TimeSpan AverageAge { get; set; }

        /// <summary>
        /// Age of the oldest item.
        /// </summary>
        public TimeSpan OldestItemAge { get; set; }

        /// <summary>
        /// Age of the newest item.
        /// </summary>
        public TimeSpan NewestItemAge { get; set; }

        /// <summary>
        /// Number of unique source URLs.
        /// </summary>
        public int UniqueSourceCount { get; set; }

        /// <summary>
        /// Items synchronized in the last 24 hours.
        /// </summary>
        public int ItemsSyncedLast24Hours { get; set; }

        /// <summary>
        /// Items marked as stale in the last 24 hours.
        /// </summary>
        public int ItemsMarkedStaleLast24Hours { get; set; }

        /// <summary>
        /// When these statistics were calculated.
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
