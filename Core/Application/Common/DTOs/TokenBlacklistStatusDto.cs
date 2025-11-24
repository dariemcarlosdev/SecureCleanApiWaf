namespace CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO for token blacklist status response.
    /// Provides comprehensive information about whether a token is blacklisted.
    /// </summary>
    public class TokenBlacklistStatusDto
    {
        /// <summary>
        /// Whether the token is currently blacklisted.
        /// </summary>
        public bool IsBlacklisted { get; set; }

        /// <summary>
        /// JWT ID (JTI) of the token.
        /// </summary>
        public string? TokenId { get; set; }

        /// <summary>
        /// When the token was blacklisted (if applicable).
        /// </summary>
        public DateTime? BlacklistedAt { get; set; }

        /// <summary>
        /// When the token naturally expires.
        /// </summary>
        public DateTime? TokenExpiresAt { get; set; }

        /// <summary>
        /// Current status of the token.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Additional details about the token status.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// When this status was last checked.
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this result came from cache.
        /// </summary>
        public bool FromCache { get; set; }

        /// <summary>
        /// Creates a blacklisted token status.
        /// <summary>
        /// Create a TokenBlacklistStatusDto representing a token that has been blacklisted.
        /// </summary>
        /// <param name="tokenId">The token's JWT ID (jti), or null if unknown.</param>
        /// <param name="blacklistedAt">The UTC time when the token was blacklisted, or null if not recorded.</param>
        /// <param name="tokenExpiresAt">The token's natural expiration time, or null if unknown.</param>
        /// <param name="fromCache">Whether the status was retrieved from cache.</param>
        /// <returns>A TokenBlacklistStatusDto with IsBlacklisted set to `true`, Status set to "blacklisted", Details describing the blacklist, CheckedAt set to the current UTC time, and FromCache set to the provided value.</returns>
        public static TokenBlacklistStatusDto Blacklisted(
            string? tokenId, 
            DateTime? blacklistedAt, 
            DateTime? tokenExpiresAt, 
            bool fromCache = false)
        {
            return new TokenBlacklistStatusDto
            {
                IsBlacklisted = true,
                TokenId = tokenId,
                BlacklistedAt = blacklistedAt,
                TokenExpiresAt = tokenExpiresAt,
                Status = "blacklisted",
                Details = "Token has been blacklisted and is no longer valid",
                CheckedAt = DateTime.UtcNow,
                FromCache = fromCache
            };
        }

        /// <summary>
        /// Creates a valid (not blacklisted) token status.
        /// <summary>
        /// Creates a TokenBlacklistStatusDto representing a valid (not blacklisted) token.
        /// </summary>
        /// <param name="tokenId">The token's JWT ID (jti), or null if unavailable.</param>
        /// <param name="tokenExpiresAt">The token's natural expiration time, or null if unknown.</param>
        /// <param name="fromCache">Whether the result was retrieved from cache.</param>
        /// <returns>A DTO indicating the token is valid; CheckedAt is set to the current UTC time.</returns>
        public static TokenBlacklistStatusDto Valid(
            string? tokenId, 
            DateTime? tokenExpiresAt, 
            bool fromCache = false)
        {
            return new TokenBlacklistStatusDto
            {
                IsBlacklisted = false,
                TokenId = tokenId,
                TokenExpiresAt = tokenExpiresAt,
                Status = "valid",
                Details = "Token is not blacklisted and remains valid",
                CheckedAt = DateTime.UtcNow,
                FromCache = fromCache
            };
        }

        /// <summary>
        /// Creates an invalid token status (malformed or expired).
        /// <summary>
        /// Create a TokenBlacklistStatusDto representing an invalid token status.
        /// </summary>
        /// <param name="reason">Human-readable explanation for why the token is considered invalid.</param>
        /// <returns>A TokenBlacklistStatusDto with IsBlacklisted set to false, Status set to "invalid", Details set to the provided reason, CheckedAt set to the current UTC time, and FromCache set to false.</returns>
        public static TokenBlacklistStatusDto Invalid(string reason)
        {
            return new TokenBlacklistStatusDto
            {
                IsBlacklisted = false,
                Status = "invalid",
                Details = reason,
                CheckedAt = DateTime.UtcNow,
                FromCache = false
            };
        }
    }
}