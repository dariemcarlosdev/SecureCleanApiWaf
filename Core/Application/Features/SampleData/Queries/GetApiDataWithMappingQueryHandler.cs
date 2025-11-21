using MediatR;
using AutoMapper;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Application.Common.Profiles;
using SecureCleanApiWaf.Core.Application.Common.Mapping;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using SecureCleanApiWaf.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace SecureCleanApiWaf.Core.Application.Features.SampleData.Queries
{
    /// <summary>
    /// Query Handler for retrieving API data with AutoMapper for known DTO structures.
    /// This handler demonstrates the hybrid approach using both AutoMapper and custom mapping.
    /// </summary>
    /// <remarks>
    /// **Hybrid Mapping Strategy:**
    /// 
    /// **Scenario 1: Known API Structure (Recommended)**
    /// - Fetch as strongly-typed DTO (ApiItemDto)
    /// - Use AutoMapper for entity conversion
    /// - Better performance, type safety, compile-time validation
    /// 
    /// **Scenario 2: Unknown/Dynamic API Structure**
    /// - Fetch as dynamic
    /// - Use custom ApiDataMapper
    /// - Flexible, handles varying property names
    /// 
    /// This handler supports both approaches and chooses based on API predictability.
    /// </remarks>
    public class GetApiDataWithMappingQueryHandler : IRequestHandler<GetApiDataWithMappingQuery, Result<List<ApiDataItemDto>>>
    {
        // Dependencies

        // These are infrastructure services, but serve different proposes fallowing the CQRS pattern
        // Separation of Concerns is maintained by clearly defining responsibilities.        
        private readonly IApiDataItemRepository _repository; // For caching and persistence, Manages stored data in the database Data Management
        private readonly IApiIntegrationService _apiService; // Fetches fresh data from external third-party APIs

        //Both AutoMapper and custom mapper are injected for flexibility
        private readonly IMapper _autoMapper; // For known structures
        private readonly ApiDataMapper _customMapper; // For dynamic/unknown structures
        private readonly ILogger<GetApiDataWithMappingQueryHandler> _logger;

        private static readonly TimeSpan DefaultFreshnessThreshold = TimeSpan.FromHours(1);

        /// <summary>
        /// Initializes a new instance of the handler and validates that all injected dependencies are not null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency (repository, apiService, autoMapper, customMapper, or logger) is null.</exception>
        public GetApiDataWithMappingQueryHandler(
            IApiDataItemRepository repository,
            IApiIntegrationService apiService,
            IMapper autoMapper,
            ApiDataMapper customMapper,
            ILogger<GetApiDataWithMappingQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _autoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            _customMapper = customMapper ?? throw new ArgumentNullException(nameof(customMapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves API data for the specified URL using cache when fresh; otherwise fetches from the external API and maps results using either AutoMapper (for known structures) or a custom dynamic mapper, synchronizes the repository, and returns DTOs.
        /// </summary>
        /// <param name="request">Query containing the API URL and a flag indicating whether to use AutoMapper for mapping known API structures.</param>
        /// <returns>`Result<List<ApiDataItemDto>>` containing the mapped list of API data items on success; on failure the result contains an error message. If the external API call fails but cached data exists, returns stale cached DTOs instead of an error.</returns>
        public async Task<Result<List<ApiDataItemDto>>> Handle(
            GetApiDataWithMappingQuery request, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing GetApiDataWithMappingQuery for URL: {ApiUrl}, UseAutoMapper: {UseAutoMapper}", 
                    request.ApiUrl, request.UseAutoMapper);

                // ===== STEP 1: Check Repository Cache =====
                var cachedItems = await _repository.GetItemsBySourceUrlAsync(request.ApiUrl, cancellationToken);
                
                var needsRefresh = !cachedItems.Any() || 
                    cachedItems.Any(item => item.NeedsRefresh(DefaultFreshnessThreshold));

                if (!needsRefresh)
                {
                    // Return cached data mapped to DTO
                    var cachedDtos = _autoMapper.Map<List<ApiDataItemDto>>(cachedItems.Where(i => i.Status == Core.Domain.Enums.DataStatus.Active).ToList());
                    _logger.LogInformation("Returning {Count} fresh cached items", cachedDtos.Count);
                    return Result<List<ApiDataItemDto>>.Ok(cachedDtos);
                }

                // ===== STEP 2: Fetch from API with Appropriate Method =====
                List<ApiDataItem> freshEntities;

                if (request.UseAutoMapper)
                {
                    // ===== APPROACH 1: AutoMapper (Known API Structure) =====
                    _logger.LogInformation("Using AutoMapper for known API structure");

                    // Fetch as strongly-typed DTO
                    var apiResult = await _apiService.GetAllDataAsync<List<ApiItemDto>>(request.ApiUrl);
                    
                    if (!apiResult.Success)
                    {
                        return HandleApiFailure(apiResult.Error, cachedItems);
                    }

                    // Use AutoMapper for batch conversion
                    freshEntities = new List<ApiDataItem>();
                    foreach (var dto in apiResult.Data)
                    {
                        // Create entity using factory method (sets SourceUrl, LastSyncedAt, etc.)
                        var entity = ApiDataItem.CreateFromExternalSource(
                            externalId: dto.Id,
                            name: dto.Name,
                            description: dto.Description ?? string.Empty,
                            sourceUrl: request.ApiUrl);

                        // Use AutoMapper to populate metadata
                        var mappedEntity = _autoMapper.Map(dto, entity);
                        freshEntities.Add(mappedEntity);
                    }
                }
                else
                {
                    // ===== APPROACH 2: Custom Mapper (Unknown/Dynamic API Structure) =====
                    _logger.LogInformation("Using custom mapper for dynamic API structure");

                    // Fetch as dynamic
                    var apiResult = await _apiService.GetAllDataAsync<dynamic>(request.ApiUrl);
                    
                    if (!apiResult.Success)
                    {
                        return HandleApiFailure(apiResult.Error, cachedItems);
                    }

                    // Use custom mapper with flexible property matching
                    freshEntities = _customMapper.MapToApiDataItems(apiResult.Data, request.ApiUrl);
                }

                if (!freshEntities.Any())
                {
                    _logger.LogWarning("No items mapped from API response");
                    return Result<List<ApiDataItemDto>>.Fail("No data available from API");
                }

                // ===== STEP 3: Sync with Repository =====
                await SyncWithRepository(freshEntities, cachedItems, cancellationToken);

                // ===== STEP 4: Return as DTOs =====
                var resultDtos = _autoMapper.Map<List<ApiDataItemDto>>(freshEntities);
                return Result<List<ApiDataItemDto>>.Ok(resultDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GetApiDataWithMappingQuery");
                return Result<List<ApiDataItemDto>>.Fail($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle an API fetch failure by returning stale cached items when available, otherwise a failure result.
        /// </summary>
        /// <param name="error">Error message describing the API fetch failure.</param>
        /// <param name="cachedItems">Cached entities retrieved from the repository for the requested API URL.</param>
        /// <returns>`Ok` result containing DTOs for cached items whose status is not Deleted if any exist; otherwise a `Fail` result containing the provided error message.</returns>
        private Result<List<ApiDataItemDto>> HandleApiFailure(string error, IReadOnlyList<ApiDataItem> cachedItems)
        {
            _logger.LogWarning("API fetch failed: {Error}", error);

            if (cachedItems.Any())
            {
                // Return stale cached data as DTOs
                var staleDtos = _autoMapper.Map<List<ApiDataItemDto>>(
                    cachedItems.Where(i => i.Status != Core.Domain.Enums.DataStatus.Deleted).ToList());
                
                _logger.LogInformation("Returning {Count} stale cached items", staleDtos.Count);
                return Result<List<ApiDataItemDto>>.Ok(staleDtos);
            }

            return Result<List<ApiDataItemDto>>.Fail(error);
        }

        /// <summary>
        /// Synchronizes freshly mapped ApiDataItem entities with the repository and persists the resulting changes.
        /// </summary>
        /// <param name="freshEntities">Entities produced from the external API to be inserted or used to update existing records.</param>
        /// <param name="cachedItems">Cached items previously retrieved for the same API URL (provided for context; not modified by this method).</param>
        /// <param name="cancellationToken">Token to observe while performing repository operations.</param>
        /// <remarks>
        /// For each item in <paramref name="freshEntities"/>, the method adds it when no repository record exists with the same external ID;
        /// otherwise it updates the existing record's name and description, replaces its metadata with the fresh item's metadata, and saves all changes.
        /// </remarks>
        private async Task SyncWithRepository(
            List<ApiDataItem> freshEntities, 
            IReadOnlyList<ApiDataItem> cachedItems,
            CancellationToken cancellationToken)
        {
            foreach (var freshItem in freshEntities)
            {
                var existingItem = await _repository.GetByExternalIdAsync(
                    freshItem.ExternalId, cancellationToken);
                
                if (existingItem == null)
                {
                    await _repository.AddAsync(freshItem, cancellationToken);
                }
                else
                {
                    existingItem.UpdateFromExternalSource(freshItem.Name, freshItem.Description);
                    existingItem.ClearMetadata();
                    foreach (var metadata in freshItem.Metadata)
                    {
                        existingItem.AddMetadata(metadata.Key, metadata.Value);
                    }
                    await _repository.UpdateAsync(existingItem, cancellationToken);
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}