using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for third-party API integration service.
    /// Abstracts external API calls for testability and maintainability.
    /// </summary>
    /// <remarks>
    /// This interface follows the Dependency Inversion Principle by allowing the Application layer
    /// to define the contract while the Infrastructure layer provides the implementation.
    /// This enables easy mocking in unit tests and flexibility to swap implementations.
    /// 
    /// Integration with Domain Layer:
    /// - Provides both generic and domain-specific methods
    /// - Supports mapping to domain entities through specialized methods
    /// - Maintains flexibility for different API response structures
    /// </remarks>
    public interface IApiIntegrationService
    {
        /// <summary>
        /// Gets all data from the specified API endpoint.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve and deserialize.</typeparam>
        /// <param name="apiUrl">The relative path to the API endpoint (e.g., "api/data").</param>
        /// <returns>A Result containing the data if successful, or an error message if failed.</returns>
        /// <remarks>
        /// The apiUrl should be a relative path since the HttpClient is configured with a base address.
        /// Example: "api/products" or "api/users"
        /// 
        /// Generic method for flexibility - use when working with DTOs or dynamic responses.
        /// </remarks>
        Task<Result<T>> GetAllDataAsync<T>(string apiUrl);

        /// <summary>
        /// Gets data by ID from the specified API endpoint.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve and deserialize.</typeparam>
        /// <param name="apiUrl">The relative path to the API endpoint (e.g., "api/data").</param>
        /// <param name="id">The unique identifier of the resource to retrieve.</param>
        /// <returns>A Result containing the data if successful, or an error message if failed.</returns>
        /// <remarks>
        /// The method constructs the full path by appending the id to the apiUrl.
        /// Example: apiUrl="api/products", id="123" ? GET /api/products/123
        /// 
        /// Generic method for flexibility - use when working with DTOs or dynamic responses.
        /// </remarks>
        Task<Result<T>> GetDataByIdAsync<T>(string apiUrl, string id);

        /// <summary>
        /// Gets API data and maps it to ApiDataItem domain entities.
        /// </summary>
        /// <param name="apiUrl">The relative path to the API endpoint.</param>
        /// <returns>A Result containing list of ApiDataItem domain entities.</returns>
        /// <remarks>
        /// Domain-specific method that:
        /// - Fetches data from external API
        /// - Automatically maps to ApiDataItem entities
        /// - Validates data structure
        /// - Handles mapping errors gracefully
        /// 
        /// Use this method when you need to work directly with domain entities.
        /// 
        /// Example:
        /// ```csharp
        /// var result = await _apiService.GetApiDataItemsAsync("api/products");
        /// if (result.Success)
        /// {
        ///     foreach (var item in result.Data)
        ///     {
        ///         await _repository.AddAsync(item, cancellationToken);
        ///     }
        /// }
        /// ```
        /// </remarks>
        Task<Result<List<ApiDataItem>>> GetApiDataItemsAsync(string apiUrl);

        /// <summary>
        /// Gets a single API data item by ID and maps it to ApiDataItem domain entity.
        /// </summary>
        /// <param name="apiUrl">The relative path to the API endpoint.</param>
        /// <param name="id">The unique identifier of the resource.</param>
        /// <returns>A Result containing the ApiDataItem domain entity.</returns>
        /// <remarks>
        /// Domain-specific method for retrieving and mapping a single item.
        /// Combines GetDataByIdAsync with automatic domain entity mapping.
        /// 
        /// Example:
        /// ```csharp
        /// var result = await _apiService.GetApiDataItemByIdAsync("api/products", "123");
        /// if (result.Success)
        /// {
        ///     var existingItem = await _repository.GetByExternalIdAsync(result.Data.ExternalId);
        ///     if (existingItem == null)
        ///     {
        ///         await _repository.AddAsync(result.Data);
        ///     }
        /// }
        /// ```
        /// </remarks>
        Task<Result<ApiDataItem>> GetApiDataItemByIdAsync(string apiUrl, string id);

        /// <summary>
        /// Checks if the API endpoint is available and responding.
        /// </summary>
        /// <param name="apiUrl">The relative path to the API endpoint.</param>
        /// <returns>A Result indicating if the API is healthy.</returns>
        /// <remarks>
        /// Health check method for monitoring external API availability.
        /// Useful for:
        /// - Pre-flight checks before data operations
        /// - Circuit breaker pattern implementation
        /// - Health monitoring dashboards
        /// - Graceful degradation strategies
        /// 
        /// Example:
        /// ```csharp
        /// var healthCheck = await _apiService.CheckApiHealthAsync("api/health");
        /// if (!healthCheck.Success)
        /// {
        ///     // Use cached data or return error
        ///     _logger.LogWarning("External API unavailable, using cached data");
        /// }
        /// ```
        /// </remarks>
        Task<Result<bool>> CheckApiHealthAsync(string apiUrl);

        /// <summary>
        /// Gets paginated data from the specified API endpoint.
        /// </summary>
        /// <typeparam name="T">The type of data items in the page.</typeparam>
        /// <param name="apiUrl">The relative path to the API endpoint.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A Result containing the paginated response.</returns>
        /// <remarks>
        /// Supports APIs with pagination capabilities.
        /// Automatically constructs query parameters for paging.
        /// 
        /// Example:
        /// ```csharp
        /// var result = await _apiService.GetPaginatedDataAsync<ProductDto>(
        ///     "api/products", 
        ///     page: 1, 
        ///     pageSize: 50);
        /// ```
        /// </remarks>
        Task<Result<PaginatedResponseDto<T>>> GetPaginatedDataAsync<T>(string apiUrl, int page, int pageSize);
    }
}
