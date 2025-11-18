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

        public GetApiDataWithMappingQuery(string apiUrl, bool useAutoMapper = true)
        {
            ApiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
            UseAutoMapper = useAutoMapper;
        }
    }
}
