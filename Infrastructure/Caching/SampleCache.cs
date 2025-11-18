using Microsoft.Extensions.Caching.Memory;

namespace SecureCleanApiWaf.Infrastructure.Caching
{
    /// <summary>
    /// Implements the Cache-Aside pattern using in-memory caching for improved performance in the Blazor Server application.
    /// This pattern checks the cache first for a value; if not found, it simulates fetching data (here, a static "Cached value"),
    /// stores it in the cache, and returns it. This reduces redundant computations or API calls, aligning with the project's
    /// caching strategy as outlined in the README for efficient data retrieval and scalability.
    /// </summary>
    public class SampleCache
    {
        private readonly IMemoryCache _cache;
        public SampleCache(IMemoryCache cache) => _cache = cache;
        public string GetOrSet(string key)
        {
            if (!_cache.TryGetValue(key, out string value))
            {
                value = "Cached value";
                _cache.Set(key, value);
            }
            return value;
        }
    }
}
