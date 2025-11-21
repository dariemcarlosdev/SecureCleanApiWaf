using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.Profiles;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Features.SampleData.Queries
{
    /// <summary>
    /// Query for fetching API data with choice of mapping strategy.
    /// </summary>
    public class GetApiDataWithMappingQuery : IRequest<Result<List<ApiDataItemDto>>>
    {
        public string ApiUrl { get; }
        public bool UseAutoMapper { get; }

        /// <summary>
        /// Initializes a new <see cref="GetApiDataWithMappingQuery"/> with the specified API URL and mapping preference.
        /// </summary>
        /// <param name="apiUrl">The target API URL to fetch data from.</param>
        /// <param name="useAutoMapper">If true, use AutoMapper for mapping; otherwise use an alternative/manual mapping strategy.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiUrl"/> is null.</exception>
        public GetApiDataWithMappingQuery(string apiUrl, bool useAutoMapper = true)
        {
            ApiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
            UseAutoMapper = useAutoMapper;
        }
    }
}