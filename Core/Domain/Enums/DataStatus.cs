namespace CleanArchitecture.ApiTemplate.Core.Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle status of data items synchronized from external APIs.
    /// </summary>
    /// <remarks>
    /// Data status helps track the freshness and validity of cached or synchronized data
    /// from external sources, enabling efficient data management and refresh strategies.
    /// 
    /// Status Lifecycle:
    /// ```
    ///     [External API Fetch]
    ///            ?
    ///      ????????????
    ///      ?  Active  ? ??????? Recent & Valid ??????
    ///      ????????????                             ?
    ///            ?                                  ?
    ///            ? Time passes                      ?
    ///            ? (exceeds refresh interval)       ?
    ///            ?                                  ?
    ///      ????????????                             ?
    ///      ?  Stale   ? ??????? Needs Refresh ??????
    ///      ????????????                             ?
    ///            ?                                  ?
    ///            ? User/Admin delete                ?
    ///            ? Source removed                   ?
    ///            ?                                  ?
    ///      ????????????                             ?
    ///      ? Deleted  ?                             ?
    ///      ????????????                             ?
    ///            ?                                  ?
    ///            ? Re-fetch from API ????????????????
    ///            ?
    ///      [Back to Active]
    /// ```
    /// 
    /// Use Cases:
    /// - Cache management for external API data
    /// - Data freshness indicators
    /// - Automatic refresh triggers
    /// - User interface status display
    /// - Background sync job prioritization
    /// 
    /// Integration with Caching:
    /// ```csharp
    /// public async Task<ApiDataItem> GetDataAsync(string id)
    /// {
    ///     var cached = await _cache.GetAsync<ApiDataItem>(id);
    ///     
    ///     if (cached != null && cached.Status == DataStatus.Active)
    ///         return cached; // Use cached data
    ///         
    ///     if (cached?.Status == DataStatus.Stale)
    ///         _ = RefreshDataInBackground(id); // Async refresh
    ///         
    ///     return cached ?? await FetchFromApiAsync(id);
    /// }
    /// ```
    /// </remarks>
    public enum DataStatus
    {
        /// <summary>
        /// Data is current, valid, and recently synchronized from the source.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Recently fetched from external API
        /// - Within acceptable freshness window
        /// - Can be served directly from cache
        /// - No refresh needed
        /// 
        /// Typical Freshness Windows:
        /// - Real-time data: 1-5 minutes
        /// - Frequent updates: 15-30 minutes
        /// - Moderate updates: 1-4 hours
        /// - Infrequent updates: 1-24 hours
        /// - Static data: 1-7 days
        /// 
        /// Implementation Example:
        /// ```csharp
        /// public class ApiDataItem : BaseEntity
        /// {
        ///     public DateTime LastSyncedAt { get; private set; }
        ///     public DataStatus Status { get; private set; }
        ///     
        ///     public void MarkAsActive()
        ///     {
        ///         Status = DataStatus.Active;
        ///         LastSyncedAt = DateTime.UtcNow;
        ///         UpdatedAt = DateTime.UtcNow;
        ///     }
        ///     
        ///     public bool NeedsRefresh(TimeSpan maxAge)
        ///     {
        ///         if (Status == DataStatus.Deleted)
        ///             return false;
        ///             
        ///         return DateTime.UtcNow - LastSyncedAt > maxAge;
        ///     }
        /// }
        /// ```
        /// 
        /// Cache Strategy:
        /// - Serve directly without API call
        /// - Lower latency for users
        /// - Reduced API costs
        /// - Better offline capability
        /// 
        /// Monitoring:
        /// - Track cache hit rates
        /// - Monitor freshness distribution
        /// - Alert on sync failures
        /// - Measure API cost savings
        /// </remarks>
        Active = 1,

        /// <summary>
        /// Data is outdated and should be refreshed from the source.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Exceeded freshness window
        /// - Still usable but potentially outdated
        /// - Should trigger background refresh
        /// - Can be served while refreshing
        /// 
        /// Stale-While-Revalidate Pattern:
        /// ```csharp
        /// public async Task<Result<ApiDataItem>> GetDataAsync(string id)
        /// {
        ///     // Get from cache
        ///     var cached = await _cache.GetAsync<ApiDataItem>(id);
        ///     
        ///     if (cached == null)
        ///     {
        ///         // Cache miss - fetch from API
        ///         return await FetchAndCacheAsync(id);
        ///     }
        ///     
        ///     if (cached.Status == DataStatus.Stale)
        ///     {
        ///         // Trigger background refresh
        ///         _ = Task.Run(async () =>
        ///         {
        ///             try
        ///             {
        ///                 await RefreshDataAsync(id);
        ///                 _logger.LogInformation(
        ///                     "Background refresh completed for {Id}", id);
        ///             }
        ///             catch (Exception ex)
        ///             {
        ///                 _logger.LogError(ex,
        ///                     "Background refresh failed for {Id}", id);
        ///             }
        ///         });
        ///         
        ///         // Return stale data immediately
        ///         return Result<ApiDataItem>.Ok(cached);
        ///     }
        ///     
        ///     // Data is active
        ///     return Result<ApiDataItem>.Ok(cached);
        /// }
        /// ```
        /// 
        /// Background Refresh Strategy:
        /// 
        /// **Priority Queue:**
        /// ```csharp
        /// public class RefreshPriorityQueue
        /// {
        ///     public void EnqueueRefresh(string id, int priority)
        ///     {
        ///         var staleness = GetStaleness(id);
        ///         var accessFrequency = GetAccessFrequency(id);
        ///         
        ///         // Higher priority = more stale + more frequently accessed
        ///         priority = staleness * accessFrequency;
        ///         
        ///         _queue.Enqueue(id, priority);
        ///     }
        /// }
        /// ```
        /// 
        /// **Batch Refresh:**
        /// ```csharp
        /// public async Task RefreshStaleDataBatchAsync()
        /// {
        ///     var staleItems = await _repository
        ///         .GetStaleItemsAsync(batchSize: 50);
        ///     
        ///     var refreshTasks = staleItems
        ///         .Select(item => RefreshItemAsync(item.Id))
        ///         .ToArray();
        ///     
        ///     await Task.WhenAll(refreshTasks);
        ///     
        ///     _logger.LogInformation(
        ///         "Refreshed {Count} stale items", staleItems.Count);
        /// }
        /// ```
        /// 
        /// **Scheduled Refresh:**
        /// ```csharp
        /// // Hosted service for periodic refresh
        /// public class DataRefreshService : BackgroundService
        /// {
        ///     protected override async Task ExecuteAsync(
        ///         CancellationToken stoppingToken)
        ///     {
        ///         while (!stoppingToken.IsCancellationRequested)
        ///         {
        ///             await RefreshStaleDataAsync();
        ///             await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        ///         }
        ///     }
        /// }
        /// ```
        /// 
        /// User Experience:
        /// - Show "Updating..." indicator
        /// - Display last updated timestamp
        /// - Allow manual refresh option
        /// - Progressive data loading
        /// 
        /// Example UI:
        /// ```html
        /// <div class="data-card">
        ///     <div class="data-content">
        ///         {data}
        ///     </div>
        ///     <div class="data-status stale">
        ///         <i class="icon-refresh"></i>
        ///         Last updated: 2 hours ago (updating...)
        ///     </div>
        /// </div>
        /// ```
        /// 
        /// Metrics to Track:
        /// - Number of stale items
        /// - Average staleness duration
        /// - Refresh success rate
        /// - Background refresh latency
        /// - User-triggered refresh count
        /// </remarks>
        Stale = 2,

        /// <summary>
        /// Data has been marked as deleted and should not be displayed or used.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Soft-deleted from system
        /// - Source removed from external API
        /// - Should not appear in queries
        /// - Retained for audit/compliance
        /// 
        /// Deletion Triggers:
        /// 
        /// **User-Initiated:**
        /// - User removes item from their view
        /// - User unsubscribes from data source
        /// - User's preference to hide item
        /// 
        /// **System-Initiated:**
        /// - External API returns 404 (not found)
        /// - Source data deprecated
        /// - Data quality issues detected
        /// - Compliance/legal removal
        /// 
        /// **Admin-Initiated:**
        /// - Manual data cleanup
        /// - Policy violation
        /// - Data quality issues
        /// - Duplicate removal
        /// 
        /// Implementation Pattern:
        /// ```csharp
        /// public class ApiDataItem : BaseEntity
        /// {
        ///     public void MarkAsDeleted(string reason)
        ///     {
        ///         if (Status == DataStatus.Deleted)
        ///             throw new InvalidDomainOperationException(
        ///                 "Delete data",
        ///                 "Data is already deleted");
        ///         
        ///         Status = DataStatus.Deleted;
        ///         SoftDelete(); // Inherited from BaseEntity
        ///         
        ///         _logger.LogInformation(
        ///             "Data item {Id} marked as deleted. Reason: {Reason}",
        ///             Id, reason);
        ///     }
        ///     
        ///     public void Restore()
        ///     {
        ///         if (Status != DataStatus.Deleted)
        ///             throw new InvalidDomainOperationException(
        ///                 "Restore data",
        ///                 "Only deleted data can be restored");
        ///         
        ///         Status = DataStatus.Stale; // Needs refresh
        ///         base.Restore(); // Inherited from BaseEntity
        ///     }
        /// }
        /// ```
        /// 
        /// Query Filter:
        /// ```csharp
        /// // Automatically exclude deleted data
        /// modelBuilder.Entity<ApiDataItem>()
        ///     .HasQueryFilter(e =>
        ///         e.Status != DataStatus.Deleted &&
        ///         !e.IsDeleted);
        /// 
        /// // Explicitly include deleted (for admin views)
        /// var allItems = await _context.ApiDataItems
        ///     .IgnoreQueryFilters()
        ///     .Where(x => x.ExternalId == id)
        ///     .ToListAsync();
        /// ```
        /// 
        /// Cleanup Strategy:
        /// ```csharp
        /// public async Task CleanupDeletedDataAsync()
        /// {
        ///     var retentionPeriod = TimeSpan.FromDays(90);
        ///     var cutoffDate = DateTime.UtcNow - retentionPeriod;
        ///     
        ///     // Hard delete after retention period
        ///     var itemsToDelete = await _context.ApiDataItems
        ///         .IgnoreQueryFilters()
        ///         .Where(x =>
        ///             x.Status == DataStatus.Deleted &&
        ///             x.DeletedAt < cutoffDate)
        ///         .ToListAsync();
        ///     
        ///     _context.ApiDataItems.RemoveRange(itemsToDelete);
        ///     await _context.SaveChangesAsync();
        ///     
        ///     _logger.LogInformation(
        ///         "Permanently deleted {Count} old items", itemsToDelete.Count);
        /// }
        /// ```
        /// 
        /// External API Handling:
        /// ```csharp
        /// public async Task<Result<ApiDataItem>> SyncFromApiAsync(string id)
        /// {
        ///     try
        ///     {
        ///         var apiData = await _apiClient.GetAsync(id);
        ///         
        ///         // Update or create item
        ///         var item = await GetOrCreateAsync(id);
        ///         item.UpdateFromApi(apiData);
        ///         item.MarkAsActive();
        ///         
        ///         await _repository.SaveAsync();
        ///         return Result<ApiDataItem>.Ok(item);
        ///     }
        ///     catch (HttpNotFoundException)
        ///     {
        ///         // Source no longer exists
        ///         var item = await GetAsync(id);
        ///         item?.MarkAsDeleted("Source removed from API");
        ///         await _repository.SaveAsync();
        ///         
        ///         return Result<ApiDataItem>.Fail("Data no longer available");
        ///     }
        /// }
        /// ```
        /// 
        /// Audit Trail:
        /// - Log all deletion events
        /// - Track deletion reasons
        /// - Monitor deletion patterns
        /// - Alert on unusual deletions
        /// 
        /// Compliance Considerations:
        /// - Retention policies (GDPR, SOX, HIPAA)
        /// - Right to erasure (GDPR Article 17)
        /// - Data portability requirements
        /// - Audit trail requirements
        /// </remarks>
        Deleted = 3
    }
}
