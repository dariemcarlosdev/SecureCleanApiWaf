using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SecureCleanApiWaf.Core.Application.Features.SampleData.Queries
{
    /// <summary>
    /// Handles queries to retrieve API data by identifier and returns the result as a strongly typed object.
    /// </summary>
    /// <remarks>This handler uses the <see cref="IApiIntegrationService"/> abstraction to perform asynchronous API calls for
    /// fetching data by ID. It returns a Result<T> indicating success or failure, including error information if the
    /// operation does not succeed. This class is typically used in a MediatR pipeline to process GetApiDataByIdQuery<T>
    /// requests. By depending on the interface rather than the concrete implementation, this handler follows the 
    /// Dependency Inversion Principle and is easily testable.</remarks>
    /// <typeparam name="T">The type of the data to be retrieved from the API.</typeparam>
    public class GetApiDataByIdQueryHandler<T> : IRequestHandler<GetApiDataByIdQuery<T>, Result<T>>
    {
        private readonly IApiIntegrationService _apiService;

        /// <summary>Initializes a new instance of GetApiDataByIdQueryHandler with the required API integration service.</summary>
        public GetApiDataByIdQueryHandler(IApiIntegrationService apiIntegrationService)
        {
            _apiService = apiIntegrationService;
        }

        /// <summary>
        /// Handles a query to retrieve API data by its identifier and returns the result as a strongly typed object.
        /// </summary>
        /// <param name="request">The query containing the API path and the identifier of the data to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <summary>
        /// Handles a GetApiDataByIdQuery by retrieving the specified resource from the API and returning it wrapped in a Result<T>.
        /// </summary>
        /// <param name="request">Query containing the API path and the resource ID to retrieve.</param>
        /// <returns>A Result&lt;T&gt; containing the fetched data when the API call succeeds; otherwise a failure Result with an error message.</returns>
        public async Task<Result<T>> Handle(GetApiDataByIdQuery<T> request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _apiService.GetDataByIdAsync<T>(request.ApiPath, request.Id);
                return result.Success ? Result<T>.Ok(result.Data) : Result<T>.Fail(result.Error);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail($"Exception: {ex.Message}");
            }
        }
    }
}