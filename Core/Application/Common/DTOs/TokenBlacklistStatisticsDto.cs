namespace SecureCleanApiWaf.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO for comprehensive token blacklist system statistics.
    /// Used for monitoring, administrative dashboards, and health checks.
    /// </summary>
    public class TokenBlacklistStatisticsDto
    {
        /// <summary>
        /// Total number of tokens currently in the blacklist.
        /// </summary>
        public int TotalBlacklistedTokens { get; set; }

        /// <summary>
        /// Number of expired tokens that could be cleaned up.
        /// </summary>
        public int ExpiredTokensPendingCleanup { get; set; }

        /// <summary>
        /// Estimated memory usage of the blacklist in bytes.
        /// </summary>
        public long EstimatedMemoryUsageBytes { get; set; }

        /// <summary>
        /// Cache hit rate percentage for blacklist lookups (if available).
        /// </summary>
        public double? CacheHitRatePercent { get; set; }

        /// <summary>
        /// When these statistics were last calculated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// System performance metrics.
        /// </summary>
        public PerformanceMetricsDto Performance { get; set; } = new();

        /// <summary>
        /// Security-related statistics.
        /// </summary>
        public SecurityMetricsDto Security { get; set; } = new();

        /// <summary>
        /// System health indicators.
        /// </summary>
        public HealthIndicatorsDto Health { get; set; } = new();
    }

    /// <summary>
    /// Performance-related metrics for the blacklist system.
    /// </summary>
    public class PerformanceMetricsDto
    {
        /// <summary>
        /// Average response time for blacklist checks in milliseconds.
        /// </summary>
        public double AverageCheckTimeMs { get; set; }

        /// <summary>
        /// Average response time for blacklist operations in milliseconds.
        /// </summary>
        public double AverageBlacklistTimeMs { get; set; }

        /// <summary>
        /// Number of blacklist checks in the last hour.
        /// </summary>
        public long ChecksLastHour { get; set; }

        /// <summary>
        /// Number of tokens blacklisted in the last hour.
        /// </summary>
        public long BlacklistOperationsLastHour { get; set; }

        /// <summary>
        /// Memory cache hit rate percentage.
        /// </summary>
        public double? MemoryCacheHitRate { get; set; }

        /// <summary>
        /// Distributed cache hit rate percentage.
        /// </summary>
        public double? DistributedCacheHitRate { get; set; }
    }

    /// <summary>
    /// Security-related metrics for the blacklist system.
    /// </summary>
    public class SecurityMetricsDto
    {
        /// <summary>
        /// Number of blocked authentication attempts due to blacklisted tokens.
        /// </summary>
        public long BlockedAttemptsLastHour { get; set; }

        /// <summary>
        /// Number of suspicious patterns detected.
        /// </summary>
        public long SuspiciousPatternsDetected { get; set; }

        /// <summary>
        /// Most recent security events.
        /// </summary>
        public string[] RecentSecurityEvents { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Top IP addresses with blocked attempts.
        /// </summary>
        public Dictionary<string, int> TopBlockedIpAddresses { get; set; } = new();
    }

    /// <summary>
    /// Health indicators for the blacklist system.
    /// </summary>
    public class HealthIndicatorsDto
    {
        /// <summary>
        /// Overall system health status.
        /// </summary>
        public HealthStatusDto Status { get; set; } = HealthStatusDto.Healthy;

        /// <summary>
        /// Memory usage status.
        /// </summary>
        public HealthStatusDto MemoryStatus { get; set; } = HealthStatusDto.Healthy;

        /// <summary>
        /// Cache performance status.
        /// </summary>
        public HealthStatusDto CacheStatus { get; set; } = HealthStatusDto.Healthy;

        /// <summary>
        /// Any warnings or issues detected.
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Recommendations for system optimization.
        /// </summary>
        public string[] Recommendations { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Health status enumeration for system components.
    /// </summary>
    public enum HealthStatusDto
    {
        /// <summary>
        /// System is operating normally.
        /// </summary>
        Healthy,

        /// <summary>
        /// System has minor issues but is functional.
        /// </summary>
        Warning,

        /// <summary>
        /// System has significant issues affecting performance.
        /// </summary>
        Degraded,

        /// <summary>
        /// System is not functioning properly.
        /// </summary>
        Unhealthy
    }
}
