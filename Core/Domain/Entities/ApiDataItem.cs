using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.Enums;
using SecureCleanApiWaf.Core.Domain.Exceptions;

namespace SecureCleanApiWaf.Core.Domain.Entities
{
    /// <summary>
    /// ApiDataItem aggregate root.
    /// Represents data synchronized from external APIs with caching and freshness management.
    /// 
    /// This is a aggregate root entity in the domain-driven design (DDD) sense that:
    /// - It encapsulates all related data and behavior for managing API-sourced data.
    /// - It enforces invariants and business rules around data freshness, staleness, and lifecycle.
    /// - It serves as the main entry point for operations on API data within the domain.
    /// - It ensures consistency and integrity of the data it manages.
    /// - Rises domain events when significant state changes occur (e.g., data marked as stale or deleted).
    /// 
    /// Invariants:
    /// - Data must have a valid external ID and source URL.
    /// - Data freshness is tracked via LastSyncedAt timestamp.
    /// - Data status must be one of the defined enum values (Active, Stale, Deleted).
    /// - Updates to data must validate input and enforce business rules.
    /// - Deletion is a soft delete, preserving data for audit and potential restoration.
    /// - Other invariants may be defined based on specific domain requirements and business rules here.
    /// 
    /// 
    /// </summary>
    /// <remarks>
    /// This entity manages data retrieved from third-party APIs, tracking freshness,
    /// handling cache invalidation, and maintaining data quality.
    /// 
    /// Key Responsibilities:
    /// - External data synchronization tracking
    /// - Cache freshness management
    /// - Data staleness detection
    /// - Metadata management
    /// - Source attribution
    /// 
    /// Usage Example:
    /// ```csharp
    /// // Create from external API response
    /// var item = ApiDataItem.CreateFromExternalSource(
    ///     externalId: "12345",
    ///     name: "Sample Product",
    ///     description: "Product description",
    ///     sourceUrl: "https://api.example.com/products/12345"
    /// );
    /// 
    /// // Check if refresh needed
    /// if (item.NeedsRefresh(TimeSpan.FromHours(1)))
    /// {
    ///     await RefreshFromApiAsync(item);
    /// }
    /// 
    /// // Update from fresh API call
    /// item.UpdateFromExternalSource("Updated Name", "Updated Description");
    /// 
    /// // Manage lifecycle
    /// item.MarkAsStale();
    /// item.MarkAsActive();
    /// ```
    /// </remarks>
    // Clear Indicator this is an aggregate root
    public class ApiDataItem : BaseEntity, IAggregateRoot // Inherits from BaseEntity for common properties, and implements IAggregateRoot to signify aggregate root status
    {
        private readonly Dictionary<string, object> _metadata = new();

