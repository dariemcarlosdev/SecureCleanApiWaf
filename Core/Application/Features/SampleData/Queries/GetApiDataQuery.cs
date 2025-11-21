using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Behaviors;
using SecureCleanApiWaf.Core.Application.Common.Models;

namespace SecureCleanApiWaf.Core.Application.Features.SampleData.Queries
{
    /// <summary>
    /// Represents a query Class for each API data retrieval operation from a specified API endpoint, with optional caching and expiration settings.
    /// This class implements the IRequest interface from MediatR to facilitate request/response handling, and the ICacheable to apply caching behaviors.
    /// </summary>
    /// <remarks>Use this query to request data from an external API, optionally bypassing the cache or configuring
    /// cache expiration policies. The query supports both sliding and absolute expiration for cached results. The cache key
    /// is generated based on the API URL, ensuring unique cache entries per endpoint.</remarks>
    /// <typeparam name="T">The type of data expected to be returned from the API.</typeparam>
    public class GetApiDataQuery<T> : IRequest<Result<T>>, ICacheable
    {
        /// <summary>
        /// Gets the base URL of the API endpoint used for requests.
        /// </summary>
        public string ApiUrl { get; }

        public bool BypassCache { get; set; }
        public string CacheKey => $"ApiData:{ApiUrl}";
        public int SlidingExpirationInMinutes { get; set; } = 30;
        public int AbsoluteExpirationInMinutes { get; set; } = 60;

        /// <summary>
        /// Initializes a new instance of the GetApiDataQuery class with the specified API URL and cache bypass option.
        /// </summary>
        /// <param name="apiUrl">The URL of the API endpoint to query. Cannot be null or empty.</param>
        /// <param name="bypassCache">Specifies whether to bypass any cached data and retrieve fresh results from the API. Set to <see
        /// <summary>
        /// Initializes a new <see cref="GetApiDataQuery{T}"/> for the specified API endpoint.
        /// </summary>
        /// <param name="apiUrl">The base URL of the API endpoint to retrieve data from.</param>
        /// <param name="bypassCache">If <see langword="true"/>, bypass cached responses and fetch fresh data; otherwise allow using cached results.</param>
        public GetApiDataQuery(string apiUrl, bool bypassCache = false)
        {
            ApiUrl = apiUrl;
            BypassCache = bypassCache;
        }
    }
}