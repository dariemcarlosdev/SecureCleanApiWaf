using Microsoft.EntityFrameworkCore;
using SecureCleanApiWaf.Core.Domain.Entities;

namespace SecureCleanApiWaf.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context for the application.
    /// Manages database connections and entity configurations.
    /// </summary>
    /// <remarks>
    /// This DbContext follows Clean Architecture principles:
    /// - Located in Infrastructure layer (data access concern)
    /// - References Domain layer entities (ApiDataItem, User, Token)
    /// - Configured via dependency injection
    /// - Supports migrations for database schema management
    /// 
    /// Key Responsibilities:
    /// - Database connection management
    /// - Entity tracking and change detection
    /// - Query translation (LINQ to SQL)
    /// - Transaction management
    /// - Database migrations
    /// 
    /// Best Practices Implemented:
    /// - Async operations for scalability
    /// - Query filters for soft deletes
    /// - Optimized configurations for performance
    /// - Proper indexes for common queries
    /// - Audit field management (CreatedAt, UpdatedAt)
    /// 
    /// Usage Example:
    /// ```csharp
    /// public class ApiDataItemRepository : IApiDataItemRepository
    /// {
    ///     private readonly ApplicationDbContext _context;
    ///     
    ///     public async Task<ApiDataItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    ///     {
    ///         return await _context.ApiDataItems
    ///             .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    ///     }
    /// }
    /// ```
    /// </remarks>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext.
        /// </summary>
        /// <param name="options">Configuration options for the context.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the ApiDataItems entity set.
        /// </summary>
        /// <remarks>
        /// Represents the ApiDataItems table in the database.
        /// Used for querying and persisting API data item entities.
        /// </remarks>
        public DbSet<ApiDataItem> ApiDataItems { get; set; } = null!;

        /// <summary>
        /// Gets or sets the Users entity set.
        /// </summary>
        /// <remarks>
        /// Represents the Users table in the database.
        /// Used for authentication and user management.
        /// </remarks>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Gets or sets the Tokens entity set.
        /// </summary>
        /// <remarks>
        /// Represents the Tokens table in the database.
        /// Used for JWT token blacklist management.
        /// </remarks>
        public DbSet<Token> Tokens { get; set; } = null!;

        /// <summary>
        /// Configures the model relationships and constraints.
        /// </summary>
        /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
        /// <remarks>
        /// This method is called by EF Core during model creation.
        /// It applies entity configurations from separate configuration classes.
        /// 
        /// Configuration is organized by entity for maintainability:
        /// - ApiDataItemConfiguration.cs
        /// - UserConfiguration.cs (future)
        /// - TokenConfiguration.cs (future)
        /// 
        /// Benefits of separate configuration classes:
        /// - Single Responsibility Principle
        /// - Easier to maintain and test
        /// - Better organization for large models
        /// - Reusable configurations
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from the assembly
            // This automatically discovers and applies IEntityTypeConfiguration<T> implementations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global query filters for soft delete pattern
            // Automatically excludes soft-deleted entities from all queries
            // To include deleted entities, use IgnoreQueryFilters()
            modelBuilder.Entity<ApiDataItem>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<User>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<Token>()
                .HasQueryFilter(e => !e.IsDeleted);
        }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        /// <remarks>
        /// Overridden to automatically update audit fields (CreatedAt, UpdatedAt).
        /// This ensures consistent audit tracking across all entities.
        /// 
        /// Behavior:
        /// - New entities: Sets CreatedAt to current UTC time
        /// - Modified entities: Sets UpdatedAt to current UTC time
        /// - Unchanged entities: No modifications
        /// 
        /// This approach centralizes audit logic, preventing:
        /// - Inconsistent timestamp handling
        /// - Manual timestamp updates in repositories
        /// - Timezone issues (always uses UTC)
        /// </remarks>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Get all tracked entities that inherit from BaseEntity
            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // Set creation timestamp for new entities using EF Core Property access
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    // Set update timestamp for modified entities using EF Core Property access
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Synchronous version of SaveChangesAsync.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        /// <remarks>
        /// Overridden to maintain consistency with async version.
        /// However, prefer using SaveChangesAsync for better scalability.
        /// </remarks>
        public override int SaveChanges()
        {
            // Get all tracked entities that inherit from BaseEntity
            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // Set creation timestamp for new entities using EF Core Property access
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    // Set update timestamp for modified entities using EF Core Property access
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }

            return base.SaveChanges();
        }
    }
}
