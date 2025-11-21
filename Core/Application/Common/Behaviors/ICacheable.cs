namespace SecureCleanApiWaf.Core.Application.Common.Behaviors
{
    /// <summary>
    /// Defines members required for objects that support caching, including cache key identification and expiration
    /// policies.
    /// </summary>
    /// <remarks>Implement this interface to enable objects to participate in caching mechanisms that use
    /// cache keys and expiration strategies. The interface provides properties to control cache bypassing and to
    /// specify both sliding and absolute expiration intervals. This is commonly used in scenarios where objects need
    /// fine-grained control over their caching behavior, such as in distributed or in-memory cache systems.</remarks>
    public interface ICacheable
    {
        /// <summary>
        /// Gets a value indicating whether cache retrieval should be bypassed when accessing data.
        /// </summary>
        /// <remarks>When <see langword="true"/>, data is fetched directly from the source rather than
        /// using any cached results. This can be useful for ensuring the most up-to-date information is retrieved, but
        /// may impact performance if the source is slower than the cache.</remarks>
        bool BypassCache { get; }
        /// <summary>
        /// Gets the unique key used to identify the cached item associated with this instance.
        /// </summary>
        string CacheKey { get; }
        /// <summary>
        /// Gets the sliding expiration interval, in minutes, for the associated cache entry.
        /// </summary>
        /// <remarks>The sliding expiration resets each time the cache entry is accessed. If the entry is
        /// not accessed within the specified interval, it will be removed from the cache. This property is typically
        /// used to control cache lifetimes based on activity.</remarks>
        int SlidingExpirationInMinutes { get; }
        /// <summary>
        /// Gets the absolute expiration time, in minutes, for the cached item.
        /// </summary>
        int AbsoluteExpirationInMinutes { get; }
    }
}
