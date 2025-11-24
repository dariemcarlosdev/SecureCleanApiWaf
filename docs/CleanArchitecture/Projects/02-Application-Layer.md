# CleanArchitecture.ApiTemplate.Application Project

> *"The application layer is thin. It does not contain business rules or knowledge, but only coordinates tasks and delegates work to collaborations of domain objects in the next layer down."*  
> � **Eric Evans**, Domain-Driven Design

---

**📚 New to Clean Architecture or DDD?**  
Read **[Architecture Patterns Explained](../ARCHITECTURE_PATTERNS_EXPLAINED.md)** first to understand how Clean Architecture and Domain-Driven Design work together in this project.

---

## 📖 Overview
The **Application Layer** contains all application business logic and orchestrates the flow of data to and from the Domain layer. It implements use cases through CQRS patterns using MediatR and defines interfaces for infrastructure services.

---

## 🎯 Purpose
- Define use cases as Commands and Queries (CQRS)
- Orchestrate domain objects to fulfill business requirements
- Define interfaces for infrastructure dependencies (abstractions)
- Implement cross-cutting concerns via pipeline behaviors
- Map between domain entities and DTOs
- Remain independent of UI and infrastructure implementations

---

## 📁 Project Structure

```
CleanArchitecture.ApiTemplate.Application/
📖? Common/
?   📖? Behaviors/                    # MediatR Pipeline Behaviors
?   ?   📖? CachingBehavior.cs       # Response caching
?   ?   📖? LoggingBehavior.cs       # Request/response logging
?   ?   📖? ValidationBehavior.cs    # FluentValidation
?   ?   📖? PerformanceBehavior.cs   # Performance monitoring
?   ?
?   📖? Interfaces/                   # Abstractions for Infrastructure
?   ?   📖? IApplicationDbContext.cs # Database abstraction
?   ?   📖? IApiIntegrationService.cs # External API abstraction
?   ?   📖? ICacheService.cs         # Caching abstraction
?   ?   📖? IDateTime.cs             # Time abstraction (for testing)
?   ?   📖? IEmailService.cs         # Email sending abstraction
?   ?
?   📖? Models/                       # Shared models and results
?   ?   📖? Result.cs                # Result<T> pattern
?   ?   📖? PaginatedList.cs         # Pagination wrapper
?   ?   📖? ErrorDetails.cs          # Error response model
?   ?
?   📖? Mappings/                     # AutoMapper profiles
?   ?   📖? MappingProfile.cs        # Entity to DTO mappings
?   ?
?   📖? Exceptions/                   # Application exceptions
?       📖? ValidationException.cs
?       📖? NotFoundException.cs
?       📖? BadRequestException.cs
?
📖? Features/                         # Feature-based organization (Vertical Slices)
?   📖? SampleData/
?   ?   📖? Commands/
?   ?   ?   📖? CreateSampleData/
?   ?   ?   ?   📖? CreateSampleDataCommand.cs
?   ?   ?   ?   📖? CreateSampleDataCommandHandler.cs
?   ?   ?   ?   📖? CreateSampleDataCommandValidator.cs
?   ?   ?   📖? UpdateSampleData/
?   ?   ?       📖? UpdateSampleDataCommand.cs
?   ?   ?       📖? UpdateSampleDataCommandHandler.cs
?   ?   ?
?   ?   📖? Queries/
?   ?       📖? GetApiData/
?   ?       ?   📖? GetApiDataQuery.cs        # Implements ICacheable
?   ?       ?   📖? GetApiDataQueryHandler.cs
?   ?       ?   📖? SampleDataDto.cs          # Response DTO
?   ?       📖? GetApiDataById/
?   ?           📖? GetApiDataByIdQuery.cs
?   ?           📖? GetApiDataByIdQueryHandler.cs
?   ?
?   ?📖 [OtherFeatures]/
?       📖? Commands/
?       📖? Queries/
?
📖? DependencyInjection.cs            # Extension method: AddApplication()

```

---

## 🔑 Key Concepts

### 1. **CQRS Pattern with MediatR**

#### **Query (Read Operation)**
```csharp
/// <summary>
/// Query to retrieve all sample data with caching
/// Implements ICacheable for automatic caching via CachingBehavior
/// </summary>
public record GetApiDataQuery : IRequest<Result<List<SampleDataDto>>>, ICacheable
{
    // Unique cache key for this query
    public string CacheKey => "GetApiData_All";
    
    // Cache for 5 minutes
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(5);
    
    // Sliding expiration: reset timer on access
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(2);
}

/// <summary>
/// Handler for GetApiDataQuery
/// Depends on IApiIntegrationService abstraction (implemented in Infrastructure)
/// </summary>
public class GetApiDataQueryHandler : IRequestHandler<GetApiDataQuery, Result<List<SampleDataDto>>>
{
    private readonly IApiIntegrationService _apiService;
    private readonly ILogger<GetApiDataQueryHandler> _logger;
    
    public GetApiDataQueryHandler(
        IApiIntegrationService apiService,
        ILogger<GetApiDataQueryHandler> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }
    
    public async Task<Result<List<SampleDataDto>>> Handle(
        GetApiDataQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetApiDataQuery");
        
        // Call infrastructure service via abstraction
        var result = await _apiService.GetAllDataAsync<List<SampleDataDto>>("api/data");
        
        if (!result.Success)
        {
            _logger.LogError("Failed to retrieve data: {Error}", result.Error);
            return Result<List<SampleDataDto>>.Fail(result.Error);
        }
        
        return Result<List<SampleDataDto>>.Ok(result.Data);
    }
}

/// <summary>
/// Data Transfer Object for API responses
/// </summary>
public record SampleDataDto
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

#### **Command (Write Operation)**
```csharp
/// <summary>
/// Command to create new sample data
/// </summary>
public record CreateSampleDataCommand : IRequest<Result<string>>
{
    public string Name { get; init; }
    public string Description { get; init; }
}

