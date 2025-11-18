using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Application.Common.Mapping;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using SecureCleanApiWaf.Core.Domain.Entities;

namespace SecureCleanApiWaf.Infrastructure.Services
{
    /// <summary>
    /// Service for business logic and third-party API integration using IHttpClientFactory.
    /// Implements global try-catch, logging, latency metrics, and the Result Pattern.
    /// </summary>
    /// <remarks>
    /// Enhanced with domain-specific methods for ApiDataItem entities.
    /// Provides both generic and domain-aware API integration capabilities.
    /// </remarks>
    public class ApiIntegrationService : IApiIntegrationService
    {
        // IHttpClientFactory is the recommended way to manage HttpClient instances in .NET for API integrations.
        // It avoids common issues such as socket exhaustion by reusing handlers, enables centralized configuration,
        // supports named/typed clients, and integrates with dependency injection for testability and maintainability.
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApiIntegrationService> _logger;
        private readonly ApiDataMapper _mapper;

        public ApiIntegrationService(
            IHttpClientFactory httpClientFactory, 
            ILogger<ApiIntegrationService> logger,
            ApiDataMapper mapper)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets data from a third-party API using the Result Pattern.
        /// Logs request, response, exceptions, and latency.
        /// </summary>
        /// <param name="apiUrl">The third-party API endpoint. Relative path</param>
        /// <returns>Result containing API response or error details.</returns>
        public async Task<Result<T>> GetAllDataAsync<T>(string apiUrl)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Requesting data from {ApiUrl}", apiUrl); 
                var client = _httpClientFactory.CreateClient("ThirdPartyApiClient"); // create named client
                
                var response = await client.GetAsync(apiUrl); // The apiUrl parameter in GetDataAsync should be a relative path (e.g., $"api/data/{id}") since the named client has a base address set. If it's a full URL, the base address will be ignored. 

                var duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Received response from {ApiUrl} in {Duration}ms", apiUrl, duration.TotalMilliseconds);

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // Deserialize JSON response to type T
                var data = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                return Result<T>.Ok(data);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Error calling {ApiUrl} after {Duration}ms", apiUrl, duration.TotalMilliseconds);
                return Result<T>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Gets data by ID from a third-party API using the Result Pattern.
        /// Logs request, response, exceptions, and latency.
        /// </summary>
        /// <param name="apiUrl">The third-party API endpoint. Relative path</param>
        /// <param name="id">The identifier for the data to retrieve</param>
        /// <returns>Result containing API response or error details.</returns>
        public async Task<Result<T>> GetDataByIdAsync<T>(string apiUrl, string id)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var fullPath = $"{apiUrl}/{id}";
                
                _logger.LogInformation("Requesting data by ID from {ApiUrl}", fullPath);
                
