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
        /// <summary>
/// Retrieves an ApiDataItem by its unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the ApiDataItem to retrieve.</param>
/// <returns>The ApiDataItem with the specified id, or `null` if no matching item is found.</returns>
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
        /// <summary>
/// Retrieves an <see cref="ApiDataItem"/> that matches the specified external system identifier.
/// </summary>
/// <param name="externalId">The identifier assigned to the item by an external system.</param>
/// <returns>The matching <see cref="ApiDataItem"/>, or <c>null</c> if no match exists.</returns>
        Task<ApiDataItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active (non-deleted, non-stale) API data items.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active items.</returns>
        /// <remarks>
        /// Returns only items with Active status.
        /// Used for serving fresh data to API consumers.
        /// <summary>
/// Retrieves all active API data items — items that are not marked as deleted and are not marked as stale.
/// </summary>
/// <returns>A read-only list of active <see cref="ApiDataItem"/> instances.</returns>
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
        /// <summary>
/// Retrieves all API data items regardless of their status (including deleted or stale items).
/// </summary>
/// <returns>A read-only list containing every <see cref="ApiDataItem"/> in the repository.</returns>
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
        /// <summary>
/// Retrieves all ApiDataItem instances that have the specified data status.
/// </summary>
/// <param name="status">The DataStatus value to filter items by.</param>
/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
/// <returns>A read-only list of ApiDataItem objects whose status equals the specified value.</returns>
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
        /// <summary>
/// Finds API data items whose last synchronization time is older than the provided maximum age and therefore need refreshing.
/// </summary>
/// <param name="maxAge">The maximum allowed age since an item's LastSyncedAt; items older than this value are considered to need refresh.</param>
/// <returns>A read-only list of ApiDataItem instances whose LastSyncedAt age exceeds <paramref name="maxAge"/>.</returns>
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
        /// <summary>
/// Retrieves API data items that originate from the specified source URL.
/// </summary>
/// <param name="sourceUrl">The source URL to filter items by.</param>
/// <returns>A read-only list of <see cref="ApiDataItem"/> instances that originate from the specified source URL.</returns>
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
        /// <summary>
/// Finds ApiDataItem objects whose names match a case-insensitive partial search term.
/// </summary>
/// <param name="searchTerm">The substring to match against item names; matching is case-insensitive and treats the term as a partial search.</param>
/// <returns>A read-only list of ApiDataItem instances whose names match the provided search term.</returns>
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
        /// <summary>
/// Retrieves ApiDataItem entities that contain a specified metadata key, optionally filtered to a specific metadata value.
/// </summary>
/// <param name="metadataKey">The metadata key to match.</param>
/// <param name="metadataValue">An optional metadata value to match; when null, items containing the key are returned regardless of the value.</param>
/// <returns>A read-only list of ApiDataItem objects matching the metadata criteria.</returns>
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
        /// <summary>
/// Checks whether a non-deleted ApiDataItem with the specified external system identifier exists.
/// </summary>
/// <param name="externalId">The external system identifier to check for.</param>
/// <returns>`true` if a non-deleted item with the given external ID exists, `false` otherwise.</returns>
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
        /// <summary>
/// Adds a new ApiDataItem to the repository.
/// </summary>
/// <param name="item">The ApiDataItem to add; must not be null. The item will be tracked and persisted when SaveChangesAsync is called.</param>
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
        /// <summary>
/// Adds multiple ApiDataItem instances to the repository.
/// </summary>
/// <param name="items">The collection of ApiDataItem objects to add.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
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
        /// <summary>
/// Applies the provided ApiDataItem's updated values to the repository state.
/// </summary>
/// <param name="item">The ApiDataItem containing the updated data to store.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
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
        /// <summary>
/// Updates multiple ApiDataItem entities in a single batch operation.
/// </summary>
/// <param name="items">The collection of items to update.</param>
/// <param name="cancellationToken">Token to observe for cancellation.</param>
/// <returns>The number of items updated.</returns>
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
        /// <summary>
/// Marks the given ApiDataItem as deleted in the repository (soft delete) without removing its data.
/// </summary>
/// <param name="item">The ApiDataItem to soft-delete; the item is expected to be marked as deleted prior to calling this method.</param>
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
        /// <summary>
/// Permanently removes ApiDataItem records that were soft-deleted prior to the specified cutoff date.
/// </summary>
/// <param name="olderThan">Remove items soft-deleted before this date and time (UTC is recommended).</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>The number of items that were permanently removed.</returns>
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
        /// <summary>
/// Marks all ApiDataItem entities that originate from the specified source URL as stale.
/// </summary>
/// <param name="sourceUrl">The source URL whose items should be marked stale.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>The number of items that were marked as stale.</returns>
        Task<int> MarkSourceAsStaleAsync(string sourceUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about API data items for monitoring.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics about data items.</returns>
        /// <remarks>
        /// Provides insights for cache performance and data freshness monitoring.
        /// Returns counts by status, average age, etc.
        /// <summary>
/// Retrieves aggregated statistics for ApiDataItem entities such as counts by status, age metrics, and other summary telemetry.
/// </summary>
/// <returns>An <see cref="ApiDataStatisticsDto"/> containing counts, averages, and other summary metrics for ApiDataItem entities.</returns>
        Task<ApiDataStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of entities affected.</returns>
        /// <remarks>
        /// Commits the unit of work transaction.
        /// Should be called after Add/Update/Delete operations.
        /// <summary>
/// Persists all pending changes tracked by the repository to the underlying data store.
/// </summary>
/// <returns>The number of state entries written to the underlying data store.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}