using MediatR;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace CleanArchitecture.ApiTemplate.Core.Application.Features.Authentication.Commands
{
    /// <summary>
    /// Handler for BlacklistTokenCommand that processes JWT token blacklisting operations.
    /// </summary>
    /// <remarks>
    /// This handler implements the business logic for token blacklisting within the CQRS pattern.
    /// It coordinates with the token blacklist service while providing comprehensive error handling,
    /// logging, and response formatting.
    /// 
    /// Responsibilities:
    /// - Validate JWT token format and claims
    /// - Coordinate with ITokenBlacklistService for actual blacklisting
    /// - Provide detailed response with security recommendations
    /// - Log security events for audit purposes
    /// - Handle errors gracefully without exposing sensitive information
    /// 
    /// Integration Points:
    /// - Uses ITokenBlacklistService for blacklisting operations
    /// - Follows existing Result<T> pattern for consistent error handling
    /// - Integrates with application logging infrastructure
    /// </remarks>
    public class BlacklistTokenCommandHandler : IRequestHandler<BlacklistTokenCommand, Result<BlacklistTokenResponse>>
    {
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ILogger<BlacklistTokenCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of BlacklistTokenCommandHandler.
        /// </summary>
        /// <param name="tokenBlacklistService">Service for token blacklisting operations</param>
        /// <summary>
        /// Initializes a new instance of <see cref="BlacklistTokenCommandHandler"/> with its required dependencies.
        /// </summary>
        public BlacklistTokenCommandHandler(
            ITokenBlacklistService tokenBlacklistService,
            ILogger<BlacklistTokenCommandHandler> logger)
        {
            _tokenBlacklistService = tokenBlacklistService ?? throw new ArgumentNullException(nameof(tokenBlacklistService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the token blacklisting command.
        /// </summary>
        /// <param name="request">The blacklist token command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <summary>
        /// Processes a blacklist request for a JWT: validates the token, determines its state, and blacklists it if still valid.
        /// </summary>
        /// <param name="request">Command containing the JWT to blacklist and optional metadata (Reason, ClientIpAddress).</param>
        /// <param name="cancellationToken">Token used to cancel the blacklist operation.</param>
        /// <returns>`Result` containing a `BlacklistTokenResponse` with token metadata and status on success, or error information on failure.</returns>
        public async Task<Result<BlacklistTokenResponse>> Handle(BlacklistTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // ===== STEP 1: Validate Input =====
                if (string.IsNullOrWhiteSpace(request.JwtToken))
                {
                    _logger.LogWarning("BlacklistTokenCommand received with empty JWT token");
                    return Result<BlacklistTokenResponse>.Fail("JWT token is required for blacklisting");
                }

                // ===== STEP 2: Parse and Validate Token =====
                var tokenInfo = ExtractTokenInformation(request.JwtToken);
                
                if (!tokenInfo.IsValid)
                {
                    _logger.LogWarning("BlacklistTokenCommand received with invalid JWT token format");
                    return Result<BlacklistTokenResponse>.Fail("Invalid JWT token format");
                }

                if (tokenInfo.ExpiresAt <= DateTime.UtcNow)
                {
                    // Token is already expired, no need to blacklist but return success
                    _logger.LogInformation(
                        "Token blacklist requested for already expired token. JTI: {Jti}, Username: {Username}",
                        tokenInfo.Jti, tokenInfo.Username);

                    var expiredResponse = new BlacklistTokenResponse
                    {
                        TokenId = tokenInfo.Jti ?? "unknown",
                        Username = tokenInfo.Username,
                        BlacklistedAt = DateTime.UtcNow,
                        TokenExpiresAt = tokenInfo.ExpiresAt,
                        Status = "already_expired",
                        Details = "Token was already expired, no blacklisting needed",
                        ClientRecommendations = GetClientRecommendations()
                    };

                    return Result<BlacklistTokenResponse>.Ok(expiredResponse);
                }

                // ===== STEP 3: Perform Blacklisting for Valid Token =====
                await _tokenBlacklistService.BlacklistTokenAsync(request.JwtToken, cancellationToken);

                // ===== STEP 4: Log Security Event =====
                _logger.LogInformation(
                    "Token blacklisted successfully via CQRS command. JTI: {Jti}, Username: {Username}, Reason: {Reason}, ClientIP: {ClientIP}",
                    tokenInfo.Jti, tokenInfo.Username, request.Reason ?? "logout", request.ClientIpAddress ?? "unknown");

                // ===== STEP 5: Build Success Response =====
                var response = new BlacklistTokenResponse
                {
                    TokenId = tokenInfo.Jti ?? "unknown",
                    Username = tokenInfo.Username,
                    BlacklistedAt = DateTime.UtcNow,
                    TokenExpiresAt = tokenInfo.ExpiresAt,
                    Status = "blacklisted",
                    Details = $"Token successfully blacklisted. Reason: {request.Reason ?? "logout"}",
                    ClientRecommendations = GetClientRecommendations()
                };

                return Result<BlacklistTokenResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, 
                    "Error processing BlacklistTokenCommand. Reason: {Reason}, ClientIP: {ClientIP}",
                    request.Reason ?? "logout", request.ClientIpAddress ?? "unknown");

                return Result<BlacklistTokenResponse>.Fail(
                    "An error occurred while blacklisting the token. Please try again.");
            }
        }

        /// <summary>
        /// Extracts key information from JWT token for processing.
        /// </summary>
        /// <param name="jwtToken">JWT token to parse</param>
        /// <summary>
        /// Extracts the token identifier, username, and expiration time from the provided JWT string.
        /// </summary>
        /// <param name="jwtToken">The JWT compact serialization to parse.</param>
        /// <returns>A TokenInformation containing `Jti`, `Username`, `ExpiresAt`, and `IsValid`; if parsing fails, returns a TokenInformation with `IsValid` set to `false`.</returns>
        private TokenInformation ExtractTokenInformation(string jwtToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwtToken);

                var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                var username = token.Claims.FirstOrDefault(c => 
                    c.Type == JwtRegisteredClaimNames.UniqueName || 
                    c.Type == JwtRegisteredClaimNames.Name ||
                    c.Type == "username")?.Value;

                var expClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                var expiresAt = DateTime.UtcNow.AddHours(1); // Default fallback

                if (long.TryParse(expClaim, out var exp))
                {
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                }

                return new TokenInformation
                {
                    Jti = jti,
                    Username = username,
                    ExpiresAt = expiresAt,
                    IsValid = !string.IsNullOrEmpty(jti)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JWT token in BlacklistTokenCommandHandler");
                return new TokenInformation { IsValid = false };
            }
        }

        /// <summary>
        /// Gets standard client-side security recommendations.
        /// </summary>
        /// <summary>
        /// Client-side remediation steps to perform after a token is blacklisted or invalidated.
        /// </summary>
        /// <returns>An array of user-facing security recommendations (remove stored token, clear cached data, redirect to login, and similar actions).</returns>
        private static string[] GetClientRecommendations()
        {
            return new[]
            {
                "Remove token from client storage (localStorage, sessionStorage, cookies)",
                "Clear any cached user data and application state",
                "Redirect to login page or update authentication state",
                "Consider clearing other sensitive client-side data"
            };
        }

        /// <summary>
        /// Internal class to hold parsed token information.
        /// </summary>
        private class TokenInformation
        {
            public string? Jti { get; set; }
            public string? Username { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsValid { get; set; }
        }
    }
}