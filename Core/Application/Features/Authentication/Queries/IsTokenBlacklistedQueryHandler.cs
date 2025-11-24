using MediatR;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Models;
using CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace CleanArchitecture.ApiTemplate.Core.Application.Features.Authentication.Queries
{
    /// <summary>
    /// Handler for IsTokenBlacklistedQuery that checks JWT token blacklist status.
    /// </summary>
    /// <remarks>
    /// This handler implements the business logic for token blacklist checking within the CQRS pattern.
    /// It coordinates with the token blacklist service while providing comprehensive error handling,
    /// caching support, and detailed response information.
    /// 
    /// Responsibilities:
    /// - Validate JWT token format and extract claims
    /// - Check token status using ITokenBlacklistService
    /// - Provide detailed response with status information
    /// - Support caching through ICacheable implementation
    /// - Handle errors gracefully without exposing sensitive information
    /// 
    /// Integration Points:
    /// - Uses ITokenBlacklistService for blacklist lookups
    /// - Follows existing Result<T> pattern for consistent responses
    /// - Integrates with application caching behavior
    /// - Provides audit-friendly logging
    /// </remarks>
    public class IsTokenBlacklistedQueryHandler : IRequestHandler<IsTokenBlacklistedQuery, Result<TokenBlacklistStatusDto>>
    {
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ILogger<IsTokenBlacklistedQueryHandler> _logger;

        /// <summary>
        /// Initializes a new instance of IsTokenBlacklistedQueryHandler.
        /// </summary>
        /// <param name="tokenBlacklistService">Service for token blacklist operations</param>
        /// <summary>
        /// Initializes a new instance of <see cref="IsTokenBlacklistedQueryHandler"/> with required dependencies.
        /// </summary>
        public IsTokenBlacklistedQueryHandler(
            ITokenBlacklistService tokenBlacklistService,
            ILogger<IsTokenBlacklistedQueryHandler> logger)
        {
            _tokenBlacklistService = tokenBlacklistService ?? throw new ArgumentNullException(nameof(tokenBlacklistService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the token blacklist status query.
        /// </summary>
        /// <param name="request">The token blacklist query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <summary>
        /// Determines whether the provided JWT is blacklisted and returns a structured blacklist status.
        /// </summary>
        /// <param name="request">The query containing the JWT to check; if <see cref="IsTokenBlacklistedQuery.JwtToken"/> is null or empty the result will be an Invalid status indicating the token is required.</param>
        /// <returns>A Result containing a <see cref="TokenBlacklistStatusDto"/> that indicates `Blacklisted`, `Valid`, or `Invalid`. If an internal error occurs the handler returns a `Valid` status with Details set to "Unable to verify blacklist status, assuming valid".</returns>
        public async Task<Result<TokenBlacklistStatusDto>> Handle(IsTokenBlacklistedQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // ===== STEP 1: Validate Input =====
                if (string.IsNullOrWhiteSpace(request.JwtToken))
                {
                    _logger.LogWarning("IsTokenBlacklistedQuery received with empty JWT token");
                    return Result<TokenBlacklistStatusDto>.Ok(
                        TokenBlacklistStatusDto.Invalid("JWT token is required for blacklist checking"));
                }

                // ===== STEP 2: Parse and Validate Token =====
                var tokenInfo = ExtractTokenInformation(request.JwtToken);
                
                if (!tokenInfo.IsValid)
                {
                    _logger.LogWarning("IsTokenBlacklistedQuery received with invalid JWT token format");
                    return Result<TokenBlacklistStatusDto>.Ok(
                        TokenBlacklistStatusDto.Invalid("Invalid JWT token format"));
                }

                // ===== STEP 3: Check if Token is Expired =====
                if (tokenInfo.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogDebug("Token blacklist check for expired token. JTI: {Jti}", tokenInfo.Jti);
                    return Result<TokenBlacklistStatusDto>.Ok(
                        TokenBlacklistStatusDto.Invalid("Token has expired"));
                }

                // ===== STEP 4: Check Blacklist Status =====
                var isBlacklisted = await _tokenBlacklistService.IsTokenBlacklistedAsync(request.JwtToken, cancellationToken);

                // ===== STEP 5: Build Response =====
                TokenBlacklistStatusDto status;
                
                if (isBlacklisted)
                {
                    _logger.LogInformation("Token found in blacklist during CQRS query. JTI: {Jti}", tokenInfo.Jti);
                    
                    status = TokenBlacklistStatusDto.Blacklisted(
                        tokenInfo.Jti, 
                        null, // We don't have blacklisted date from the service in this context
                        tokenInfo.ExpiresAt, 
                        fromCache: false); // CachingBehavior will handle actual cache status
                }
                else
                {
                    _logger.LogDebug("Token not found in blacklist. JTI: {Jti}", tokenInfo.Jti);
                    
                    status = TokenBlacklistStatusDto.Valid(
                        tokenInfo.Jti, 
                        tokenInfo.ExpiresAt, 
                        fromCache: false); // CachingBehavior will handle actual cache status
                }

                return Result<TokenBlacklistStatusDto>.Ok(status);
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, "Error processing IsTokenBlacklistedQuery");

                // For security reasons, don't expose internal errors
                // Assume token is valid if we can't check (fail open for availability)
                var errorStatus = TokenBlacklistStatusDto.Valid(null, null, fromCache: false);
                errorStatus.Details = "Unable to verify blacklist status, assuming valid";
                
                return Result<TokenBlacklistStatusDto>.Ok(errorStatus);
            }
        }

        /// <summary>
        /// Extracts key information from JWT token for processing.
        /// </summary>
        /// <param name="jwtToken">JWT token to parse</param>
        /// <summary>
        /// Parses a JWT string to extract the token identifier (JTI) and expiration and indicates whether the token could be parsed successfully.
        /// </summary>
        /// <param name="jwtToken">The JWT compact serialization to parse.</param>
        /// <returns>A TokenInformation containing the token's JTI, the expiration as a UTC DateTime (defaults to one hour from now if the `exp` claim is missing or cannot be parsed), and IsValid set to `true` when a JTI is present; on parse failure returns IsValid = `false`.</returns>
        private TokenInformation ExtractTokenInformation(string jwtToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwtToken);

                var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                
                var expClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                var expiresAt = DateTime.UtcNow.AddHours(1); // Default fallback

                if (long.TryParse(expClaim, out var exp))
                {
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                }

                return new TokenInformation
                {
                    Jti = jti,
                    ExpiresAt = expiresAt,
                    IsValid = !string.IsNullOrEmpty(jti)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JWT token in IsTokenBlacklistedQueryHandler");
                return new TokenInformation { IsValid = false };
            }
        }

        /// <summary>
        /// Internal class to hold parsed token information.
        /// </summary>
        private class TokenInformation
        {
            public string? Jti { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsValid { get; set; }
        }
    }
}