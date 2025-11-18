using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;

namespace SecureCleanApiWaf.Infrastructure.Caching
{
    /// <summary>
    /// Implementation of ICacheService using IDistributedCache for distributed caching.
    /// Provides abstraction over distributed cache operations with JSON serialization.
    /// </summary>
    /// <remarks>
    /// This implementation uses IDistributedCache which can be backed by:
    /// - In-memory cache (development)
    /// - Redis (production)
    /// - SQL Server (production)
    /// The JSON serialization ensures complex objects can be cached efficiently.
    /// </remarks>
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a cached value by its key.
        /// </summary>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, cancellationToken);

                if (string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Stores a value in the cache with an optional expiration time.
        /// </summary>
        public async Task SetAsync<T>(
            string key, 
            T value, 
            TimeSpan? expiration = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
                };

                var serializedData = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serializedData, options, cancellationToken);

                _logger.LogDebug("Cached data for key: {Key} with expiration: {Expiration}", 
                    key, options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching data for key: {Key}", key);
            }
        }

        /// <summary>
        /// Removes a cached value by its key.
        /// </summary>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
                _logger.LogDebug("Removed cache entry for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            }
        }

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, cancellationToken);
                var exists = !string.IsNullOrEmpty(cachedData);
                
                _logger.LogDebug("Cache key {Key} exists: {Exists}", key, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }
    }
}