/// <summary>
/// Validator for CreateSampleDataCommand using FluentValidation
/// </summary>
public class CreateSampleDataCommandValidator : AbstractValidator<CreateSampleDataCommand>
{
    public CreateSampleDataCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}

/// <summary>
/// Handler for CreateSampleDataCommand
/// </summary>
public class CreateSampleDataCommandHandler : IRequestHandler<CreateSampleDataCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateSampleDataCommandHandler> _logger;
    
    public CreateSampleDataCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateSampleDataCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Result<string>> Handle(
        CreateSampleDataCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Create domain entity using factory method
            var entity = SampleEntity.Create(request.Name, request.Description);
            
            // Add to DbContext via abstraction
            await _context.SampleEntities.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Created sample data with ID: {Id}", entity.Id);
            
            return Result<string>.Ok(entity.Id.ToString());
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain validation failed");
            return Result<string>.Fail(ex.Message);
        }
    }
}
```

---

### 2. **Pipeline Behaviors (Cross-Cutting Concerns)**

#### **Caching Behavior**
```csharp
/// <summary>
/// Interface for cacheable queries
/// Queries implementing this interface will be automatically cached
/// </summary>
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? AbsoluteExpirationRelativeToNow { get; }
    TimeSpan? SlidingExpiration { get; }
}

/// <summary>
/// MediatR pipeline behavior for automatic caching of query responses
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    
    public CachingBehavior(
        IDistributedCache cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache if request implements ICacheable
        if (request is not ICacheable cacheableRequest)
        {
            return await next();
        }
        
        var cacheKey = cacheableRequest.CacheKey;
        
        // Try to get from cache
        var cachedResponse = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedResponse))
        {
            _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedResponse);
        }
        
        _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
        
        // Execute handler
        var response = await next();
        
        // Cache the response
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheableRequest.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = cacheableRequest.SlidingExpiration
        };
        
        var serializedResponse = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(cacheKey, serializedResponse, options, cancellationToken);
        
        _logger.LogInformation("Cached response for key: {CacheKey}", cacheKey);
        
        return response;
    }
}
```

#### **Validation Behavior**
```csharp
/// <summary>
/// Validates requests using FluentValidation before handler execution
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }
        
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
        
        return await next();
    }
}
```

#### **Logging Behavior**
```csharp
/// <summary>
/// Logs all requests and responses for debugging and monitoring
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);
        
        var response = await next();
        
        _logger.LogInformation("Handled {RequestName}: {@Response}", requestName, response);
        
        return response;
    }
}
```

#### **Performance Behavior**
```csharp
/// <summary>
/// Monitors request execution time and logs slow requests
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer;
    
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new Stopwatch();
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();
        
        var response = await next();
        
        _timer.Stop();
        
        var elapsedMilliseconds = _timer.ElapsedMilliseconds;
        
        if (elapsedMilliseconds > 500) // Threshold for slow requests
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds}ms) {@Request}",
                requestName,
                elapsedMilliseconds,
                request);
        }
        
        return response;
    }
}
```

---

### 3. **Interface Abstractions**

```csharp
/// <summary>
/// Database context abstraction (implemented by EF Core in Infrastructure)
/// </summary>
public interface IApplicationDbContext
{
    DbSet<SampleEntity> SampleEntities { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// External API integration abstraction
/// </summary>
public interface IApiIntegrationService
{
    Task<Result<T>> GetAllDataAsync<T>(string apiUrl);
    Task<Result<T>> GetDataByIdAsync<T>(string apiUrl, string id);
}

/// <summary>
/// Caching service abstraction
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// DateTime abstraction for testability
/// </summary>
public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
```

---

### 4. **Result Pattern**

```csharp
/// <summary>
/// Represents the result of an operation with success/failure state
/// </summary>
public record Result<T>
{
    public bool Success { get; init; }
    public T Data { get; init; }
    public string Error { get; init; }
    public List<string> Errors { get; init; } = new();
    
    public static Result<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };
    
    public static Result<T> Fail(string error) => new()
    {
        Success = false,
        Error = error,
        Errors = new List<string> { error }
    };
    
    public static Result<T> Fail(List<string> errors) => new()
    {
        Success = false,
        Errors = errors,
        Error = string.Join("; ", errors)
    };
}
```

---

## 📦 Dependencies

### **NuGet Packages**
```xml
<ItemGroup>
  <!-- MediatR for CQRS -->
  <PackageReference Include="MediatR" Version="12.2.0" />
  
