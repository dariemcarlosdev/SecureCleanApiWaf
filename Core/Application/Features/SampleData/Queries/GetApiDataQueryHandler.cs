using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Application.Common.Mapping;
using SecureCleanApiWaf.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace SecureCleanApiWaf.Core.Application.Features.SampleData.Queries
{
    /// <summary>
    /// Query Handler for retrieving API data using ApiDataItem entity and repository pattern.
    /// This handler implements IRequestHandler from MediatR within the CQRS pattern.
    /// </summary>
    /// <remarks>
    /// This handler fully integrates with the domain layer through ApiDataItem entity and IApiDataItemRepository,
    /// following domain-driven design principles. It implements a cache-first strategy with automatic
    /// refresh of stale data from external APIs.
    /// 
    /// Key Responsibilities:
    /// - Check repository cache for existing data
    /// - Fetch from external API if not cached or stale
    /// - Map API responses to ApiDataItem domain entities
    /// - Persist data through repository
    /// - Handle data freshness and staleness
    /// - Log operations for monitoring
    /// 
    /// Integration Points:
    /// - Uses IApiDataItemRepository for data persistence
    /// - Uses IApiIntegrationService for external API calls
    /// - Uses ApiDataMapper to convert API responses to domain entities
    /// - Works with ApiDataItem domain entity
    /// - Follows Result<T> pattern for error handling
    /// 
    /// Domain Integration Benefits:
    /// - Enforces business rules through ApiDataItem entity
    /// - Tracks data freshness and staleness
    /// - Provides rich metadata management
    /// - Supports audit trail
    /// - Enables cache invalidation strategies
    /// 
    /// Caching Strategy:
    /// 1. Check repository for existing data by SourceUrl
    /// 2. If found and fresh (< 1 hour old), return cached data
    /// 3. If found but stale, refresh from API and update
    /// 4. If not found, fetch from API and create new entities
    /// 5. Persist all changes through repository
    /// 
    /// Performance Features:
    /// - Database-backed caching (faster than external API)
    /// - Stale-while-revalidate pattern (returns stale data while refreshing)
    /// - Batch operations support
    /// - Configurable freshness thresholds
    /// </remarks>
    /// <typeparam name="T">The type of data to be returned (typically List<ApiDataItem> or custom DTO).</typeparam>
    public class GetApiDataQueryHandler<T> : IRequestHandler<GetApiDataQuery<T>, Result<T>>
    {
        private readonly IApiDataItemRepository _repository;
        private readonly IApiIntegrationService _apiService;
        private readonly ApiDataMapper _mapper;
        private readonly ILogger<GetApiDataQueryHandler<T>> _logger;

        // Default freshness threshold: data older than this needs refresh
        private static readonly TimeSpan DefaultFreshnessThreshold = TimeSpan.FromHours(1);

        /// <summary>
        /// Initializes a new instance of the GetApiDataQueryHandler class.
        /// </summary>
        /// <param name="repository">Repository for ApiDataItem persistence and retrieval.</param>
        /// <param name="apiService">Service for external API integration operations.</param>
        /// <param name="mapper">Mapper for converting API responses to domain entities.</param>
        /// <summary>
        /// Initializes a new instance of <see cref="GetApiDataQueryHandler{T}"/> with its required dependencies.
        /// </summary>
        /// <param name="repository">Repository for persisting and retrieving ApiDataItem domain entities.</param>
        /// <param name="apiService">Service used to fetch data from the external API.</param>
        /// <param name="mapper">Mapper that converts external API responses into ApiDataItem entities.</param>
        /// <param name="logger">Logger for the handler.</param>
        public GetApiDataQueryHandler(
            IApiDataItemRepository repository,
            IApiIntegrationService apiService,
            ApiDataMapper mapper,
            ILogger<GetApiDataQueryHandler<T>> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles a request to retrieve data from cache or external API asynchronously.
        /// </summary>
        /// <param name="request">The query containing the API URL for data retrieval.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <summary>
        /// Handles a GetApiDataQuery by returning API data using a cache-first strategy and synchronizing repository state.
        /// </summary>
        /// <param name="request">The query containing the API URL to fetch and the requested response type.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A Result&lt;T&gt; containing the mapped response data on success; a failure Result with an error message on failure.</returns>
        public async Task<Result<T>> Handle(GetApiDataQuery<T> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing GetApiDataQuery for URL: {ApiUrl}", request.ApiUrl);

                // ===== STEP 1: Check Repository Cache First =====
                var cachedItems = await _repository.GetItemsBySourceUrlAsync(request.ApiUrl, cancellationToken);
                
                _logger.LogInformation("Found {Count} cached items for URL: {ApiUrl}", 
                    cachedItems.Count, request.ApiUrl);

                // ===== STEP 2: Determine if Refresh is Needed =====
                var needsRefresh = !cachedItems.Any() || 
                    cachedItems.Any(item => item.NeedsRefresh(DefaultFreshnessThreshold));

                if (!needsRefresh)
                {
                    // Return fresh cached data
                    _logger.LogInformation("Returning {Count} fresh cached items", cachedItems.Count);
                    return MapToResponseType(cachedItems.Where(i => i.Status == Core.Domain.Enums.DataStatus.Active).ToList());
                }

                // ===== STEP 3: Fetch Fresh Data from External API =====
                _logger.LogInformation("Fetching fresh data from external API: {ApiUrl}", request.ApiUrl);
                
                var apiResult = await _apiService.GetAllDataAsync<dynamic>(request.ApiUrl);
                
                if (!apiResult.Success)
                {
                    _logger.LogWarning("Failed to fetch data from external API. URL: {ApiUrl}, Error: {Error}",
                        request.ApiUrl, apiResult.Error);
                    
                    // Return stale data if available (stale-while-revalidate pattern)
                    if (cachedItems.Any())
                    {
                        _logger.LogInformation("Returning {Count} stale cached items due to API failure", 
                            cachedItems.Count);
                        
                        // Mark items as stale for future refresh attempts
                        foreach (var item in cachedItems)
                        {
                            item.MarkAsStale();
                        }
                        await _repository.UpdateRangeAsync(cachedItems, cancellationToken);
                        await _repository.SaveChangesAsync(cancellationToken);
                        
                        return MapToResponseType(cachedItems.Where(i => i.Status != Core.Domain.Enums.DataStatus.Deleted).ToList());
                    }
                    
                    return Result<T>.Fail(apiResult.Error);
                }

                // ===== STEP 4: Map API Response to Domain Entities =====
                var freshDomainEntities = _mapper.MapToApiDataItems(apiResult.Data, request.ApiUrl);
                
                _logger.LogInformation("Mapped {Count} items from API response", (object)freshDomainEntities.Count);

                if (!freshDomainEntities.Any())
                {
                    _logger.LogWarning("No items mapped from API response");
                    return Result<T>.Fail("No data available from API");
                }

                // ===== STEP 5: Sync with Repository (Create/Update) =====
                var syncedItems = new List<ApiDataItem>();
                
                foreach (var freshItem in freshDomainEntities)
                {
                    var existingItem = await _repository.GetByExternalIdAsync(
                        freshItem.ExternalId, cancellationToken);
                    
                    if (existingItem == null)
                    {
                        // Create new entity
                        await _repository.AddAsync(freshItem, cancellationToken);
                        syncedItems.Add(freshItem);
                        
                        _logger.LogDebug("Created new ApiDataItem: {ExternalId}", (object)freshItem.ExternalId);
                    }
                    else
                    {
                        // Update existing entity
                        existingItem.UpdateFromExternalSource(freshItem.Name, freshItem.Description);
                        
                        // Copy metadata
                        existingItem.ClearMetadata();
                        foreach (var metadata in freshItem.Metadata)
                        {
                            existingItem.AddMetadata(metadata.Key, metadata.Value);
                        }
                        
                        await _repository.UpdateAsync(existingItem, cancellationToken);
                        syncedItems.Add(existingItem);
                        
                        _logger.LogDebug("Updated ApiDataItem: {ExternalId}", (object)existingItem.ExternalId);
                    }
                }

                // ===== STEP 6: Handle Deletions (items in cache but not in API) =====
                // Extract ExternalIds from fresh entities
                var apiExternalIds = new HashSet<string>();
                foreach (var item in freshDomainEntities)
                {
                    apiExternalIds.Add(item.ExternalId);
                }
                
                var itemsToDelete = cachedItems
                    .Where(cached => !apiExternalIds.Contains(cached.ExternalId))
                    .ToList();
                
                if (itemsToDelete.Any())
                {
                    foreach (var itemToDelete in itemsToDelete)
                    {
                        itemToDelete.MarkAsDeleted("No longer exists in external API");
                        await _repository.UpdateAsync(itemToDelete, cancellationToken);
                    }
                    
                    _logger.LogInformation("Marked {Count} items as deleted (not in API response)", 
                        itemsToDelete.Count);
                }

                // ===== STEP 7: Persist All Changes =====
                var changeCount = await _repository.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Saved {ChangeCount} changes to repository", changeCount);

                // ===== STEP 8: Return Active Items =====
                return MapToResponseType(syncedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GetApiDataQuery for URL: {ApiUrl}", request.ApiUrl);
                return Result<T>.Fail($"An error occurred while fetching data: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps ApiDataItem entities to the expected response type T.
        /// </summary>
        /// <param name="items">List of domain entities.</param>
        /// <summary>
        /// Map domain ApiDataItem entities to the requested response shape T (e.g., List&lt;ApiDataItem&gt;, IEnumerable&lt;ApiDataItem&gt;, object, or a compatible custom DTO).
        /// </summary>
        /// <returns>`Result<T>` containing the mapped response value when mapping succeeds; a failed `Result<T>` with an error message when mapping fails.</returns>
        private Result<T> MapToResponseType(IReadOnlyList<ApiDataItem> items)
        {
            try
            {
                // Handle different response types
                
                // Case 1: T is List<ApiDataItem> - direct return
                if (typeof(T) == typeof(List<ApiDataItem>))
                {
                    return Result<T>.Ok((T)(object)items.ToList());
                }
                
                // Case 2: T is IEnumerable<ApiDataItem>
                if (typeof(T) == typeof(IEnumerable<ApiDataItem>))
                {
                    return Result<T>.Ok((T)(object)items);
                }
                
                // Case 3: T is object - serialize for flexibility
                if (typeof(T) == typeof(object))
                {
                    return Result<T>.Ok((T)(object)items);
                }

                // Case 4: Custom DTO mapping (extend as needed)
                // For now, attempt to cast/convert
                var result = (T)(object)items;
                return Result<T>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping domain entities to response type {Type}", typeof(T).Name);
                return Result<T>.Fail($"Error mapping response: {ex.Message}");
            }
        }
    }
}