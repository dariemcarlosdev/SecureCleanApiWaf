using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Behaviors;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Features.Authentication.Queries
{
    /// <summary>
    /// Query to get comprehensive statistics about the token blacklist system.
    /// </summary>
    /// <remarks>
    /// This query provides monitoring and administrative insights into the token blacklist system.
    /// It's useful for:
    /// - Health check endpoints
    /// - Administrative dashboards
    /// - Performance monitoring
    /// - Capacity planning
    /// - Security audit reports
    /// 
    /// Caching Strategy:
    /// - Implements ICacheable with moderate cache duration (5 minutes)
    /// - Statistics don't change frequently, so caching improves performance
    /// - Bypass cache option for real-time administrative needs
    /// - Cache key is static since statistics are global
    /// 
    /// Usage Scenarios:
    /// - Admin dashboard display
    /// - Health monitoring systems
    /// - Performance analysis
    /// - Security audit reports
    /// - Capacity planning decisions
    /// </remarks>
    public class GetTokenBlacklistStatsQuery : IRequest<Result<TokenBlacklistStatisticsDto>>, ICacheable
    {
        /// <summary>
        /// Whether to bypass cache and get real-time statistics
        /// </summary>
        public bool BypassCache { get; set; }

        /// <summary>
        /// Static cache key for blacklist statistics
        /// </summary>
        public string CacheKey => "TokenBlacklistStats:Global";

        /// <summary>
        /// Cache for 5 minutes (statistics don't change frequently)
        /// </summary>
        public int SlidingExpirationInMinutes { get; set; } = 5;

        /// <summary>
        /// Absolute expiration of 10 minutes
        /// </summary>
        public int AbsoluteExpirationInMinutes { get; set; } = 10;

        /// <summary>
        /// Initializes a new instance of GetTokenBlacklistStatsQuery.
        /// </summary>
        /// <summary>
        /// Creates a query to request global token blacklist statistics, optionally bypassing cached results.
        /// </summary>
        /// <param name="bypassCache">If true, the query will bypass caching to obtain real-time statistics; otherwise cached results may be used.</param>
        public GetTokenBlacklistStatsQuery(bool bypassCache = false)
        {
            BypassCache = bypassCache;
        }
    }
}