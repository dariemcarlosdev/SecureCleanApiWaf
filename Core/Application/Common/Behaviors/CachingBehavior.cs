using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;
using System;

namespace CleanArchitecture.ApiTemplate.Core.Application.Common.Behaviors
{
    /// <summary>
    /// Implements a MediatR pipeline behavior for response caching in the Blazor Server application's API integration layer.
    /// This behavior intercepts requests that implement ICacheable, checking for cached responses using distributed caching
    /// to avoid redundant processing or external API calls. If a cache hit occurs, it deserializes and returns the cached data,
    /// refreshing the cache entry; otherwise, it executes the handler, caches the response with configurable sliding and absolute
    /// expirations (defaulting to 30 and 60 minutes respectively), and logs the operation. It supports bypassing the cache via
    /// the request's BypassCache flag, promoting performance and scalability as per the project's caching strategy (Cache-Aside
    /// and MediatR response caching) detailed in the README, while ensuring thread-safety and error resilience through async operations.
    ///
    /// MediatR is a popular .NET library that implements the mediator pattern, enabling decoupled communication between
    /// components by sending requests (commands, queries, notifications) through a central mediator. Instead of calling
    /// services or handlers directly, requests are routed via MediatR, which locates and executes the appropriate handler.
    /// This approach improves maintainability, testability, and separation of concerns in complex applications.
    ///
    /// Pipeline behaviors in MediatR allow you to run cross-cutting logic before and after request handlers.
    /// They are similar to middleware in ASP.NET Core, enabling features like logging, caching, validation,
    /// and error handling to be applied consistently across all requests without modifying individual handlers.
    /// This promotes separation of concerns and keeps business logic clean and focused.
    ///
    /// In a CQRS (Command Query Responsibility Segregation) architecture, MediatR is often used to dispatch commands (for state changes)
    /// and queries (for data retrieval) to their respective handlers. This caching behavior is especially useful for queries, as it can
    /// intercept query requests, check if the result is already cached, and return the cached data if available�reducing load on the system
    /// and improving performance. For commands, which change state, caching is typically bypassed. This ensures that queries are fast and
    /// scalable, while commands remain consistent and reliable, fully supporting the separation of read and write concerns central to CQRS.
    /// </summary>
    public class CachingBehavior<TRequest, TResponse>(
        ILogger<CachingBehavior<TRequest, TResponse>> logger,
        IDistributedCache cache
        )
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheable
    {
        /// <summary>
        /// Intercepts cacheable requests to return a cached response when available or invoke the handler, cache its result, and return that response.
        /// </summary>
        /// <param name="request">The cacheable request that provides the cache key and controls behavior (set BypassCache to skip caching; may specify SlidingExpirationInMinutes and AbsoluteExpirationInMinutes).</param>
        /// <param name="next">The handler delegate to execute when a cached response is not available or caching is bypassed.</param>
        /// <param name="cancellationToken">Cancellation token used for cache operations and handler execution.</param>
        /// <returns>The cached response if present for the request's CacheKey; otherwise the response produced by invoking the handler.</returns>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            TResponse response;
            // Bypass cache if specified in the request
            if (request.BypassCache) return await next();

            // Local function to get response and add it to cache
            // This function executes the actual request handler (e.g., database or service call) via 'await next()'.
            // If a valid response is returned, it configures cache entry options using sliding and absolute expiration values
            // (defaulting to 30 and 60 minutes if not specified in the request).
            // The response is serialized to JSON bytes (JsonSerializer.Serialize and Encoding.Default.GetBytes) and stored in the distributed in-memory cache using the provided cache key.
            // Finally, it returns the response.
            // This ensures that subsequent requests for the same key can be served directly from cache, improving performance and reducing redundant processing or external API calls.
            async Task<TResponse> GetResponseAndAddToCache()
            {
                response = await next();
                if (response != null)
                {
                    var slidingExpiration = request.SlidingExpirationInMinutes == 0 ? 30 : request.SlidingExpirationInMinutes;
                    var absoluteExpiration = request.AbsoluteExpirationInMinutes == 0 ? 60 : request.AbsoluteExpirationInMinutes;
                    var options = new DistributedCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(slidingExpiration))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(absoluteExpiration));

                    var serializedData = Encoding.Default.GetBytes(JsonSerializer.Serialize(response));

                    // Store serialized response in cache. This is stored as a byte array.
                    await cache.SetAsync(request.CacheKey, serializedData, options, cancellationToken);
                }
                return response;
            }

            // Attempt to retrieve a cached response using the provided cache key
            var cachedResponse = await cache.GetAsync(request.CacheKey, cancellationToken);

            // If found in cache:
            // 1. Deserialize the cached byte array back into the expected response type.
            // 2. Log that the response was fetched from cache.
            // 3. Refresh the cache entry to extend its sliding expiration.
            // 4. Returns the deserialized response immediately, avoiding redundant handler execution (e.g., no database or API call).
            if (cachedResponse != null)
            {
                response = JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cachedResponse))!;
                logger.LogInformation("fetched from cache with key : {CacheKey}", request.CacheKey);
                cache.Refresh(request.CacheKey);
            }
            // If no cached response is found:
            // 1. Calls the local function to execute the request handler and obtain the response.
            else
            {
                response = await GetResponseAndAddToCache();
                logger.LogInformation("added to cache with key : {CacheKey}", request.CacheKey);
            }
            // Return the response
            return response;
        }
    }
}