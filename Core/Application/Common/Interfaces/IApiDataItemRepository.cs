using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.Enums;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Repository interface for ApiDataItem aggregate root.
    /// Provides data access methods for ApiDataItem entity following Repository pattern.
    /// </summary>
    /// <remarks>
    /// This interface abstracts data access for ApiDataItem entities, supporting
    /// external API data synchronization, caching, and freshness management.
    /// 
    /// Key Responsibilities:
    /// - API data CRUD operations
    /// - Data lookup by various criteria (ID, ExternalId, Status)
    /// - Data freshness tracking and management
    /// - Stale data detection and cleanup
    /// - Source attribution and metadata management
    /// 
    /// Benefits:
    /// - Decouples API data management from infrastructure
    /// - Enables unit testing with mock repositories
    /// - Provides clear contract for data operations
    /// - Supports caching and freshness strategies
    /// 
    /// Implementation Note:
    /// The concrete implementation should be in the Infrastructure layer,
    /// typically using Entity Framework Core with optimized queries for
    /// freshness checks (indexed on LastSyncedAt and Status).
    /// 
    /// Usage Example:
    /// ```csharp
    /// public class GetApiDataQueryHandler : IRequestHandler<GetApiDataQuery, Result<List<ApiDataItem>>>
    /// {
    ///     private readonly IApiDataItemRepository _repository;
    ///     private readonly IApiIntegrationService _apiService;
    ///     
    ///     public async Task<Result<List<ApiDataItem>>> Handle(GetApiDataQuery request, CancellationToken cancellationToken)
    ///     {
    ///         // Check cache first
    ///         var items = await _repository.GetActiveItemsAsync(cancellationToken);
    ///         
    ///         // Refresh stale data
    ///         var staleItems = items.Where(i => i.NeedsRefresh(TimeSpan.FromHours(1))).ToList();
    ///         foreach (var item in staleItems)
    ///         {
    ///             var freshData = await _apiService.GetDataByIdAsync(item.ExternalId);
    ///             item.UpdateFromExternalSource(freshData.Name, freshData.Description);
    ///             await _repository.UpdateAsync(item, cancellationToken);
    ///         }
    ///         
    ///         return Result<List<ApiDataItem>>.Ok(items);
    ///     }
    /// }
    /// ```
    /// </remarks>
    public interface IApiDataItemRepository
    {
        /// <summary>
        /// Gets an API data item by its unique ID.
        /// </summary>
        /// <param name="id">The item's unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The item if found, null otherwise.</returns>
        Task<ApiDataItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an API data item by its external system ID.
        /// </summary>
        /// <param name="externalId">The external system's identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The item if found, null otherwise.</returns>
        /// <remarks>
        /// Used to find existing items when synchronizing from external APIs.
        /// Prevents duplicate storage of the same external data.
        /// Should be optimized with database index on ExternalId.
        /// </remarks>
        Task<ApiDataItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active (non-deleted, non-stale) API data items.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active items.</returns>
        /// <remarks>
        /// Returns only items with Active status.
        /// Used for serving fresh data to API consumers.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all API data items regardless of status.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all items.</returns>
        /// <remarks>
        /// Includes active, stale, and deleted items.
        /// Used for administrative purposes and full data exports.
        /// Consider pagination for large datasets.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> GetAllItemsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets API data items by status.
        /// </summary>
        /// <param name="status">The status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of items with the specified status.</returns>
        /// <remarks>
        /// Useful for batch operations like refreshing stale items
        /// or cleaning up deleted items.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> GetItemsByStatusAsync(DataStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets API data items that need refresh based on age threshold.
        /// </summary>
        /// <param name="maxAge">Maximum acceptable age before refresh.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of items needing refresh.</returns>
        /// <remarks>
        /// Returns items where (UtcNow - LastSyncedAt) > maxAge.
        /// Used by background refresh jobs to identify stale data.
        /// Should be optimized with index on LastSyncedAt.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> GetItemsNeedingRefreshAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets API data items from a specific source URL.
        /// </summary>
        /// <param name="sourceUrl">The source API URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of items from the specified source.</returns>
        /// <remarks>
        /// Useful for managing data from multiple API sources.
        /// Enables source-specific refresh or cleanup operations.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> GetItemsBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for API data items by name (case-insensitive partial match).
        /// </summary>
        /// <param name="searchTerm">The search term to match against item names.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of items matching the search term.</returns>
        /// <remarks>
        /// Performs case-insensitive partial match on Name field.
        /// Useful for search functionality in API endpoints.
        /// Consider adding pagination for large result sets.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets items with specific metadata key-value pair.
        /// </summary>
        /// <param name="metadataKey">The metadata key to search for.</param>
        /// <param name="metadataValue">The metadata value to match (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of items with matching metadata.</returns>
        /// <remarks>
        /// Searches JSON metadata field for specific keys/values.
        /// Implementation depends on database JSON support.
        /// </remarks>
        Task<IReadOnlyList<ApiDataItem>> GetItemsByMetadataAsync(string metadataKey, object? metadataValue = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an item with the specified external ID already exists.
        /// </summary>
        /// <param name="externalId">The external system's identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if exists, false otherwise.</returns>
        /// <remarks>
        /// Used to prevent duplicate storage of external data.
        /// Should check against non-deleted items only.
        /// </remarks>
        Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new API data item to the repository.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Creates a new record in the database.
        /// Typically called after fetching data from external API.
        /// </remarks>
        Task AddAsync(ApiDataItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple API data items in batch.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Optimized batch operation for bulk imports.
        /// More efficient than adding one by one.
        /// </remarks>
        Task AddRangeAsync(IEnumerable<ApiDataItem> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing API data item in the repository.
        /// </summary>
        /// <param name="item">The item to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Updates item data, status, metadata, etc.
        /// Called after refreshing data from external API.
        /// </remarks>
        Task UpdateAsync(ApiDataItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple items in batch.
        /// </summary>
        /// <param name="items">The items to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of items updated.</returns>
        /// <remarks>
        /// Optimized batch operation for bulk updates.
        /// Useful for background refresh jobs.
        /// </remarks>
        Task<int> UpdateRangeAsync(IEnumerable<ApiDataItem> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an API data item (soft delete).
        /// </summary>
        /// <param name="item">The item to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Soft delete - marks item as deleted but preserves data.
        /// Use item.MarkAsDeleted() before calling this.
        /// </remarks>
        Task DeleteAsync(ApiDataItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently removes deleted items (hard delete for cleanup).
        /// </summary>
        /// <param name="olderThan">Only delete items deleted before this date.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of items permanently deleted.</returns>
        /// <remarks>
        /// Hard delete for cleanup of old deleted items.
        /// Should be used carefully, typically by scheduled cleanup jobs.
        /// Permanently removes data - cannot be recovered.
        /// </remarks>
        Task<int> PermanentlyDeleteOldItemsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks all items from a specific source as stale.
        /// </summary>
        /// <param name="sourceUrl">The source API URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of items marked as stale.</returns>
        /// <remarks>
        /// Bulk operation to invalidate cache for an entire API source.
        /// Useful when external API structure changes.
        /// </remarks>
        Task<int> MarkSourceAsStaleAsync(string sourceUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about API data items for monitoring.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics about data items.</returns>
        /// <remarks>
        /// Provides insights for cache performance and data freshness monitoring.
        /// Returns counts by status, average age, etc.
        /// </remarks>
        Task<ApiDataStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of entities affected.</returns>
        /// <remarks>
        /// Commits the unit of work transaction.
        /// Should be called after Add/Update/Delete operations.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
