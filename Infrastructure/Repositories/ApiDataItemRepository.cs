using Microsoft.EntityFrameworkCore;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces;
using CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs;
using CleanArchitecture.ApiTemplate.Core.Domain.Entities;
using CleanArchitecture.ApiTemplate.Core.Domain.Enums;
using CleanArchitecture.ApiTemplate.Infrastructure.Data;

namespace CleanArchitecture.ApiTemplate.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework Core implementation of IApiDataItemRepository.
    /// Provides data access for ApiDataItem entities using EF Core.
    /// </summary>
    /// <remarks>
    /// This repository implementation follows best practices:
    /// - Async/await for all database operations (scalability)
    /// - IQueryable composition for flexible querying
    /// - Proper use of AsNoTracking for read-only queries (performance)
    /// - Comprehensive error handling
    /// - Optimized queries with proper indexing
    /// - Batch operations for efficiency
    /// 
    /// Clean Architecture Benefits:
    /// - Infrastructure layer implementation (EF Core details)
    /// - Application layer interface (business operations)
    /// - Domain layer entities (business rules)
    /// - Easy to swap implementations (e.g., Dapper, MongoDB)
    /// - Testable via interface mocking
    /// 
    /// Performance Considerations:
    /// - Uses AsNoTracking for read-only queries
    /// - Batch operations minimize database round-trips
    /// - Proper indexing in ApiDataItemConfiguration
    /// - Query optimization via IQueryable composition
    /// </remarks>
    public class ApiDataItemRepository : IApiDataItemRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the ApiDataItemRepository.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of <see cref="ApiDataItemRepository"/> using the provided EF Core <see cref="ApplicationDbContext"/>.
        /// </summary>
        /// <param name="context">The application's EF Core database context used for data access; must not be null.</param>
        public ApiDataItemRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<ApiDataItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ApiDataItems
                .AsNoTracking() // Read-only, better performance
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ApiDataItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return null;

            // Uses IX_ApiDataItems_ExternalId unique index for fast lookup
            return await _context.ApiDataItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ApiDataItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default)
        {
            // Uses IX_ApiDataItems_Status index
            return await _context.ApiDataItems
                .AsNoTracking()
                .Where(x => x.Status == DataStatus.Active)
                .OrderByDescending(x => x.LastSyncedAt) // Most recent first
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all ApiDataItem entities, including soft-deleted items, ordered by creation time descending.
        /// </summary>
        /// <returns>A read-only list of all ApiDataItem entities ordered by CreatedAt descending.</returns>
        public async Task<IReadOnlyList<ApiDataItem>> GetAllItemsAsync(CancellationToken cancellationToken = default)
        {
            // Note: This ignores the global query filter to include deleted items
            return await _context.ApiDataItems
                .AsNoTracking()
                .IgnoreQueryFilters() // Include soft-deleted items
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ApiDataItem>> GetItemsByStatusAsync(DataStatus status, CancellationToken cancellationToken = default)
        {
            // Uses IX_ApiDataItems_Status index
            return await _context.ApiDataItems
                .AsNoTracking()
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.LastSyncedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves items whose LastSyncedAt is older than the specified maximum age, prioritizing the oldest first.
        /// </summary>
        /// <param name="maxAge">Maximum allowed age since an item's LastSyncedAt; items older than this value are included.</param>
        /// <returns>A read-only list of ApiDataItem instances with Status not equal to Deleted and LastSyncedAt earlier than (UtcNow - maxAge), ordered from oldest to newest LastSyncedAt.</returns>
        public async Task<IReadOnlyList<ApiDataItem>> GetItemsNeedingRefreshAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow - maxAge;

            // Uses IX_ApiDataItems_LastSyncedAt_Status composite index
            // Efficient query for background refresh jobs
            return await _context.ApiDataItems
                .AsNoTracking()
                .Where(x => x.Status != DataStatus.Deleted && x.LastSyncedAt < cutoffDate)
                .OrderBy(x => x.LastSyncedAt) // Oldest first (highest priority for refresh)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ApiDataItem>> GetItemsBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return Array.Empty<ApiDataItem>();

            // Uses IX_ApiDataItems_SourceUrl index
            return await _context.ApiDataItems
                .AsNoTracking()
                .Where(x => x.SourceUrl == sourceUrl)
                .OrderByDescending(x => x.LastSyncedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ApiDataItem>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Array.Empty<ApiDataItem>();

            // Case-insensitive partial match using EF.Functions.Like
            // Uses IX_ApiDataItems_Name_Status composite index
            var searchPattern = $"%{searchTerm}%";

            return await _context.ApiDataItems
                .AsNoTracking()
                .Where(x => EF.Functions.Like(x.Name, searchPattern))
                .Where(x => x.Status == DataStatus.Active) // Only active items in search
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves ApiDataItem entities that contain the specified metadata key, optionally filtered to those whose metadata value equals the provided value.
        /// </summary>
        /// <param name="metadataKey">The metadata key to match. If null or whitespace, an empty list is returned.</param>
        /// <param name="metadataValue">Optional metadata value to match; when null, any item that has the key is included.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
        /// <returns>A list of items that have the specified metadata key and, if <paramref name="metadataValue"/> is provided, whose metadata value equals it.</returns>
        /// <remarks>
        /// Filtering is performed in memory after loading all items from the database; this may be inefficient for large datasets.
        /// </remarks>
        public async Task<IReadOnlyList<ApiDataItem>> GetItemsByMetadataAsync(
            string metadataKey,
            object? metadataValue = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(metadataKey))
                return Array.Empty<ApiDataItem>();

            // Load all items and filter in memory (JSON querying is database-specific)
            // For production with large datasets, consider using JSON functions:
            // - SQL Server: JSON_VALUE, OPENJSON
            // - PostgreSQL: jsonb operators
            var allItems = await _context.ApiDataItems
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return allItems
                .Where(x => x.HasMetadata(metadataKey))
                .Where(x => metadataValue == null || 
                           (x.GetMetadata<object>(metadataKey)?.Equals(metadataValue) ?? false))
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return false;

            // Uses IX_ApiDataItems_ExternalId unique index
            // AnyAsync is more efficient than FirstOrDefaultAsync for existence checks
            return await _context.ApiDataItems
                .AsNoTracking()
                .AnyAsync(x => x.ExternalId == externalId && x.Status != DataStatus.Deleted, cancellationToken);
        }

        /// <summary>
        /// Registers an <see cref="ApiDataItem"/> with the repository for insertion on the next unit-of-work save.
        /// </summary>
        /// <param name="item">The <see cref="ApiDataItem"/> to add; must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
        public async Task AddAsync(ApiDataItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            await _context.ApiDataItems.AddAsync(item, cancellationToken);
            // Note: SaveChangesAsync is called separately for unit of work pattern
        }

        /// <inheritdoc/>
        public async Task AddRangeAsync(IEnumerable<ApiDataItem> items, CancellationToken cancellationToken = default)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var itemList = items.ToList();
            if (itemList.Count == 0)
                return;

            // Bulk insert - more efficient than adding one by one
            await _context.ApiDataItems.AddRangeAsync(itemList, cancellationToken);
            // Note: SaveChangesAsync is called separately for unit of work pattern
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(ApiDataItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Mark entity as modified
            _context.ApiDataItems.Update(item);
            // Note: SaveChangesAsync is called separately for unit of work pattern
            await Task.CompletedTask; // Maintain async signature
        }

        /// <summary>
        /// Updates a collection of ApiDataItem entities in the context and persists the changes.
        /// </summary>
        /// <param name="items">The items to update; must not be null or empty.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
        public async Task<int> UpdateRangeAsync(IEnumerable<ApiDataItem> items, CancellationToken cancellationToken = default)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var itemList = items.ToList();
            if (itemList.Count == 0)
                return 0;

            // Bulk update - more efficient than updating one by one
            _context.ApiDataItems.UpdateRange(itemList);
            return await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Marks the given ApiDataItem as deleted in the DbContext without persisting the change.
        /// </summary>
        /// <param name="item">The ApiDataItem to mark as deleted; expected to have been marked deleted prior to calling.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
        /// <remarks>Does not call SaveChangesAsync — the caller is responsible for persisting the change.</remarks>
        public async Task DeleteAsync(ApiDataItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Soft delete - item should already be marked as deleted via MarkAsDeleted()
            _context.ApiDataItems.Update(item);
            // Note: SaveChangesAsync is called separately for unit of work pattern
            await Task.CompletedTask; // Maintain async signature
        }

        /// <summary>
        /// Permanently removes items that were soft-deleted before the specified cutoff date.
        /// </summary>
        /// <param name="olderThan">Items with a non-null <c>DeletedAt</c> earlier than this UTC timestamp will be hard-deleted.</param>
        /// <returns>The number of items that were removed from the database.</returns>
        public async Task<int> PermanentlyDeleteOldItemsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            // Find items to permanently delete
            var itemsToDelete = await _context.ApiDataItems
                .IgnoreQueryFilters() // Include soft-deleted items
                .Where(x => x.Status == DataStatus.Deleted && x.DeletedAt.HasValue && x.DeletedAt.Value < olderThan)
                .ToListAsync(cancellationToken);

            if (itemsToDelete.Count == 0)
                return 0;

            // Hard delete - permanently removes from database
            _context.ApiDataItems.RemoveRange(itemsToDelete);
            return await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Marks all active ApiDataItem entities that share the given source URL as stale.
        /// </summary>
        /// <param name="sourceUrl">The source URL whose active items should be marked stale; if null or whitespace no items are modified.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of entries persisted to the database (0 if no items were modified).</returns>
        public async Task<int> MarkSourceAsStaleAsync(string sourceUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return 0;

            // Uses IX_ApiDataItems_SourceUrl index
            var itemsToMarkStale = await _context.ApiDataItems
                .Where(x => x.SourceUrl == sourceUrl && x.Status == DataStatus.Active)
                .ToListAsync(cancellationToken);

            if (itemsToMarkStale.Count == 0)
                return 0;

            // Mark all items as stale
            foreach (var item in itemsToMarkStale)
            {
                item.MarkAsStale();
            }

            return await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Aggregates diagnostic statistics for ApiDataItem entities.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the database queries.</param>
        /// <returns>
        /// An ApiDataStatisticsDto containing:
        /// - total counts of active, stale, and deleted items (deleted count ignores global query filters),
        /// - average, oldest, and newest ages computed from LastSyncedAt,
        /// - count of distinct source URLs,
        /// - counts of items synced or marked stale in the last 24 hours,
        /// - the UTC timestamp when the statistics were calculated.
        /// </returns>
        public async Task<ApiDataStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);

            // Parallel execution of independent queries for better performance
            var activeCountTask = _context.ApiDataItems
                .AsNoTracking()
                .CountAsync(x => x.Status == DataStatus.Active, cancellationToken);

            var staleCountTask = _context.ApiDataItems
                .AsNoTracking()
                .CountAsync(x => x.Status == DataStatus.Stale, cancellationToken);

            var deletedCountTask = _context.ApiDataItems
                .AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(x => x.Status == DataStatus.Deleted, cancellationToken);

            var uniqueSourceCountTask = _context.ApiDataItems
                .AsNoTracking()
                .Select(x => x.SourceUrl)
                .Distinct()
                .CountAsync(cancellationToken);

            var syncedLast24HoursTask = _context.ApiDataItems
                .AsNoTracking()
                .CountAsync(x => x.LastSyncedAt >= last24Hours, cancellationToken);

            var staleLast24HoursTask = _context.ApiDataItems
                .AsNoTracking()
                .CountAsync(x => x.Status == DataStatus.Stale && x.UpdatedAt >= last24Hours, cancellationToken);

            // Wait for all queries to complete
            await Task.WhenAll(
                activeCountTask,
                staleCountTask,
                deletedCountTask,
                uniqueSourceCountTask,
                syncedLast24HoursTask,
                staleLast24HoursTask);

            // Calculate age statistics
            var allItems = await _context.ApiDataItems
                .AsNoTracking()
                .Select(x => x.LastSyncedAt)
                .ToListAsync(cancellationToken);

            var ages = allItems.Select(x => now - x).ToList();

            return new ApiDataStatisticsDto
            {
                TotalActiveItems = await activeCountTask,
                TotalStaleItems = await staleCountTask,
                TotalDeletedItems = await deletedCountTask,
                AverageAge = ages.Any() ? TimeSpan.FromSeconds(ages.Average(x => x.TotalSeconds)) : TimeSpan.Zero,
                OldestItemAge = ages.Any() ? ages.Max() : TimeSpan.Zero,
                NewestItemAge = ages.Any() ? ages.Min() : TimeSpan.Zero,
                UniqueSourceCount = await uniqueSourceCountTask,
                ItemsSyncedLast24Hours = await syncedLast24HoursTask,
                ItemsMarkedStaleLast24Hours = await staleLast24HoursTask,
                CalculatedAt = now
            };
        }

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Saves all pending changes to the database
            // Audit timestamps (CreatedAt, UpdatedAt) are automatically handled by DbContext
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}