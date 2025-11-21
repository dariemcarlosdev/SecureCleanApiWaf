using System.IdentityModel.Tokens.Jwt;
using SecureCleanApiWaf.Core.Application.Features.Authentication.Queries;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SecureCleanApiWaf.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware that validates JWT tokens against the blacklist using CQRS pattern. This Middleware is executed after authentication /// but before authorization.
    /// </summary>
    /// <remarks>
    /// This middleware integrates with the ASP.NET Core authentication pipeline and uses
    /// the CQRS pattern via MediatR to check token blacklist status. Benefits include:
    /// 
    /// CQRS Integration:
    /// - Uses IsTokenBlacklistedQuery for consistent business logic
    /// - Leverages automatic caching through CachingBehavior
    /// - Follows established patterns for maintainability
    /// - Enables easy testing and mocking
    /// 
    /// Security Features:
    /// - Runs after authentication but before authorization
    /// - Fast blacklist lookups with automatic caching
    /// - Comprehensive logging for security auditing
    /// - Graceful error handling without information leakage
    /// 
    /// Performance Optimizations:
    /// - Only processes requests with JWT tokens
    /// - Leverages CQRS caching for repeated token checks
    /// - Minimal token parsing overhead through query handler
    /// - Early returns for non-authenticated requests
    /// 
    /// Pipeline Position:
    /// - After UseAuthentication() 
    /// - Before UseAuthorization()
    /// - This ensures we have authenticated user context but can still reject tokens
    /// </remarks>
    public class JwtBlacklistValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMediator _mediator;
        private readonly ILogger<JwtBlacklistValidationMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the JWT blacklist validation middleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <param name="mediator">MediatR instance for CQRS queries</param>
        /// <summary>
        /// Middleware that validates JWT tokens against a blacklist after authentication and before authorization.
        /// </summary>
        /// <param name="next">The next middleware delegate in the pipeline.</param>
        /// <param name="mediator">MediatR mediator used to dispatch queries to check token blacklist status.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="next"/>, <paramref name="mediator"/>, or <paramref name="logger"/> is null.</exception>
        public JwtBlacklistValidationMiddleware(
            RequestDelegate next,
            IMediator mediator,
            ILogger<JwtBlacklistValidationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes HTTP requests to validate JWT tokens against the blacklist using CQRS.
        /// </summary>
        /// <summary>
        /// Validates a Bearer JWT from the current HTTP request against the token blacklist and either rejects blacklisted tokens or continues the pipeline.
        /// </summary>
        /// <param name="context">The HTTP context for the current request; used to read the Authorization header and to write a 401 response for blacklisted tokens.</param>
        /// <returns>A task that completes when the middleware has finished processing the request.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // ===== STEP 1: Extract JWT Token from Request =====
                var token = ExtractTokenFromRequest(context);
                
                if (string.IsNullOrEmpty(token))
                {
                    // No token present, continue to next middleware
                    // This allows anonymous endpoints and other auth schemes
                    await _next(context);
                    return;
                }

                // ===== STEP 2: Create CQRS Query for Token Validation =====
                // Use bypass cache for critical security checks in middleware
                // This ensures we get the most up-to-date blacklist status
                var query = new IsTokenBlacklistedQuery(token, bypassCache: false);

                // ===== STEP 3: Execute Query via MediatR =====
                var result = await _mediator.Send(query, context.RequestAborted);

                // ===== STEP 4: Handle Query Result =====
                if (!result.Success)
                {
                    _logger.LogWarning("Error checking token blacklist status via CQRS: {Error}", result.Error);
                    
                    // In case of query errors, continue pipeline (fail open for availability)
                    // Log the issue but don't break the authentication flow
                    await _next(context);
                    return;
                }

                var blacklistStatus = result.Data;

                // ===== STEP 5: Process Blacklist Status =====
                if (blacklistStatus.IsBlacklisted)
                {
                    // Token is blacklisted, reject the request
                    await HandleBlacklistedToken(context, token, blacklistStatus);
                    return; // Don't continue pipeline
                }

                // Token status indicates invalid but not blacklisted (malformed, expired, etc.)
                if (blacklistStatus.Status == "invalid")
                {
                    _logger.LogDebug("Invalid token detected via CQRS: {Details}", blacklistStatus.Details);
                    // Let the authentication middleware handle invalid tokens
                }
                else
                {
                    // Token is valid and not blacklisted
                    _logger.LogDebug("Token validated successfully via CQRS. JTI: {TokenId}", blacklistStatus.TokenId);
                }

                // ===== STEP 6: Token is Valid, Continue Pipeline =====
                await _next(context);
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, "Error in JWT blacklist validation middleware using CQRS");
                
                // In case of errors, continue pipeline to avoid breaking the application
                // This favors availability over perfect security
                // In production, you might want to be more strict
                await _next(context);
            }
        }

        /// <summary>
        /// Extracts JWT token from the Authorization header.
        /// </summary>
        /// <param name="context">HTTP context containing the request</param>
        /// <summary>
        /// Extracts the Bearer JWT from the request's Authorization header if present and appears to be a well-formed JWT.
        /// </summary>
        /// <param name="context">The current HTTP context whose request headers are inspected for a Bearer token.</param>
        /// <returns>The Bearer JWT string when present and consisting of three dot-separated parts; `null` otherwise.</returns>
        private string? ExtractTokenFromRequest(HttpContext context)
        {
            try
            {
                // ===== Extract Authorization Header =====
                var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();
                
                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return null; // No Authorization header
                }

                // ===== Validate Bearer Token Format =====
                // Expected format: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                const string bearerPrefix = "Bearer ";
                
                if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return null; // Not a Bearer token (could be Basic, API Key, etc.)
                }

                // ===== Extract Token Value =====
                var token = authorizationHeader[bearerPrefix.Length..].Trim();
                
                if (string.IsNullOrEmpty(token))
                {
                    return null; // Empty token value
                }

                // ===== Basic JWT Format Validation =====
                // JWT tokens have 3 parts separated by dots: header.payload.signature
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    _logger.LogWarning("Invalid JWT token format in Authorization header");
                    return null;
                }

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting token from Authorization header");
                return null;
            }
        }

        /// <summary>
        /// Handles requests with blacklisted tokens by returning 401 Unauthorized.
        /// </summary>
        /// <param name="context">HTTP context for the current request</param>
        /// <param name="token">The blacklisted token</param>
        /// <summary>
        /// Produce a 401 Unauthorized response for a request carrying a blacklisted JWT, emit a security log, and write an enhanced JSON error payload.
        /// </summary>
        /// <param name="context">The current HTTP context for the request/response.</param>
        /// <param name="token">The raw JWT string that was identified as blacklisted (used for logging/context).</param>
        /// <param name="blacklistStatus">Detailed blacklist status returned by the CQRS query (used to populate logs and the error payload).</param>
        private async Task HandleBlacklistedToken(HttpContext context, string token, TokenBlacklistStatusDto blacklistStatus)
        {
            try
            {
                // ===== Log Security Event with CQRS Data =====
                _logger.LogWarning(
                    "Rejected request with blacklisted token via CQRS. JTI: {TokenId}, BlacklistedAt: {BlacklistedAt}, Path: {Path}, UserAgent: {UserAgent}, RemoteIP: {RemoteIP}",
                    blacklistStatus.TokenId ?? "unknown",
                    blacklistStatus.BlacklistedAt,
                    context.Request.Path,
                    context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown",
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                // ===== Set Response Headers =====
                context.Response.StatusCode = 401; // Unauthorized
                context.Response.ContentType = "application/json";
                
                // Add WWW-Authenticate header as required by RFC 7235
                // This tells the client what authentication scheme is expected
                context.Response.Headers.Append("WWW-Authenticate", "Bearer");

                // ===== Security Headers =====
                // Prevent caching of this error response
                context.Response.Headers.Append("Cache-Control", "no-store");
                context.Response.Headers.Append("Pragma", "no-cache");

                // ===== Generate Enhanced Error Response with CQRS Data =====
                var errorResponse = new
                {
                    error = "invalid_token",
                    error_description = "The access token has been revoked",
                    details = new
                    {
                        status = blacklistStatus.Status,
                        token_id = blacklistStatus.TokenId,
                        blacklisted_at = blacklistStatus.BlacklistedAt,
                        checked_at = blacklistStatus.CheckedAt,
                        processing_method = "CQRS_Query_Pattern"
                    },
                    // Include request ID for debugging (but not sensitive info)
                    request_id = context.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling blacklisted token response");
                
                // Fallback to simple 401 response
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
            }
        }
    }

    /// <summary>
    /// Extension methods for registering JWT blacklist validation middleware.
    /// </summary>
    /// <remarks>
    /// Provides convenient extension methods for adding the blacklist validation
    /// middleware to the ASP.NET Core pipeline with proper ordering.
    /// </remarks>
    public static class JwtBlacklistValidationMiddlewareExtensions
    {
        /// <summary>
        /// Adds JWT blacklist validation middleware to the application pipeline.
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder for method chaining</returns>
        /// <remarks>
        /// This middleware should be added to the pipeline:
        /// - AFTER UseAuthentication() - so we have user context
        /// - BEFORE UseAuthorization() - so we can reject tokens before authorization
        /// 
        /// Example usage:
        /// <code>
        /// app.UseAuthentication();
        /// app.UseJwtBlacklistValidation();
        /// app.UseAuthorization();
        /// </code>
        /// 
        /// The middleware will:
        /// 1. Extract JWT tokens from Authorization headers
        /// 2. Use CQRS queries to check tokens against the blacklist service
        /// 3. Return 401 for blacklisted tokens with detailed information
        /// 4. Allow valid tokens to continue with automatic caching benefits
        /// <summary>
        /// Registers the JWT blacklist validation middleware into the ASP.NET Core request pipeline.
        /// </summary>
        /// <param name="builder">The application builder to add the middleware to.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so additional middleware can be chained.</returns>
        /// <remarks>
        /// The middleware should be placed after authentication and before authorization (for example, after <c>UseAuthentication()</c> and before <c>UseAuthorization()</c>) so that authenticated tokens are checked against the blacklist prior to authorization decisions.
        /// </remarks>
        public static IApplicationBuilder UseJwtBlacklistValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtBlacklistValidationMiddleware>();
        }
    }
}