  <!-- FluentValidation -->
  <PackageReference Include="FluentValidation" Version="11.9.0" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
  
  <!-- AutoMapper (optional) -->
  <PackageReference Include="AutoMapper" Version="12.0.1" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
  
  <!-- Caching -->
  <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="8.0.0" />
  
  <!-- Logging -->
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
</ItemGroup>

<!-- Project References -->
<ItemGroup>
  <ProjectReference Include="..\CleanArchitecture.ApiTemplate.Domain\CleanArchitecture.ApiTemplate.Domain.csproj" />
</ItemGroup>
```

---

## 🔧 Dependency Injection Setup

```csharp
/// <summary>
/// Extension method to register Application layer services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Register pipeline behaviors (order matters!)
            cfg.AddBehavior<IPipelineBehavior, LoggingBehavior>();
            cfg.AddBehavior<IPipelineBehavior, ValidationBehavior>();
            cfg.AddBehavior<IPipelineBehavior, CachingBehavior>();
            cfg.AddBehavior<IPipelineBehavior, PerformanceBehavior>();
        });
        
        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);
        
        // Register AutoMapper (optional)
        services.AddAutoMapper(assembly);
        
        return services;
    }
}
```

---

## 🧪 Testing Strategy

### **Unit Tests**
```csharp
public class GetApiDataQueryHandlerTests
{
    private readonly Mock<IApiIntegrationService> _mockApiService;
    private readonly Mock<ILogger<GetApiDataQueryHandler>> _mockLogger;
    private readonly GetApiDataQueryHandler _handler;
    
    public GetApiDataQueryHandlerTests()
    {
        _mockApiService = new Mock<IApiIntegrationService>();
        _mockLogger = new Mock<ILogger<GetApiDataQueryHandler>>();
        _handler = new GetApiDataQueryHandler(_mockApiService.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task Handle_SuccessfulApiCall_ReturnsSuccessResult()
    {
        // Arrange
        var expectedData = new List<SampleDataDto>
        {
            new() { Id = "1", Name = "Test", Description = "Test Desc" }
        };
        
        _mockApiService
            .Setup(x => x.GetAllDataAsync<List<SampleDataDto>>(It.IsAny<string>()))
            .ReturnsAsync(Result<List<SampleDataDto>>.Ok(expectedData));
        
        var query = new GetApiDataQuery();
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Test", result.Data[0].Name);
    }
    
    [Fact]
    public async Task Handle_FailedApiCall_ReturnsFailureResult()
    {
        // Arrange
        _mockApiService
            .Setup(x => x.GetAllDataAsync<List<SampleDataDto>>(It.IsAny<string>()))
            .ReturnsAsync(Result<List<SampleDataDto>>.Fail("API error"));
        
        var query = new GetApiDataQuery();
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("API error", result.Error);
    }
}
```

---

## 📦 Application Layer Checklist

- [ ] CQRS implemented with MediatR
- [ ] Commands for write operations
- [ ] Queries for read operations
- [ ] Pipeline behaviors for cross-cutting concerns
- [ ] Interfaces defined for infrastructure dependencies
- [ ] FluentValidation for input validation
- [ ] Result pattern for operation outcomes
- [ ] DTOs for data transfer
- [ ] No direct infrastructure dependencies (use abstractions)
- [ ] Feature-based folder organization

---

## ✅ Best Practices

### ? DO
- Use CQRS to separate reads from writes
- Define interfaces for all infrastructure dependencies
- Implement validation in pipeline behaviors
- Use Result<T> pattern for predictable error handling
- Keep handlers focused (single responsibility)
- Use DTOs to decouple from domain entities
- Leverage MediatR for loose coupling

### ? DON'T
- Reference infrastructure implementations directly
- Put infrastructure code in handlers
- Skip validation
- Return domain entities directly to UI
- Mix command and query logic
- Create god handlers with multiple responsibilities

---

## 📖 Migration from Current Structure

```
Current Structure 🏛️ Clean Architecture Application Layer

Features/GetData/Queries/
📖? GetApiDataQuery.cs          ? Application/Features/SampleData/Queries/GetApiData/
📖? GetApiDataQueryHandler.cs   ? Application/Features/SampleData/Queries/GetApiData/

PipelineBehaviors/
📖? CachingBehavior.cs          ? Application/Common/Behaviors/
📖? ICacheable.cs               ? Application/Common/Behaviors/

Services/Result.cs              ? Application/Common/Models/Result.cs

Models/SampleModel.cs           ? Application/Features/SampleData/Queries/GetApiData/SampleDataDto.cs
```

---

## 📝 Summary

The Application Layer:
- **Orchestrates** use cases via CQRS
- **Defines** abstractions for infrastructure
- **Implements** cross-cutting concerns via behaviors
- **Depends on** Domain layer only
- **Is independent of** infrastructure and UI
- **Enables** testability through dependency inversion

This layer is the **brain** of your application, coordinating business logic without being coupled to implementation details.