        /// <summary>
        /// Gets the ID from the external system.
        /// </summary>
        /// <remarks>
        /// Original identifier from the third-party API.
        /// Useful for correlation and updates.
        /// </remarks>
        public string ExternalId { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the item name/title.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the item description.
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the source URL where this data came from.
        /// </summary>
        /// <remarks>
        /// Full API endpoint URL for reference and re-fetching.
        /// Example: "https://api.example.com/products/12345"
        /// </remarks>
        public string SourceUrl { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the last time this data was synchronized from the source.
        /// </summary>
        public DateTime LastSyncedAt { get; private set; }

        /// <summary>
        /// Gets the current data status (Active, Stale, Deleted).
        /// </summary>
        public DataStatus Status { get; private set; }

        /// <summary>
        /// Gets additional metadata as key-value pairs.
        /// </summary>
        /// <remarks>
        /// Flexible storage for API-specific data.
        /// Examples: category, price, rating, custom fields.
        /// </remarks>
        public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();

        /// <summary>
        /// Private constructor for EF Core. Prevents direct instantiation. It is used by the static factory method called CreateFromExternalSource to create new instances.
        /// </summary>
        private ApiDataItem() { }

        /// <summary>
        /// Creates a new API data item from an external source.
        /// </summary>
        /// <param name="externalId">The ID from the external system.</param>
        /// <param name="name">The item name/title.</param>
        /// <param name="description">The item description.</param>
        /// <param name="sourceUrl">The API endpoint URL.</param>
        /// <returns>A new ApiDataItem entity.</returns>
        /// <exception cref="DomainException">Thrown when validation fails.</exception>
        public static ApiDataItem CreateFromExternalSource(
            string externalId,
            string name,
            string description,
            string sourceUrl)
        {
            // Validation: External ID
            if (string.IsNullOrWhiteSpace(externalId))
                throw new DomainException("External ID cannot be empty");

            // Validation: Name
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Name cannot be empty");

            if (name.Length > 500)
                throw new DomainException("Name cannot exceed 500 characters");

            // Validation: Description (optional but has max length)
            if (!string.IsNullOrWhiteSpace(description) && description.Length > 2000)
                throw new DomainException("Description cannot exceed 2000 characters");

            // Validation: Source URL
            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new DomainException("Source URL cannot be empty");

            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
                throw new DomainException("Source URL must be a valid absolute URL");

            return new ApiDataItem
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = name,
                Description = description ?? string.Empty,
                SourceUrl = sourceUrl,
                LastSyncedAt = DateTime.UtcNow,
                Status = DataStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Updates the item with fresh data from external source.
        /// </summary>
        /// <param name="name">Updated name.</param>
        /// <param name="description">Updated description.</param>
        /// <exception cref="DomainException">Thrown when validation fails.</exception>
        public void UpdateFromExternalSource(string name, string description)
        {
            // Validation: Name
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Name cannot be empty");

            if (name.Length > 500)
                throw new DomainException("Name cannot exceed 500 characters");

            // Validation: Description
            if (!string.IsNullOrWhiteSpace(description) && description.Length > 2000)
                throw new DomainException("Description cannot exceed 2000 characters");

            // Business Rule: Cannot update deleted data
            if (Status == DataStatus.Deleted)
            {
                throw new InvalidDomainOperationException(
                    "Update data",
                    "Cannot update deleted data items");
            }

            Name = name;
            Description = description ?? string.Empty;
            LastSyncedAt = DateTime.UtcNow;
            Status = DataStatus.Active; // Fresh data is active
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the data as stale (needs refresh).
        /// </summary>
        /// <remarks>
        /// Called when data exceeds freshness threshold.
        /// Stale data can still be served while refresh happens in background.
        /// </remarks>
        public void MarkAsStale()
        {
            if (Status == DataStatus.Deleted)
                return; // Keep deleted status

            if (Status == DataStatus.Stale)
                return; // Already stale

            Status = DataStatus.Stale;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the data as active (fresh and valid).
        /// </summary>
        public void MarkAsActive()
        {
            if (Status == DataStatus.Deleted)
            {
                throw new InvalidDomainOperationException(
                    "Mark as active",
                    "Cannot reactivate deleted data. Use Restore() first.");
            }

            Status = DataStatus.Active;
            LastSyncedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the data as deleted (soft delete).
        /// </summary>
        /// <param name="reason">Reason for deletion (for audit).</param>
        public void MarkAsDeleted(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Deletion reason is required");

            if (Status == DataStatus.Deleted)
                return; // Already deleted

            Status = DataStatus.Deleted;
            SoftDelete(); // Inherited from BaseEntity
            UpdatedAt = DateTime.UtcNow;

            // Could store reason in metadata
            AddMetadata("deletion_reason", reason);
            AddMetadata("deleted_by", "system"); // Or actual user if available
        }

        /// <summary>
        /// Restores previously deleted data.
        /// </summary>
        /// <remarks>
        /// Restored data is marked as stale and should be refreshed.
        /// </remarks>
        public new void Restore()
        {
            if (Status != DataStatus.Deleted)
            {
                throw new InvalidDomainOperationException(
                    "Restore data",
                    "Only deleted data can be restored");
            }

            base.Restore(); // Inherited from BaseEntity
            Status = DataStatus.Stale; // Needs refresh after restore
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the data needs to be refreshed based on age.
        /// </summary>
        /// <param name="maxAge">Maximum acceptable age before refresh.</param>
        /// <returns>True if refresh is needed, false otherwise.</returns>
        /// <remarks>
        /// Implements stale-while-revalidate pattern.
        /// 
        /// Example:
        /// ```csharp
        /// // Check if data is older than 1 hour
        /// if (item.NeedsRefresh(TimeSpan.FromHours(1)))
        /// {
        ///     // Trigger background refresh
        ///     await RefreshDataAsync(item);
        /// }
        /// ```
        /// </remarks>
        public bool NeedsRefresh(TimeSpan maxAge)
        {
            if (Status == DataStatus.Deleted)
                return false; // Deleted data doesn't need refresh

            var age = DateTime.UtcNow - LastSyncedAt;
            return age > maxAge;
        }

        /// <summary>
        /// Gets the age of the data (time since last sync).
        /// </summary>
        /// <returns>TimeSpan representing data age.</returns>
        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - LastSyncedAt;
        }

        /// <summary>
        /// Checks if the data is fresh based on a threshold.
        /// </summary>
        /// <param name="freshnessThreshold">Threshold for considering data fresh.</param>
        /// <returns>True if data is fresh, false otherwise.</returns>
        public bool IsFresh(TimeSpan freshnessThreshold)
        {
            if (Status != DataStatus.Active)
                return false;

            return GetAge() <= freshnessThreshold;
        }

        /// <summary>
        /// Adds or updates a metadata entry.
        /// </summary>
        /// <param name="key">Metadata key.</param>
        /// <param name="value">Metadata value.</param>
        /// <exception cref="DomainException">Thrown when key is invalid.</exception>
        public void AddMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new DomainException("Metadata key cannot be empty");

            if (value == null)
                throw new DomainException("Metadata value cannot be null");

            _metadata[key] = value;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a metadata entry.
        /// </summary>
        /// <param name="key">Metadata key to remove.</param>
        /// <returns>True if removed, false if key didn't exist.</returns>
        public bool RemoveMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var removed = _metadata.Remove(key);
            
            if (removed)
            {
                UpdatedAt = DateTime.UtcNow;
            }

            return removed;
        }

        /// <summary>
        /// Gets a metadata value by key.
        /// </summary>
        /// <typeparam name="T">Expected type of the value.</typeparam>
        /// <param name="key">Metadata key.</param>
        /// <returns>The metadata value, or default(T) if not found.</returns>
        public T? GetMetadata<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            if (!_metadata.TryGetValue(key, out var value))
                return default;

            try
            {
                return (T)value;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Checks if metadata contains a specific key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if key exists, false otherwise.</returns>
        public bool HasMetadata(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && _metadata.ContainsKey(key);
        }

        /// <summary>
        /// Clears all metadata.
        /// </summary>
        public void ClearMetadata()
        {
            _metadata.Clear();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the source URL (if API endpoint changes).
        /// </summary>
        /// <param name="newSourceUrl">The new source URL.</param>
        /// <exception cref="DomainException">Thrown when URL is invalid.</exception>
        public void UpdateSourceUrl(string newSourceUrl)
        {
            if (string.IsNullOrWhiteSpace(newSourceUrl))
                throw new DomainException("Source URL cannot be empty");

            if (!Uri.TryCreate(newSourceUrl, UriKind.Absolute, out var uri))
                throw new DomainException("Source URL must be a valid absolute URL");

            SourceUrl = newSourceUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a summary of the data item for logging/debugging.
        /// </summary>
        /// <returns>Data item summary string.</returns>
        public string GetSummary()
        {
            return $"ID: {Id}, " +
                   $"ExternalID: {ExternalId}, " +
                   $"Name: {Name}, " +
                   $"Status: {Status}, " +
                   $"LastSync: {LastSyncedAt:yyyy-MM-dd HH:mm:ss}, " +
                   $"Age: {GetAge().TotalMinutes:F1} minutes, " +
                   $"Metadata: {Metadata.Count} items";
        }
    }
}
