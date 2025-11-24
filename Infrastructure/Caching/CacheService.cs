using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces;

namespace CleanArchitecture.ApiTemplate.Infrastructure.Caching
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

        /// <summary>
        /// Initializes a new instance of <see cref="CacheService"/> with the specified distributed cache and logger.
        /// </summary>
        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a cached value by its key.
        /// <summary>
        /// Retrieve a cached value by key and deserialize it to the specified type.
        /// </summary>
        /// <param name="key">The cache key to read.</param>
        /// <param name="cancellationToken">Token to cancel the cache retrieval operation.</param>
        /// <returns>The cached value deserialized to <typeparamref name="T"/>, or `default(T)` if the key is not present or an error occurs.</returns>
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
        /// <summary>
        /// Stores a value in the distributed cache under the specified key using JSON serialization.
        /// </summary>
        /// <param name="key">Cache key to store the value under.</param>
        /// <param name="value">Value to serialize and store.</param>
        /// <param name="expiration">Optional absolute expiration relative to now; if null defaults to 5 minutes.</param>
        /// <param name="cancellationToken">Token to cancel the cache operation.</param>
        /// <remarks>Exceptions during serialization or cache operations are caught and logged; the method does not propagate them.</remarks>
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
        /// <summary>
        /// Removes the cache entry identified by the specified key if it exists.
        /// </summary>
        /// <param name="key">Cache key of the entry to remove.</param>
        /// <param name="cancellationToken">Token to cancel the removal operation.</param>
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
        /// <summary>
        /// Determines whether a cache entry exists for the specified key.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns>`true` if a non-empty value is stored for the key, `false` otherwise.</returns>
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