                var client = _httpClientFactory.CreateClient("ThirdPartyApiClient");
                var response = await client.GetAsync(fullPath);
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Received response from {ApiUrl} in {Duration}ms", fullPath, duration.TotalMilliseconds);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return Result<T>.Ok(data);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Error calling {ApiUrl} after {Duration}ms", apiUrl, duration.TotalMilliseconds);
                return Result<T>.Fail(ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ApiDataItem>>> GetApiDataItemsAsync(string apiUrl)
        {
            try
            {
                _logger.LogInformation("Fetching API data items from {ApiUrl}", apiUrl);

                // Fetch raw data from API
                var apiResult = await GetAllDataAsync<dynamic>(apiUrl);
                
                if (!apiResult.Success)
                {
                    return Result<List<ApiDataItem>>.Fail(apiResult.Error);
                }

                // Map to domain entities
                var domainEntities = _mapper.MapToApiDataItems(apiResult.Data, apiUrl);
                
                if (!domainEntities.Any())
                {
                    _logger.LogWarning("No items mapped from API response for {ApiUrl}", apiUrl);
                    return Result<List<ApiDataItem>>.Fail("No valid data items found in API response");
                }

                _logger.LogInformation("Successfully mapped {Count} API items to domain entities from {ApiUrl}", 
                    (object)domainEntities.Count, apiUrl);

                return Result<List<ApiDataItem>>.Ok(domainEntities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API data items from {ApiUrl}", apiUrl);
                return Result<List<ApiDataItem>>.Fail($"Error mapping API data: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ApiDataItem>> GetApiDataItemByIdAsync(string apiUrl, string id)
        {
            try
            {
                _logger.LogInformation("Fetching API data item {Id} from {ApiUrl}", id, apiUrl);

                // Fetch raw data from API
                var apiResult = await GetDataByIdAsync<dynamic>(apiUrl, id);
                
                if (!apiResult.Success)
                {
                    return Result<ApiDataItem>.Fail(apiResult.Error);
                }

                // Map to domain entity
                var domainEntity = _mapper.MapToApiDataItem(apiResult.Data, $"{apiUrl}/{id}");
                
                if (domainEntity == null)
                {
                    _logger.LogWarning("Failed to map API item {Id} to domain entity", id);
                    return Result<ApiDataItem>.Fail("Failed to map API response to domain entity");
                }

                _logger.LogInformation("Successfully mapped API item {Id} to domain entity", id);

                return Result<ApiDataItem>.Ok(domainEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API data item {Id} from {ApiUrl}", id, apiUrl);
                return Result<ApiDataItem>.Fail($"Error mapping API data: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> CheckApiHealthAsync(string apiUrl)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Checking API health for {ApiUrl}", apiUrl);
                
                var client = _httpClientFactory.CreateClient("ThirdPartyApiClient");
                var response = await client.GetAsync(apiUrl);
                
                var duration = DateTime.UtcNow - startTime;
                
                var isHealthy = response.IsSuccessStatusCode;
                
                if (isHealthy)
                {
                    _logger.LogInformation("API health check successful for {ApiUrl} in {Duration}ms", 
                        apiUrl, duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("API health check failed for {ApiUrl}. Status: {StatusCode} in {Duration}ms", 
                        apiUrl, response.StatusCode, duration.TotalMilliseconds);
                }

                return Result<bool>.Ok(isHealthy);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "API health check error for {ApiUrl} after {Duration}ms", 
                    apiUrl, duration.TotalMilliseconds);
                return Result<bool>.Fail($"Health check failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<PaginatedResponseDto<T>>> GetPaginatedDataAsync<T>(string apiUrl, int page, int pageSize)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Construct URL with pagination query parameters
                var paginatedUrl = $"{apiUrl}?page={page}&pageSize={pageSize}";
                
                _logger.LogInformation("Requesting paginated data from {ApiUrl} (Page: {Page}, PageSize: {PageSize})", 
                    apiUrl, page, pageSize);
                
                var client = _httpClientFactory.CreateClient("ThirdPartyApiClient");
                var response = await client.GetAsync(paginatedUrl);
                
                var duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Received paginated response from {ApiUrl} in {Duration}ms", 
                    paginatedUrl, duration.TotalMilliseconds);

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // Try to deserialize as paginated response
                var paginatedData = JsonSerializer.Deserialize<PaginatedResponseDto<T>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (paginatedData == null)
                {
                    // If API doesn't return paginated format, try to deserialize as simple list
                    var items = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (items != null)
                    {
                        paginatedData = new PaginatedResponseDto<T>
                        {
                            Items = items,
                            Page = page,
                            PageSize = pageSize,
                            TotalItems = items.Count,
                            TotalPages = 1
                        };
                    }
                }

                return Result<PaginatedResponseDto<T>>.Ok(paginatedData);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Error calling paginated endpoint {ApiUrl} after {Duration}ms", 
                    apiUrl, duration.TotalMilliseconds);
                return Result<PaginatedResponseDto<T>>.Fail(ex.Message);
            }
        }
    }
}
