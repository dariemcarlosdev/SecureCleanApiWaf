using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Behaviors;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Features.Authentication.Queries
{
    /// <summary>
    /// Query to check if a JWT token is currently blacklisted.
    /// </summary>
    /// <remarks>
    /// This query implements the CQRS pattern for token validation operations.
    /// It's typically used by authentication middleware to verify token validity.
    /// 
    /// Caching Strategy:
    /// - Implements ICacheable for performance optimization
    /// - Short cache duration (1 minute) to balance performance vs. security
    /// - Cache key based on token JTI for uniqueness
    /// - Bypass cache option for critical security checks
    /// 
    /// Usage Scenarios:
    /// - Authentication middleware token validation
    /// - API endpoint security checks
    /// - Administrative token status verification
    /// - Security audit and monitoring
    /// 
    /// Performance Considerations:
    /// - Cached results reduce database/cache lookups
    /// - Fast response times for frequently checked tokens
    /// - Automatic cache invalidation based on token lifecycle
    /// </remarks>
    public class IsTokenBlacklistedQuery : IRequest<Result<TokenBlacklistStatusDto>>, ICacheable
    {
        /// <summary>
        /// The JWT token to check for blacklist status
        /// </summary>
        public string JwtToken { get; }

        /// <summary>
        /// Whether to bypass cache and get real-time status
        /// </summary>
        public bool BypassCache { get; set; }

        /// <summary>
        /// Cache key based on token JTI for uniqueness
        /// </summary>
        public string CacheKey { get; }

        /// <summary>
        /// Short sliding expiration for security-sensitive data
        /// </summary>
        public int SlidingExpirationInMinutes { get; set; } = 1;

        /// <summary>
        /// Short absolute expiration for security-sensitive data
        /// </summary>
        public int AbsoluteExpirationInMinutes { get; set; } = 2;

        /// <summary>
        /// Initializes a new instance of IsTokenBlacklistedQuery.
        /// </summary>
        /// <param name="jwtToken">JWT token to check</param>
        /// <summary>
        /// Creates a query that checks whether the provided JWT is blacklisted and prepares its cache metadata.
        /// </summary>
        /// <param name="jwtToken">The JWT to check for blacklist status; used to derive the cache key.</param>
        /// <param name="bypassCache">If true, instructs handlers to bypass cached results and obtain the latest status.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="jwtToken"/> is null.</exception>
        public IsTokenBlacklistedQuery(string jwtToken, bool bypassCache = false)
        {
            JwtToken = jwtToken ?? throw new ArgumentNullException(nameof(jwtToken));
            BypassCache = bypassCache;
            
            // Generate cache key from token JTI if possible, otherwise use token hash
            CacheKey = GenerateCacheKey(jwtToken);
        }

        /// <summary>
        /// Generates a secure cache key from the JWT token.
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <summary>
        /// Builds a cache key identifying the blacklist status for the specified JWT.
        /// </summary>
        /// <param name="token">The raw JWT string to derive the cache key from.</param>
        /// <returns>`TokenBlacklist:JTI:{jti}` if the token contains a JTI claim; otherwise `TokenBlacklist:Hash:{hash}` where `{hash}` is the token's GetHashCode().</returns>
        private static string GenerateCacheKey(string token)
        {
            try
            {
                // Try to extract JTI for a meaningful cache key
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    return $"TokenBlacklist:JTI:{jti}";
                }
            }
            catch
            {
                // Fall back to hash-based key if JTI extraction fails
            }

            // Fallback: Use hash of token for cache key (less ideal but functional)
            var tokenHash = token.GetHashCode().ToString();
            return $"TokenBlacklist:Hash:{tokenHash}";
        }
    }
}