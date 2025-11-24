using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using CleanArchitecture.ApiTemplate.Core.Application.Features.Authentication.Queries;
using CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs;

namespace CleanArchitecture.ApiTemplate.Presentation.Controllers.v1
{
    /// <summary>
    /// Administrative controller for token blacklist management and monitoring.
    /// </summary>
    /// <remarks>
    /// This controller demonstrates CQRS pattern integration with token blacklist operations.
    /// It provides administrative endpoints for:
    /// - Token status verification
    /// - System statistics and monitoring
    /// - Health check information
    /// 
    /// CQRS Benefits Demonstrated:
    /// - Clean separation between queries and business logic
    /// - Automatic caching through CachingBehavior
    /// - Consistent error handling via Result<T> pattern
    /// - Testable and maintainable code structure
    /// - Easy integration with monitoring systems
    /// 
    /// Security Considerations:
    /// - Most endpoints require Admin role for security
    /// - Token status endpoint available to authenticated users
    /// - No sensitive information exposed in responses
    /// - Comprehensive audit logging through handlers
    /// </remarks>
    [ApiController]
    [Route("api/v1/token-blacklist")]
    [Authorize] // Require authentication
    public class TokenBlacklistController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TokenBlacklistController> _logger;

        /// <summary>
        /// Initializes a new instance of TokenBlacklistController.
        /// </summary>
        /// <param name="mediator">MediatR instance for CQRS operations</param>
        /// <summary>
        /// Initializes a new instance of <see cref="TokenBlacklistController"/> with the required MediatR mediator and logger.
        /// </summary>
        /// <param name="mediator">Mediator for sending CQRS queries and commands.</param>
        /// <param name="logger">Logger for audit and debugging.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="mediator"/> or <paramref name="logger"/> is null.</exception>
        public TokenBlacklistController(IMediator mediator, ILogger<TokenBlacklistController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks if a specific JWT token is currently blacklisted using CQRS pattern.
        /// </summary>
        /// <param name="token">JWT token to check (from query parameter)</param>
        /// <param name="bypassCache">Whether to bypass cache for real-time results</param>
        /// <returns>Token blacklist status with detailed information</returns>
        /// <remarks>
        /// **CQRS Query Endpoint**
        /// 
        /// This endpoint demonstrates how to use CQRS queries for token validation:
        /// 1. Creates IsTokenBlacklistedQuery with provided parameters
        /// 2. Executes query via MediatR with automatic caching
        /// 3. Returns structured response with comprehensive status information
        /// 
        /// Caching Behavior:
        /// - Results are automatically cached for 1-2 minutes via CachingBehavior
        /// - Use bypassCache=true for real-time administrative checks
        /// - Cache keys are based on token JTI for efficiency
        /// 
        /// Use Cases:
        /// - Administrative token verification
        /// - Security audits and investigations
        /// - API testing and debugging
        /// - Integration with external monitoring systems
        /// 
        /// Example: GET /api/v1/token-blacklist/status?token=eyJ...&bypassCache=true
        /// </remarks>
        /// <response code="200">Returns detailed token blacklist status</response>
        /// <response code="400">Invalid token format or missing token</response>
        /// <summary>
        /// Check whether the specified JWT is present in the token blacklist and return a detailed status payload.
        /// </summary>
        /// <param name="token">The JWT to check (required).</param>
        /// <param name="bypassCache">If true, forces fresh evaluation rather than using a cached result.</param>
        /// <returns>
        /// 200 with a payload containing: `is_blacklisted`, `token_id`, `status`, `details`, `blacklisted_at`,
        /// `token_expires_at`, `checked_at`, `from_cache`, and `processing_method`; 
        /// 400 when the token is missing or the query fails; 401 when the caller is unauthorized; 500 on internal errors.
        /// </returns>
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTokenStatus([FromQuery] string token, [FromQuery] bool bypassCache = false)
        {
            try
            {
                // ===== Input Validation =====
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new
                    {
                        error = "missing_token",
                        message = "Token parameter is required",
                        example = "/api/v1/token-blacklist/status?token=eyJ..."
                    });
                }

                // ===== Create and Execute CQRS Query =====
                var query = new IsTokenBlacklistedQuery(token, bypassCache);
                var result = await _mediator.Send(query, HttpContext.RequestAborted);

                // ===== Handle Query Result =====
                if (!result.Success)
                {
                    _logger.LogWarning("Token status query failed: {Error}", result.Error);
                    return BadRequest(new
                    {
                        error = "query_failed",
                        message = result.Error
                    });
                }

                // ===== Return Detailed Status =====
                var status = result.Data;
                return Ok(new
                {
                    is_blacklisted = status.IsBlacklisted,
                    token_id = status.TokenId,
                    status = status.Status,
                    details = status.Details,
                    blacklisted_at = status.BlacklistedAt,
                    token_expires_at = status.TokenExpiresAt,
                    checked_at = status.CheckedAt,
                    from_cache = status.FromCache,
                    processing_method = "CQRS_Query_Pattern"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token status endpoint");
                return StatusCode(500, new
                {
                    error = "internal_error",
                    message = "An error occurred while checking token status"
                });
            }
        }

        /// <summary>
        /// Gets comprehensive statistics about the token blacklist system using CQRS pattern.
        /// </summary>
        /// <param name="bypassCache">Whether to bypass cache for real-time statistics</param>
        /// <returns>Detailed statistics including performance, security, and health metrics</returns>
        /// <remarks>
        /// **CQRS Query Endpoint for System Monitoring**
        /// 
        /// This endpoint demonstrates advanced CQRS usage for system monitoring:
        /// 1. Creates GetTokenBlacklistStatsQuery with cache control
        /// 2. Executes query via MediatR with extended caching (5-10 minutes)
        /// 3. Returns comprehensive statistics for administrative dashboards
        /// 
        /// Statistics Include:
        /// - Basic counters (blacklisted tokens, memory usage)
        /// - Performance metrics (response times, cache hit rates)
        /// - Security metrics (blocked attempts, suspicious patterns)
        /// - Health indicators (system status, warnings, recommendations)
        /// 
        /// Caching Strategy:
        /// - Results cached for 5-10 minutes (statistics don't change frequently)
        /// - Use bypassCache=true for real-time administrative monitoring
        /// - Automatic cache invalidation based on system events
        /// 
        /// Use Cases:
        /// - Administrative dashboards
        /// - Health monitoring systems
        /// - Performance analysis and capacity planning
        /// - Security audit reports
        /// 
        /// Example: GET /api/v1/token-blacklist/stats?bypassCache=true
        /// </remarks>
        /// <response code="200">Returns comprehensive blacklist statistics</response>
        /// <response code="401">Unauthorized - authentication required</response>
        /// <summary>
        /// Retrieve comprehensive token blacklist statistics for administrative use.
        /// </summary>
        /// <param name="bypassCache">If true, force fresh statistics retrieval and bypass any cached results.</param>
        /// <returns>An HTTP response: on success a 200 OK with a JSON object containing `basic`, `performance`, `security`, `health`, and `metadata` sections; otherwise an error response.</returns>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")] // Admin-only endpoint
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBlacklistStatistics([FromQuery] bool bypassCache = false)
        {
            try
            {
                _logger.LogInformation("Admin requesting blacklist statistics via CQRS");

                // ===== Create and Execute CQRS Query =====
                var query = new GetTokenBlacklistStatsQuery(bypassCache);
                var result = await _mediator.Send(query, HttpContext.RequestAborted);

                // ===== Handle Query Result =====
                if (!result.Success)
                {
                    _logger.LogWarning("Blacklist statistics query failed: {Error}", result.Error);
                    return StatusCode(500, new
                    {
                        error = "query_failed",
                        message = result.Error
                    });
                }

                // ===== Return Comprehensive Statistics =====
                var stats = result.Data;
                return Ok(new
                {
                    // ===== Basic Statistics =====
                    basic = new
                    {
                        total_blacklisted_tokens = stats.TotalBlacklistedTokens,
                        expired_tokens_pending_cleanup = stats.ExpiredTokensPendingCleanup,
                        estimated_memory_usage_bytes = stats.EstimatedMemoryUsageBytes,
                        cache_hit_rate_percent = stats.CacheHitRatePercent,
                        last_updated = stats.LastUpdated
                    },

                    // ===== Performance Metrics =====
                    performance = new
                    {
                        average_check_time_ms = stats.Performance.AverageCheckTimeMs,
                        average_blacklist_time_ms = stats.Performance.AverageBlacklistTimeMs,
                        checks_last_hour = stats.Performance.ChecksLastHour,
                        blacklist_operations_last_hour = stats.Performance.BlacklistOperationsLastHour,
                        memory_cache_hit_rate = stats.Performance.MemoryCacheHitRate,
                        distributed_cache_hit_rate = stats.Performance.DistributedCacheHitRate
                    },

                    // ===== Security Metrics =====
                    security = new
                    {
                        blocked_attempts_last_hour = stats.Security.BlockedAttemptsLastHour,
                        suspicious_patterns_detected = stats.Security.SuspiciousPatternsDetected,
                        recent_security_events = stats.Security.RecentSecurityEvents,
                        top_blocked_ip_addresses = stats.Security.TopBlockedIpAddresses
                    },

                    // ===== Health Indicators =====
                    health = new
                    {
                        overall_status = stats.Health.Status.ToString(),
                        memory_status = stats.Health.MemoryStatus.ToString(),
                        cache_status = stats.Health.CacheStatus.ToString(),
                        warnings = stats.Health.Warnings,
                        recommendations = stats.Health.Recommendations
                    },

                    // ===== Metadata =====
                    metadata = new
                    {
                        processing_method = "CQRS_Query_Pattern",
                        retrieved_at = DateTime.UtcNow,
                        from_cache = !bypassCache // Assuming cache is used unless bypassed
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in blacklist statistics endpoint");
                return StatusCode(500, new
                {
                    error = "internal_error",
                    message = "An error occurred while retrieving blacklist statistics"
                });
            }
        }

        /// <summary>
        /// Health check endpoint for the token blacklist system.
        /// </summary>
        /// <returns>Simple health status for monitoring systems</returns>
        /// <remarks>
        /// This endpoint provides a simplified health check specifically for the
        /// token blacklist system. It's designed for:
        /// - Load balancer health probes
        /// - Kubernetes liveness/readiness probes
        /// - Monitoring system integration
        /// - Quick system status verification
        /// 
        /// Uses CQRS pattern internally but returns simplified health status
        /// suitable for automated monitoring systems.
        /// </remarks>
        /// <response code="200">System is healthy</response>
        /// <summary>
        /// Performs a health check of the token blacklist system using a CQRS query and reports service health.
        /// </summary>
        /// <returns>
        /// HTTP 200 with a JSON payload { status = "healthy", service = "token-blacklist", timestamp, method } when healthy; 
        /// HTTP 503 with a JSON payload { status = "unhealthy", service = "token-blacklist", timestamp, warnings } when unhealthy or with { status = "unhealthy", service = "token-blacklist", error, timestamp } on failure.
        /// </returns>
        [HttpGet("health")]
        [AllowAnonymous] // Allow health checks without authentication
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                // Use CQRS to get health information
                var query = new GetTokenBlacklistStatsQuery(bypassCache: true);
                var result = await _mediator.Send(query, HttpContext.RequestAborted);

                if (result.Success && result.Data.Health.Status != HealthStatusDto.Unhealthy)
                {
                    return Ok(new
                    {
                        status = "healthy",
                        service = "token-blacklist",
                        timestamp = DateTime.UtcNow,
                        method = "CQRS_Health_Check"
                    });
                }

                return StatusCode(503, new
                {
                    status = "unhealthy",
                    service = "token-blacklist",
                    timestamp = DateTime.UtcNow,
                    warnings = result.Success ? result.Data.Health.Warnings : new[] { "Service unavailable" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for token blacklist system");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    service = "token-blacklist",
                    error = "Health check failed",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}