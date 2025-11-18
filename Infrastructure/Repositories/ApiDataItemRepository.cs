using Microsoft.EntityFrameworkCore;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.Enums;
using SecureCleanApiWaf.Infrastructure.Data;

namespace SecureCleanApiWaf.Infrastructure.Repositories
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
        /// <param name="context">EF Core database context.</param>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task DeleteAsync(ApiDataItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Soft delete - item should already be marked as deleted via MarkAsDeleted()
            _context.ApiDataItems.Update(item);
            // Note: SaveChangesAsync is called separately for unit of work pattern
            await Task.CompletedTask; // Maintain async signature
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
