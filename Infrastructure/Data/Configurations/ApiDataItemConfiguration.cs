using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.ApiTemplate.Core.Domain.Entities;
using CleanArchitecture.ApiTemplate.Core.Domain.Enums;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CleanArchitecture.ApiTemplate.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework Core configuration for ApiDataItem entity.
    /// Defines database schema, constraints, indexes, and relationships.
    /// </summary>
    /// <remarks>
    /// This configuration class follows EF Core best practices:
    /// - Separates configuration from DbContext (cleaner code)
    /// - Explicit schema definition (no conventions)
    /// - Performance optimizations (indexes, column types)
    /// - Data integrity constraints (required fields, max lengths)
    /// 
    /// Key Features:
    /// - Composite indexes for common query patterns
    /// - JSON column for metadata flexibility
    /// - Optimized string lengths based on domain rules
    /// - UTC datetime handling
    /// - Soft delete support via query filter (in DbContext)
    /// 
    /// Benefits:
    /// - Better query performance (proper indexes)
    /// - Data integrity enforcement at database level
    /// - Clear documentation of schema intent
    /// - Easier migrations and database updates
    /// </remarks>
    public class ApiDataItemConfiguration : IEntityTypeConfiguration<ApiDataItem>
    {
        /// <summary>
        /// Configures the ApiDataItem entity.
        /// </summary>
        /// <summary>
        /// Configures the EF Core mapping for the ApiDataItem entity, defining its table name, column types and constraints, indexes, JSON metadata conversion, soft-delete and audit properties, and optimistic concurrency behavior.
        /// </summary>
        /// <param name="builder">EntityTypeBuilder for ApiDataItem used to configure table mapping, columns, indexes, conversions, and concurrency.</param>
        public void Configure(EntityTypeBuilder<ApiDataItem> builder)
        {
            // ===== TABLE CONFIGURATION =====
            // Specify table name explicitly (good practice for production databases)
            builder.ToTable("ApiDataItems");

            // ===== PRIMARY KEY =====
            // Define primary key (Id property)
            // EF Core auto-detects 'Id' property but explicit is clearer
            builder.HasKey(x => x.Id);

            // ===== EXTERNAL ID CONFIGURATION =====
            // ExternalId: Unique identifier from external API
            builder.Property(x => x.ExternalId)
                .IsRequired() // NOT NULL constraint
                .HasMaxLength(200); // Reasonable limit for external IDs

            // Index for fast lookups by ExternalId
            // This is a critical query pattern for sync operations
            // UNIQUE index prevents duplicate storage of same external data
            builder.HasIndex(x => x.ExternalId)
                .IsUnique()
                .HasDatabaseName("IX_ApiDataItems_ExternalId");

            // ===== NAME CONFIGURATION =====
            // Name: Item title/name from external API
            builder.Property(x => x.Name)
                .IsRequired() // NOT NULL constraint
                .HasMaxLength(500); // Matches domain validation rule

            // Index for search functionality (case-insensitive search)
            // Composite index with Status for filtered searches
            builder.HasIndex(x => new { x.Name, x.Status })
                .HasDatabaseName("IX_ApiDataItems_Name_Status");

            // ===== DESCRIPTION CONFIGURATION =====
            // Description: Optional detailed description
            builder.Property(x => x.Description)
                .IsRequired(false) // Nullable
                .HasMaxLength(2000); // Matches domain validation rule

            // ===== SOURCE URL CONFIGURATION =====
            // SourceUrl: API endpoint where data came from
            builder.Property(x => x.SourceUrl)
                .IsRequired() // NOT NULL constraint
                .HasMaxLength(1000); // URLs can be long with query parameters

            // Index for source-based queries (refresh by source, etc.)
            builder.HasIndex(x => x.SourceUrl)
                .HasDatabaseName("IX_ApiDataItems_SourceUrl");

            // ===== LAST SYNCED AT CONFIGURATION =====
            // LastSyncedAt: Timestamp of last successful sync from external API
            builder.Property(x => x.LastSyncedAt)
                .IsRequired() // Always has a value
                .HasColumnType("datetime2"); // SQL Server datetime2 for precision

            // Composite index for freshness queries (critical for cache management)
            // Pattern: "Give me all items synced before X date with Active status"
            builder.HasIndex(x => new { x.LastSyncedAt, x.Status })
                .HasDatabaseName("IX_ApiDataItems_LastSyncedAt_Status");

            // ===== STATUS CONFIGURATION =====
            // Status: Data lifecycle status (Active, Stale, Deleted)
            builder.Property(x => x.Status)
                .IsRequired() // Always has a value
                .HasConversion<string>() // Store enum as string for readability in DB
                .HasMaxLength(50); // Enum names are short

            // Index for status-based filtering (very common query pattern)
            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_ApiDataItems_Status");

            // ===== METADATA CONFIGURATION =====
            // Metadata: Flexible JSON storage for API-specific fields
            // This allows storing additional data without schema changes
            // Examples: category, price, rating, custom fields
            builder.Property(x => x.Metadata)
                .HasColumnType("nvarchar(max)") // JSON column (SQL Server 2016+)
                .HasConversion(
                    // Convert dictionary to JSON string for database storage
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    // Convert JSON string back to dictionary when reading from database
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) 
                         ?? new Dictionary<string, object>(),
                    // Value comparer for change detection
                    new ValueComparer<IReadOnlyDictionary<string, object>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToDictionary(x => x.Key, x => x.Value)
                    )
                );

            // ===== BASE ENTITY PROPERTIES (INHERITED) =====
            // These properties come from BaseEntity but we configure them here

            // CreatedAt: Audit timestamp
            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()"); // SQL Server function for UTC time

            // UpdatedAt: Audit timestamp
            builder.Property(x => x.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("datetime2");

            // DeletedAt: Soft delete timestamp
            builder.Property(x => x.DeletedAt)
                .IsRequired(false)
                .HasColumnType("datetime2");

            // IsDeleted: Soft delete flag
            builder.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Composite index for soft delete queries
            // Pattern: "Give me all non-deleted items"
            // This is automatically used by global query filter
            builder.HasIndex(x => new { x.IsDeleted, x.Status })
                .HasDatabaseName("IX_ApiDataItems_IsDeleted_Status");

            // ===== PERFORMANCE OPTIMIZATIONS =====
            // Include commonly accessed fields in indexes for covering queries
            // This prevents database from accessing table data for common queries

            // Covering index for list queries with basic info
            builder.HasIndex(x => new { x.Status, x.LastSyncedAt })
                .IncludeProperties(x => new { x.Name, x.ExternalId })
                .HasDatabaseName("IX_ApiDataItems_Status_LastSyncedAt_Covering");

            // ===== CONCURRENCY CONFIGURATION =====
            // Row version for optimistic concurrency control
            // Prevents lost updates when multiple processes modify same data
            builder.Property<byte[]>("RowVersion")
                .IsRowVersion() // SQL Server ROWVERSION column
                .HasColumnName("RowVersion");
        }
    }
}