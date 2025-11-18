using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SecureCleanApiWaf.Infrastructure.Services
{
    /// <summary>
    /// Implementation of token blacklisting service using Token entity and repository pattern.
    /// </summary>
    /// <remarks>
    /// This service provides secure JWT token invalidation for logout functionality,
    /// integrating with the domain layer through Token entity and ITokenRepository.
    /// 
    /// Architecture:
    /// - Uses Token domain entity for blacklist management
    /// - Persists token revocations to database through repository
    /// - Uses memory cache for fast lookup performance
    /// - Follows domain-driven design principles
    /// 
    /// Security Features:
    /// - Token revocation tracked as domain operation (Token.Revoke)
    /// - Comprehensive audit trail through Token entity
    /// - Automatic expiration based on token lifetime
    /// - Thread-safe operations
    /// - Graceful error handling
    /// 
    /// Performance Optimizations:
    /// - Fast O(1) memory cache lookups
    /// - Database queries only on cache miss
    /// - Efficient batch cleanup operations
    /// - Memory-conscious storage
    /// 
    /// Domain Integration Benefits:
    /// - Enforces business rules through Token entity
    /// - Publishes domain events (TokenRevokedEvent)
    /// - Maintains domain invariants
    /// - Provides rich audit information
    /// - Supports security monitoring
    /// </remarks>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        // ===== Dependencies =====

        // Repository for token persistence
        private readonly ITokenRepository _tokenRepository;

        // Memory cache for fast local lookups
        private readonly IMemoryCache _memoryCache;

        // Logger for security auditing and debugging
        private readonly ILogger<TokenBlacklistService> _logger;
        
        // Cache key prefix for blacklisted tokens
        private const string BlacklistKeyPrefix = "blacklist:";

        /// <summary>
        /// Initializes a new instance of the TokenBlacklistService.
        /// </summary>
        /// <param name="tokenRepository">Repository for token persistence</param>
        /// <param name="memoryCache">Memory cache for fast local lookups</param>
        /// <param name="logger">Logger for security auditing and debugging</param>
        public TokenBlacklistService(
            ITokenRepository tokenRepository,
            IMemoryCache memoryCache,
            ILogger<TokenBlacklistService> logger)
        {
            _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task BlacklistTokenAsync(string jwtToken, CancellationToken cancellationToken = default)
        {
            try
            {
                // ===== STEP 1: Validate Input =====
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    _logger.LogWarning("Attempted to blacklist null or empty token");
                    return;
                }

                // ===== STEP 2: Parse JWT and Extract TokenId (JTI) =====
                var tokenId = ExtractTokenId(jwtToken);
                
                if (string.IsNullOrEmpty(tokenId))
                {
                    _logger.LogWarning("Token does not contain valid JTI claim, cannot blacklist");
                    return;
                }

                // ===== STEP 3: Lookup Token Entity from Repository =====
                var tokenEntity = await _tokenRepository.GetByTokenIdAsync(tokenId, cancellationToken);
                
                if (tokenEntity == null)
                {
                    _logger.LogWarning("Token with JTI {TokenId} not found in repository, cannot blacklist", tokenId);
                    return;
                }

                // ===== STEP 4: Check if Token is Already Invalid =====
                if (tokenEntity.IsExpired())
                {
                    _logger.LogInformation("Token with JTI {TokenId} is already expired, skipping blacklist", tokenId);
                    return;
                }

                if (tokenEntity.IsRevoked())
                {
                    _logger.LogInformation("Token with JTI {TokenId} is already revoked", tokenId);
                    return;
                }

                // ===== STEP 5: Revoke Token (Domain Operation) =====
                // This calls the domain entity's business logic
                // Raises TokenRevokedEvent for publishing
                tokenEntity.Revoke("User logout");

                // ===== STEP 6: Persist Token Revocation =====
                await _tokenRepository.UpdateAsync(tokenEntity, cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);

                // ===== STEP 7: Update Memory Cache for Fast Lookup =====
                var cacheKey = GetBlacklistKey(tokenId);
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = tokenEntity.ExpiresAt.AddMinutes(1), // Small buffer
                    Priority = CacheItemPriority.Normal,
                    Size = 1 // Each entry counts as 1 unit toward memory limit
                };
                
                _memoryCache.Set(cacheKey, tokenEntity, memoryCacheOptions);

                // ===== STEP 8: Log Security Event =====
                _logger.LogInformation(
                    "Token blacklisted successfully. TokenId: {TokenId}, Username: {Username}, ExpiresAt: {ExpiresAt}",
                    tokenEntity.TokenId, tokenEntity.Username, tokenEntity.ExpiresAt);

                // Note: Domain events (TokenRevokedEvent) should be published by the 
                // application layer (command handler) after this method returns
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, "Failed to blacklist token. This may allow continued use of logged-out token");
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsTokenBlacklistedAsync(string jwtToken, CancellationToken cancellationToken = default)
        {
            try
            {
                // ===== STEP 1: Validate Input =====
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return false; // Invalid tokens are not blacklisted, they're just invalid
                }

                // ===== STEP 2: Extract TokenId (JTI) from JWT =====
                var tokenId = ExtractTokenId(jwtToken);
                
                if (string.IsNullOrEmpty(tokenId))
                {
                    return false; // Tokens without JTI cannot be blacklisted
                }

                var cacheKey = GetBlacklistKey(tokenId);

                // ===== STEP 3: Check Memory Cache First (Fastest) =====
                if (_memoryCache.TryGetValue(cacheKey, out Token? cachedToken) && cachedToken != null)
                {
                    // Verify token is actually revoked and not expired
                    if (cachedToken.IsRevoked() && !cachedToken.IsExpired())
                    {
                        _logger.LogInformation(
                            "Token found in blacklist (memory cache). TokenId: {TokenId}, RevokedAt: {RevokedAt}",
                            tokenId, cachedToken.RevokedAt);
                        return true;
                    }
                    
                    // Remove invalid entry from memory cache
                    _memoryCache.Remove(cacheKey);
                }

                // ===== STEP 4: Check Repository (Database Lookup) =====
                var isBlacklisted = await _tokenRepository.IsTokenBlacklistedAsync(tokenId, cancellationToken);
                
                if (isBlacklisted)
                {
                    // Get full token entity for caching
                    var tokenEntity = await _tokenRepository.GetByTokenIdAsync(tokenId, cancellationToken);
                    
                    if (tokenEntity != null && tokenEntity.IsRevoked() && !tokenEntity.IsExpired())
                    {
                        // Update memory cache with found entry for faster future lookups
                        var memoryCacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = tokenEntity.ExpiresAt.AddMinutes(1),
                            Priority = CacheItemPriority.Normal,
                            Size = 1
                        };
                        _memoryCache.Set(cacheKey, tokenEntity, memoryCacheOptions);

                        _logger.LogInformation(
                            "Token found in blacklist (repository). TokenId: {TokenId}, RevokedAt: {RevokedAt}",
                            tokenId, tokenEntity.RevokedAt);
                        return true;
                    }
                }

                // ===== STEP 5: Token Not Found in Blacklist =====
                return false;
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, "Error checking if token is blacklisted. Assuming not blacklisted for security");
                
                // In case of errors, assume token is NOT blacklisted
                // This prevents false rejections but may allow some blacklisted tokens
                // The trade-off favors availability over perfect security
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var cleanedCount = 0;
            
            try
            {
                _logger.LogInformation("Starting token cleanup process");

                // ===== Get Expired Tokens from Repository =====
                var expiredTokens = await _tokenRepository.GetExpiredTokensAsync(cancellationToken);
                
                if (expiredTokens.Any())
                {
                    // ===== Delete Expired Tokens in Batch =====
                    cleanedCount = await _tokenRepository.DeleteExpiredTokensAsync(expiredTokens, cancellationToken);
                    await _tokenRepository.SaveChangesAsync(cancellationToken);
                    
                    // ===== Clear Memory Cache Entries for Deleted Tokens =====
                    foreach (var token in expiredTokens)
                    {
                        var cacheKey = GetBlacklistKey(token.TokenId);
                        _memoryCache.Remove(cacheKey);
                    }
                }
                
                _logger.LogInformation("Token cleanup completed. Removed {CleanedCount} expired tokens", cleanedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            return cleanedCount;
        }

        /// <inheritdoc />
        public async Task<TokenBlacklistStats> GetBlacklistStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // ===== Get Statistics from Repository =====
                var tokenStats = await _tokenRepository.GetTokenStatisticsAsync(cancellationToken);
                
                // ===== Map to BlacklistStats =====
                return new TokenBlacklistStats
                {
                    TotalBlacklistedTokens = tokenStats.RevokedTokens,
                    ExpiredTokensPendingCleanup = tokenStats.ExpiredTokens,
                    EstimatedMemoryUsageBytes = tokenStats.RevokedTokens * 200, // Rough estimate
                    CacheHitRatePercent = null, // Would require cache provider metrics
                    LastUpdated = tokenStats.CalculatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating blacklist statistics");
                
                // Return default stats in case of error
                return new TokenBlacklistStats
                {
                    TotalBlacklistedTokens = 0,
                    ExpiredTokensPendingCleanup = 0,
                    EstimatedMemoryUsageBytes = 0,
                    CacheHitRatePercent = null,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Extracts the TokenId (JTI claim) from a JWT token.
        /// </summary>
        /// <param name="jwtToken">The JWT token to parse</param>
        /// <returns>The token ID (JTI claim) or null if not found</returns>
        private string? ExtractTokenId(string jwtToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                
                // Parse token without validation (we only need JTI claim)
                var token = handler.ReadJwtToken(jwtToken);
                
                // Extract JTI (JWT ID) claim
                var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                
                return jti;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract token ID from JWT");
                return null;
            }
        }

        /// <summary>
        /// Generates a cache key for a blacklisted token.
        /// </summary>
        /// <param name="tokenId">The JWT ID (JTI)</param>
        /// <returns>Cache key for the blacklist entry</returns>
        private static string GetBlacklistKey(string tokenId) => $"{BlacklistKeyPrefix}{tokenId}";
    }
}
