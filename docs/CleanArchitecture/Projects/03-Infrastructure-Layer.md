# CleanArchitecture.ApiTemplate.Infrastructure Project

> *"The infrastructure layer provides generic technical capabilities that support the higher layers: message sending for the application, persistence for the domain, drawing widgets for the UI, and so on."*  
> � **Eric Evans**, Domain-Driven Design

---

**📚 New to Clean Architecture or DDD?**  
Read **[Architecture Patterns Explained](../ARCHITECTURE_PATTERNS_EXPLAINED.md)** first to understand how Clean Architecture and Domain-Driven Design work together in this project.

---

## 📑 Table of Contents

1. [📖 Overview](#-overview)
2. [📖 Purpose](#-purpose)
3. [📖 Project Structure](#-project-structure)
4. [📖 Key Implementations](#-key-implementations)
   - [1. Database Context (EF Core)](#1-database-context-ef-core--implemented)
   - [2. Entity Configuration (Fluent API)](#2-entity-configuration-fluent-api--implemented)
   - [3. Repository Pattern Implementation](#3-repository-pattern-implementation--implemented)
   - [4. Database Configuration Class](#4-database-configuration-class--implemented)
   - [5. API Integration Service](#5-api-integration-service--implemented)
   - [6. Dependency Injection Setup](#6-dependency-injection-setup--implemented)
5. [📖 Database Migrations](#-database-migrations)
   - [Creating Migrations](#creating-migrations)
   - [Migration Commands with Project/Startup Paths](#migration-commands-with-projectstartup-paths)
6. [📖 Dual-Service Architecture Pattern](#-dual-service-architecture-pattern)
   - [Why Two Services?](#why-two-services)
   - [Handler Usage Example](#handler-usage-example)
7. [🔧 Infrastructure Layer Checklist](#-infrastructure-layer-checklist)
8. [📖 Best Practices](#-best-practices)
   - [? DO](#-do)
   - [? DON'T](#-dont)
9. [📖 Summary](#-summary)

---

## 📖 Overview
The **Infrastructure Layer** implements all external concerns and dependencies. This layer contains concrete implementations of interfaces defined in the Application layer, including data persistence, external API integrations, file systems, email services, and more.

---

## 🎯 Purpose
- Implement Application layer interfaces
- Handle data persistence (EF Core, Dapper, etc.)
- Integrate with external APIs
- Manage caching (in-memory, Redis, SQL Server)
- Implement identity and authentication
- Handle file storage and management
- Send emails, SMS, and notifications
- Integrate with third-party services

---

## 📁 Project Structure

```
CleanArchitecture.ApiTemplate.Infrastructure/
+-- Data/                             # Database and Entity Framework Core
�   +-- ApplicationDbContext.cs      # EF Core DbContext
�   +-- DatabaseSettings.cs          # Configuration class
�   +-- Configurations/              # Entity configurations
�       +-- ApiDataItemConfiguration.cs
�
+-- Repositories/                     # Repository pattern implementations
�   +-- ApiDataItemRepository.cs     # IApiDataItemRepository implementation
�
+-- Services/                         # Service implementations
�   +-- ApiIntegrationService.cs     # IApiIntegrationService implementation
�   +-- TokenBlacklistService.cs     # ITokenBlacklistService implementation
�
+-- Caching/                          # Caching implementations
�   +-- CacheService.cs              # ICacheService implementation
�   +-- SampleCache.cs               # Legacy cache (to be migrated)
�
+-- Security/                         # Security implementations
�   +-- JwtTokenGenerator.cs         # JWT token creation
�
+-- Handlers/                         # HTTP message handlers
�   +-- ApiKeyHandler.cs             # DelegatingHandler for API keys
�
+-- Middleware/                       # Custom middleware
    +-- JwtBlacklistValidationMiddleware.cs
```

---

## 🔑 Key Implementations

### 1. **Database Context (EF Core)** ? IMPLEMENTED

Our actual `ApplicationDbContext` implementation with soft delete support and automatic audit tracking:

```csharp
/// <summary>
/// Entity Framework Core database context for the application.
/// Manages database connections and entity configurations.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the ApiDataItems entity set.
    /// </summary>
    public DbSet<ApiDataItem> ApiDataItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Users entity set.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Tokens entity set.
    /// </summary>
    public DbSet<Token> Tokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters for soft delete pattern
        modelBuilder.Entity<ApiDataItem>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<User>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<Token>()
            .HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Get all tracked entities that inherit from BaseEntity
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Set creation timestamp using EF Core Property access
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                // Set update timestamp using EF Core Property access
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

**Key Features:**
- ? Automatic audit field updates (CreatedAt, UpdatedAt)
- ? Global query filters for soft deletes
- ? Configuration discovery via assembly scanning
- ? Supports three domain entities: ApiDataItem, User, Token

---

### 2. **Entity Configuration (Fluent API)** ? IMPLEMENTED

Complete `ApiDataItemConfiguration` with optimized indexes and JSON metadata:

```csharp
/// <summary>
/// Entity Framework Core configuration for ApiDataItem entity.
/// Defines database schema, constraints, indexes, and relationships.
/// </summary>
public class ApiDataItemConfiguration : IEntityTypeConfiguration<ApiDataItem>
{
    public void Configure(EntityTypeBuilder<ApiDataItem> builder)
    {
        // Table name
        builder.ToTable("ApiDataItems");

        // Primary key
        builder.HasKey(x => x.Id);

        // ExternalId - Unique identifier from external API
        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.ExternalId)
            .IsUnique()
            .HasDatabaseName("IX_ApiDataItems_ExternalId");

        // Name - Item title/name
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.Name, x.Status })
            .HasDatabaseName("IX_ApiDataItems_Name_Status");

        // Description - Optional detailed description
        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(2000);

        // SourceUrl - API endpoint
        builder.Property(x => x.SourceUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(x => x.SourceUrl)
            .HasDatabaseName("IX_ApiDataItems_SourceUrl");

        // LastSyncedAt - Timestamp of last sync
        builder.Property(x => x.LastSyncedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.HasIndex(x => new { x.LastSyncedAt, x.Status })
            .HasDatabaseName("IX_ApiDataItems_LastSyncedAt_Status");

        // Status - Data lifecycle status
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ApiDataItems_Status");

        // Metadata - JSON storage for flexible fields
        builder.Property(x => x.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) 
                     📖 new Dictionary<string, object>(),
                new ValueComparer<IReadOnlyDictionary<string, object>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToDictionary(x => x.Key, x => x.Value)
                )
            );

        // Base Entity Properties
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetime2");

        builder.Property(x => x.DeletedAt)
            .IsRequired(false)
            .HasColumnType("datetime2");

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => new { x.IsDeleted, x.Status })
            .HasDatabaseName("IX_ApiDataItems_IsDeleted_Status");

        // Covering index for list queries
        builder.HasIndex(x => new { x.Status, x.LastSyncedAt })
            .IncludeProperties(x => new { x.Name, x.ExternalId })
            .HasDatabaseName("IX_ApiDataItems_Status_LastSyncedAt_Covering");

        // Row version for optimistic concurrency
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .HasColumnName("RowVersion");
    }
}
```

**Optimization Highlights:**
- ? **7 Strategic Indexes** for common query patterns
- ? **JSON Metadata Column** for flexible storage
- ? **Covering Indexes** to avoid table lookups
- ? **Optimistic Concurrency** with row versioning
- ? **Soft Delete Support** via IsDeleted index

---

### 3. **Repository Pattern Implementation** ? IMPLEMENTED

Complete `ApiDataItemRepository` with all 24 methods from the interface:

```csharp
/// <summary>
/// Entity Framework Core implementation of IApiDataItemRepository.
/// Provides data access for ApiDataItem entities using EF Core.
/// </summary>
public class ApiDataItemRepository : IApiDataItemRepository
{
    private readonly ApplicationDbContext _context;

    public ApiDataItemRepository(ApplicationDbContext context)
    {
        _context = context 📖 throw new ArgumentNullException(nameof(context));
    }

    // ===== QUERY METHODS =====

    public async Task<ApiDataItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiDataItems
            .AsNoTracking() // Read-only, better performance
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ApiDataItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        // Uses IX_ApiDataItems_ExternalId unique index
        return await _context.ApiDataItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiDataItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default)
    {
        // Uses IX_ApiDataItems_Status index
        return await _context.ApiDataItems
            .AsNoTracking()
            .Where(x => x.Status == DataStatus.Active)
            .OrderByDescending(x => x.LastSyncedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiDataItem>> GetItemsNeedingRefreshAsync(
        TimeSpan maxAge, 
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - maxAge;

        // Uses IX_ApiDataItems_LastSyncedAt_Status composite index

    public async Task AddAsync(ApiDataItem item, CancellationToken cancellationToken = default)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        await _context.ApiDataItems.AddAsync(item, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<ApiDataItem> items, 
        CancellationToken cancellationToken = default)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return;

        await _context.ApiDataItems.AddRangeAsync(itemList, cancellationToken);
    }

    public async Task UpdateAsync(ApiDataItem item, CancellationToken cancellationToken = default)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        _context.ApiDataItems.Update(item);
        await Task.CompletedTask;
    }

    public async Task<int> MarkSourceAsStaleAsync(
        string sourceUrl, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return 0;

        // Uses IX_ApiDataItems_SourceUrl index
        var itemsToMarkStale = await _context.ApiDataItems
            .Where(x => x.SourceUrl == sourceUrl && x.Status == DataStatus.Active)
            .ToListAsync(cancellationToken);

        if (itemsToMarkStale.Count == 0)
            return 0;

        foreach (var item in itemsToMarkStale)
        {
            item.MarkAsStale();
        }

        return await SaveChangesAsync(cancellationToken);
    }

    // ===== STATISTICS =====

    public async Task<ApiDataStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);

        // Parallel execution of independent queries
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

        await Task.WhenAll(activeCountTask, staleCountTask, deletedCountTask);

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
            CalculatedAt = now
        };
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**Repository Highlights:**
- ? **24 Methods** - Complete IApiDataItemRepository implementation
- ? **AsNoTracking** for read-only queries (performance)
- ? **Optimized Indexes** - All queries leverage proper indexes
- ? **Batch Operations** - AddRangeAsync, UpdateRangeAsync for efficiency
- ? **Parallel Queries** - GetStatisticsAsync executes queries concurrently
- ? **Unit of Work** - SaveChangesAsync called separately

**📖 Query Performance Table:**

| Method | Index Used | Performance |
|--------|-----------|-------------|
| `GetByExternalIdAsync` | IX_ApiDataItems_ExternalId (Unique) | O(log n) |
| `GetActiveItemsAsync` | IX_ApiDataItems_Status | O(log n + k) |
| `GetItemsNeedingRefreshAsync` | IX_ApiDataItems_LastSyncedAt_Status | O(log n + k) |
| `SearchByNameAsync` | IX_ApiDataItems_Name_Status | O(log n + k) |
| `GetItemsBySourceUrlAsync` | IX_ApiDataItems_SourceUrl | O(log n + k) |

*(k = number of matching rows)*

---

### 4. **Database Configuration Class** ? IMPLEMENTED

```csharp
/// <summary>
/// Configuration settings for database connection and Entity Framework Core.
/// </summary>
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelay { get; set; } = 30;
    public bool EnableQuerySplitting { get; set; } = false;

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return false;

        if (CommandTimeout <= 0)
            return false;

        if (MaxRetryCount < 0 || MaxRetryCount > 10)
            return false;

        if (MaxRetryDelay <= 0)
            return false;

        return true;
    }

    public string GetSanitizedConnectionString()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(
            ConnectionString,
            @"Password\s*=\s*[^;]+",
            "Password=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
```

**Configuration in appsettings.json:**

```json
{
  "DatabaseSettings": {
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=CleanArchitecture.ApiTemplate;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "MaxRetryDelay": 30,
    "EnableQuerySplitting": false
  }
}
```

---

### 5. **API Integration Service** ? IMPLEMENTED

(Previously documented - kept as-is for reference)

---

### 6. **Dependency Injection Setup** ? IMPLEMENTED

Complete DI configuration in `InfrastructureServiceExtensions.cs`:

```csharp
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ===== DATABASE CONFIGURATION =====
    var databaseSettings = new DatabaseSettings();
    configuration.GetSection("DatabaseSettings").Bind(databaseSettings);

    if (!databaseSettings.IsValid())
    {
        throw new InvalidOperationException(
            "Invalid database configuration. Check appsettings.json");
    }

    services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(
            databaseSettings.ConnectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: databaseSettings.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(databaseSettings.MaxRetryDelay),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(databaseSettings.CommandTimeout);
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                if (databaseSettings.EnableQuerySplitting)
                {
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }
            });

        if (databaseSettings.EnableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        if (databaseSettings.EnableDetailedErrors)
        {
            options.EnableDetailedErrors();
        }
    });

    // ===== REPOSITORY REGISTRATION =====
    services.AddScoped<IApiDataItemRepository, ApiDataItemRepository>();
    // TODO: Add other repositories
    // services.AddScoped<IUserRepository, UserRepository>();
    // services.AddScoped<ITokenRepository, TokenRepository>();

    // ===== OTHER SERVICES =====
    services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddScoped<JwtTokenGenerator>();
    services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

    // ===== HTTP CLIENT =====
    services.AddHttpClient("ThirdPartyApiClient", client =>
    {
        // Configuration...
    })
    .AddHttpMessageHandler<ApiKeyHandler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // ===== CACHING =====
    services.AddMemoryCache();
    services.AddDistributedMemoryCache();

    return services;
}
```

---

## 🗄️ Database Migrations

### **Creating Migrations**
```bash
# Add new migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# View migration SQL
dotnet ef migrations script
```

### **Migration Commands with Project/Startup Paths**
```bash
# If different projects
dotnet ef migrations add InitialCreate \
  --project Infrastructure \
  --startup-project Web \
  --context ApplicationDbContext

dotnet ef database update \
  --project Infrastructure \
  --startup-project Web \
  --context ApplicationDbContext
```

---

## 🔄 Dual-Service Architecture Pattern

**Key Insight:** This project demonstrates proper separation between **Repository** (data persistence) and **Integration Service** (external APIs).

### **Why Two Services?**

```
+--------------------------------------------------------------+
�                    CQRS Query Handler                        �
�                  (Application Layer)                         �
+-------------------------------------------------------------+
                     �                  �
                     ?                  ?
       +---------------------+  +----------------------+
       �  IApiDataItemRepo   �  � IApiIntegrationSvc   �
       �  (Infrastructure)   �  �  (Infrastructure)    �
       +---------------------+  +----------------------+
                  �                         �
                  ?                         ?
          +---------------+        +---------------+
          �   Database    �        �  External API �
          �   (SQL Server)�        �  (Third-party)�
          +---------------+        +---------------+
```

**Responsibilities:**

| Service | Purpose | Concerns |
|---------|---------|----------|
| **ApiDataItemRepository** | Data Persistence | CRUD, Queries, Caching |
| **ApiIntegrationService** | External API Calls | HTTP, Retry, Circuit Breaker |

**Handler Usage Example:**
```csharp
public class GetApiDataQueryHandler : IRequestHandler<GetApiDataQuery, Result<List<ApiDataItem>>>
{
    private readonly IApiDataItemRepository _repository;
    private readonly IApiIntegrationService _apiService;
    
    public async Task<Result<List<ApiDataItem>>> Handle(
        GetApiDataQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Check cache (repository)
        var items = await _repository.GetActiveItemsAsync(cancellationToken);
        
        // 2. Identify stale data
        var staleItems = items.Where(i => i.NeedsRefresh(TimeSpan.FromHours(1))).ToList();
        
        // 3. Refresh from external API (integration service)
        foreach (var item in staleItems)
        {
            var freshData = await _apiService.GetDataByIdAsync(item.ExternalId);
            item.UpdateFromExternalSource(freshData.Name, freshData.Description);
            await _repository.UpdateAsync(item, cancellationToken);
        }
        
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<List<ApiDataItem>>.Ok(items);
    }
}
```

---

## 🔧 Infrastructure Layer Checklist

- [x] Implements all Application layer interfaces
- [x] EF Core configured with migrations
- [x] Repository pattern for ApiDataItem
- [x] HttpClient registered with IHttpClientFactory
- [x] Distributed cache configured (in-memory/Redis/SQL)
- [x] Logging configured
- [x] Connection strings managed securely
- [x] Retry policies for external calls
- [x] Entity configurations with optimized indexes
- [x] Soft delete support with query filters
- [ ] Background jobs configured (future)
- [ ] User and Token repositories (future)

---

## ✅ Best Practices

### ? DO
- Use IHttpClientFactory for HttpClient management
- Configure connection strings in appsettings
- Use retry policies for transient failures
- Implement proper logging
- Use distributed cache for scalability
- Configure EF Core with optimized indexes
- Use migrations for database changes
- Implement soft deletes with query filters
- Use AsNoTracking for read-only queries
- Validate configuration on startup

### ? DON'T
- Create HttpClient instances directly
- Hard-code connection strings
- Ignore transient failures
- Skip error logging
- Use in-memory cache in production
- Modify database manually
- Expose infrastructure details to Application layer
- Store secrets in code
- Load unnecessary data (always filter)

---

## 📝 Summary

The Infrastructure Layer:
- **Implements** Application abstractions with EF Core and repositories
- **Handles** all external concerns (database, APIs, caching)
- **Manages** data persistence with optimized queries
- **Integrates** with third-party services securely
- **Configures** database schema with migrations
- **Provides** concrete implementations following Clean Architecture

This layer contains all the **"dirty details"** that can be swapped without affecting business logic.

**Reference Files:**
- 📖 `Infrastructure/Data/ApplicationDbContext.cs` - DbContext implementation
- 📖 `Infrastructure/Data/Configurations/ApiDataItemConfiguration.cs` - Entity configuration
- 📖 `Infrastructure/Repositories/ApiDataItemRepository.cs` - Repository implementation
- 📖 `Infrastructure/Data/DatabaseSettings.cs` - Configuration class
- 📖 `Presentation/Extensions/DependencyInjection/InfrastructureServiceExtensions.cs` - DI setup

---

**Last Updated:** January 2025  
**Status:** ? Database and Repository implementation complete  
**Next Steps:** Implement User and Token repositories
