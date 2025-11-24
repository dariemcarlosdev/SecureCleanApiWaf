namespace CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for cache service abstraction.
    /// Provides methods for distributed caching operations.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the caching implementation, allowing for easy testing
    /// and flexibility to switch between different caching providers (e.g., in-memory,
    /// Redis, SQL Server) without changing application code.
    /// </remarks>
    public interface ICacheService
    {
        /// <summary>
        /// Retrieves a cached value by its key.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The unique identifier for the cached item.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The cached value if found; otherwise, default(T).</returns>
        /// <remarks>
        /// Returns null for reference types and default value for value types if the key doesn't exist.
        /// <summary>
/// Retrieves a cached value for the specified key.
/// </summary>
/// <param name="key">The cache key to look up.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>The cached value if present; `null` for reference types or `default(T)` for value types when the key is not found.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a value in the cache with an optional expiration time.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The unique identifier for the cached item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="expiration">Optional expiration time. If null, uses default expiration (5 minutes).</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// If an item with the same key already exists, it will be overwritten.
        /// The expiration time is absolute from the time of insertion.
        /// <summary>
/// Stores a value in the distributed cache under the specified key.
/// </summary>
/// <param name="key">Cache key to store the value under.</param>
/// <param name="value">Value to store in the cache.</param>
/// <param name="expiration">Optional absolute expiration relative to now; if null a default of 5 minutes is applied.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <remarks>
/// If an entry with the same key already exists it will be overwritten. The provided expiration is absolute from the time of insertion.
/// </remarks>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a cached value by its key.
        /// </summary>
        /// <param name="key">The unique identifier for the cached item to remove.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// If the key doesn't exist, the operation completes successfully without error.
        /// <summary>
/// Removes the cached entry identified by the provided key.
/// </summary>
/// <param name="key">The cache key to remove.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <remarks>
/// Completes successfully if the key does not exist; no error is thrown for missing keys.
/// </remarks>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The unique identifier for the cached item.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <summary>
/// Determines whether a value exists in the cache for the specified key.
/// </summary>
/// <param name="key">The cache key to check for existence.</param>
/// <returns>`true` if the key exists in the cache, `false` otherwise.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}