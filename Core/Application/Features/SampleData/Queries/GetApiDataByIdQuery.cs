using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Behaviors;
using SecureCleanApiWaf.Core.Application.Common.Models;

namespace SecureCleanApiWaf.Core.Application.Features.SampleData.Queries
{
    /// <summary>
    /// Query to get data by Id from an external API.
    /// </summary>
    /// <typeparam name="T">Type of the data to retrieve.</typeparam>
    public class GetApiDataByIdQuery<T> : IRequest<Result<T>>, ICacheable
    {
        public string ApiPath { get; }
        public string Id { get; }
        public bool BypassCache { get; set; }
        public string CacheKey => $"ApiDataById:{ApiPath}:{Id}";
        public int SlidingExpirationInMinutes { get; set; } = 30;
        public int AbsoluteExpirationInMinutes { get; set; } = 60;

        /// <summary>
        /// Initializes a new instance of the GetApiDataByIdQuery class with the specified API path, resource
        /// identifier, and cache bypass option.
        /// </summary>
        /// <param name="apiPath">The relative or absolute path to the API endpoint from which data will be retrieved. Cannot be null or
        /// empty.</param>
        /// <param name="id">The unique identifier of the resource to retrieve from the API. Cannot be null or empty.</param>
        /// <param name="bypassCache">Specifies whether to bypass any cached data and force a fresh retrieval from the API. Set to <see
        /// <summary>
        /// Initializes a query to request an item of type <typeparamref name="T"/> from an external API by its identifier.
        /// </summary>
        /// <param name="apiPath">The API endpoint path that identifies the resource collection or route.</param>
        /// <param name="id">The external API identifier of the resource to retrieve.</param>
        /// <param name="bypassCache">If <see langword="true"/>, instructs handlers to bypass cached results and fetch fresh data; otherwise use cached data when available.</param>
        public GetApiDataByIdQuery(string apiPath, string id, bool bypassCache = false)
        {
            ApiPath = apiPath;
            Id = id;
            BypassCache = bypassCache;
        }
    }
}