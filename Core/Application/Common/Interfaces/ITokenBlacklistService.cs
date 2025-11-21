namespace SecureCleanApiWaf.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Service for managing JWT token blacklisting to handle secure logout functionality.
    /// </summary>
    /// <remarks>
    /// This service provides token invalidation capabilities by maintaining a blacklist
    /// of tokens that should no longer be accepted for authentication. This is essential
    /// for secure logout functionality in JWT-based authentication systems.
    /// 
    /// Key Features:
    /// - Add tokens to blacklist on logout
    /// - Check if tokens are blacklisted during authentication
    /// - Automatic cleanup of expired tokens
    /// - Thread-safe operations
    /// 
    /// Security Considerations:
    /// - Tokens are stored with their expiration time
    /// - Expired tokens are automatically removed from blacklist
    /// - Only the JTI (Token ID) is stored, not the full token
    /// - All operations are logged for security auditing
    /// 
    /// Usage in Authentication Pipeline:
    /// 1. On logout: Add token to blacklist
    /// 2. On each request: Check if token is blacklisted
    /// 3. Reject requests with blacklisted tokens
    /// </remarks>
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// Adds a JWT token to the blacklist to prevent its further use.
        /// </summary>
        /// <param name="jwtToken">The JWT token to blacklist</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>A task representing the async operation</returns>
        /// <remarks>
        /// This method extracts the JTI (JWT ID) and expiration time from the token
        /// and stores it in the blacklist cache. The token will be automatically
        /// removed from the blacklist after its natural expiration time.
        /// 
        /// Security Features:
        /// - Only stores JTI, not the full token content
        /// - Sets cache expiration to match token expiration
        /// - Logs blacklisting for security auditing
        /// 
        /// Error Handling:
        /// - Invalid tokens are logged but don't throw exceptions
        /// - Cache failures are logged and handled gracefully
        /// <summary>
/// Add the given JWT to the blacklist to prevent its future use.
/// </summary>
/// <param name="jwtToken">The JWT to blacklist; the token's JTI (unique identifier) and expiration are used to control the blacklist entry lifetime.</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
/// <remarks>
/// The service extracts the token identifier (JTI) and stores only that identifier with an expiration aligned to the token's expiry. Malformed or invalid tokens are handled gracefully and do not cause exceptions to be thrown; cache or storage errors are logged and handled without propagating sensitive details.
/// </remarks>
        Task BlacklistTokenAsync(string jwtToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a JWT token is currently blacklisted.
        /// </summary>
        /// <param name="jwtToken">The JWT token to check</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if the token is blacklisted, false otherwise</returns>
        /// <remarks>
        /// This method is called during authentication to verify if a token
        /// should be rejected due to logout or security reasons.
        /// 
        /// Performance Optimizations:
        /// - Fast cache lookups (O(1) complexity)
        /// - Minimal token parsing (only extracts JTI)
        /// - Early returns for invalid tokens
        /// 
        /// Security Features:
        /// - Handles malformed tokens gracefully
        /// - Logs suspicious token validation attempts
        /// - Never throws exceptions that could leak information
        /// <summary>
/// Determines whether the provided JWT is currently present in the token blacklist.
/// </summary>
/// <param name="jwtToken">The JWT string to check (expected to contain a JTI).</param>
/// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
/// <returns>`true` if the token is blacklisted, `false` otherwise.</returns>
        Task<bool> IsTokenBlacklistedAsync(string jwtToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes expired tokens from the blacklist to optimize memory usage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>The number of expired tokens that were removed</returns>
        /// <remarks>
        /// This method is typically called by a background service or scheduled task
        /// to clean up expired blacklist entries and prevent memory bloat.
        /// 
        /// Cleanup Strategy:
        /// - Removes tokens that have passed their expiration time
        /// - Uses batch operations for efficiency
        /// - Logs cleanup statistics for monitoring
        /// 
        /// When to Call:
        /// - During application startup (cleanup any stale entries)
        /// - Periodically via background service (e.g., hourly)
        /// - Before application shutdown (optional cleanup)
        /// 
        /// Note: In distributed cache scenarios (Redis), expired entries
        /// are often cleaned up automatically by the cache provider.
        /// <summary>
/// Removes expired entries from the token blacklist to reclaim resources.
/// </summary>
/// <remarks>
/// Intended to be executed periodically (e.g., at startup, shutdown, or by a background task) to purge tokens that have passed their expiration. In distributed-cache scenarios, some expirations may be handled by the cache provider and therefore not require manual cleanup.
/// </remarks>
/// <returns>The number of blacklist entries removed during this cleanup operation.</returns>
        Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about the current blacklist for monitoring and debugging.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Statistics about the blacklist state</returns>
        /// <remarks>
        /// Returns information useful for monitoring and debugging:
        /// - Total number of blacklisted tokens
        /// - Number of expired tokens pending cleanup
        /// - Memory usage estimates
        /// - Cache hit rates (if available)
        /// 
        /// This method is useful for:
        /// - Health check endpoints
        /// - Administrative dashboards
        /// - Performance monitoring
        /// - Capacity planning
        /// <summary>
/// Retrieve monitoring and debugging statistics for the token blacklist.
/// </summary>
/// <returns>A <see cref="TokenBlacklistStats"/> containing total blacklisted tokens, expired tokens pending cleanup, estimated memory usage in bytes, an optional cache hit rate percentage, and the UTC timestamp when the statistics were last calculated.</returns>
        Task<TokenBlacklistStats> GetBlacklistStatsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about the token blacklist state for monitoring purposes.
    /// </summary>
    /// <remarks>
    /// This class provides insights into the blacklist performance and state,
    /// useful for monitoring dashboards and administrative interfaces.
    /// </remarks>
    public class TokenBlacklistStats
    {
        /// <summary>
        /// Total number of tokens currently in the blacklist
        /// </summary>
        public int TotalBlacklistedTokens { get; set; }

        /// <summary>
        /// Number of expired tokens that could be cleaned up
        /// </summary>
        public int ExpiredTokensPendingCleanup { get; set; }

        /// <summary>
        /// Estimated memory usage of the blacklist in bytes
        /// </summary>
        public long EstimatedMemoryUsageBytes { get; set; }

        /// <summary>
        /// Cache hit rate percentage for blacklist lookups (if available)
        /// </summary>
        public double? CacheHitRatePercent { get; set; }

        /// <summary>
        /// When these statistics were last calculated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}