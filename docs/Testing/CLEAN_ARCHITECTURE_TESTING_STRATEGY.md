# Testing Strategy for Clean Architecture

> *"Testing leads to failure, and failure leads to understanding."*  
> — **Burt Rutan**, Aerospace Engineer

> *"Code without tests is broken by design."*  
> — **Jacob Kaplan-Moss**, Django Co-Creator

---

**?? New to Clean Architecture or DDD?**  
Read **[Architecture Patterns Explained](../ARCHITECTURE_PATTERNS_EXPLAINED.md)** first to understand how Clean Architecture and Domain-Driven Design work together in this project.

---

## ?? Table of Contents

1. [Overview](#-overview)
2. [Testing Pyramid](#-testing-pyramid)
3. [Test Projects Structure](#-test-projects-structure)
4. [Domain Layer Unit Tests](#-1-domain-layer-unit-tests)
5. [Application Layer Unit Tests](#-2-application-layer-unit-tests)
6. [Infrastructure Integration Tests](#-3-infrastructure-integration-tests)
7. [Web/API Functional Tests](#-4-webapi-functional-tests)
8. [Architecture Tests](#-5-architecture-tests)
   - [Aggregate Root Architecture Tests](#aggregate-root-architecture-tests-)
   - [Test Categories](#test-categories)
   - [Running Architecture Tests](#running-architecture-tests)
9. [Required Testing Packages](#-required-testing-packages)
10. [Testing Best Practices](#-testing-best-practices)
11. [Summary](#-summary)
12. [Related Documentation](#-related-documentation)
13. [Contact & Support](#-contact--support)

---

## ?? Overview
A comprehensive testing strategy for the CleanArchitecture.ApiTemplate Clean Architecture implementation. This document covers unit tests, integration tests, and functional tests across all layers.

---

## ?? Testing Pyramid

```
                    /\
                   /  \
                  /E2E \       Fewer, Slower, More Expensive
                 /------\
                /  Inte- \     Medium Coverage
               /  gration \
              /------------\
             /    Unit      \  More, Faster, Cheaper
            /    Tests      \
           /------------------\
```

- **Unit Tests**: 70% of tests - Fast, isolated, test business logic
- **Integration Tests**: 20% of tests - Test layer boundaries and external dependencies
- **E2E/Functional Tests**: 10% of tests - Test complete user workflows

---

## ?? Test Projects Structure

```
tests/
+-- CleanArchitecture.ApiTemplate.Domain.UnitTests/
¦   +-- Entities/
¦   ¦   +-- SampleEntityTests.cs
¦   ¦   +-- BaseEntityTests.cs
¦   +-- ValueObjects/
¦   ¦   +-- EmailTests.cs
¦   ¦   +-- PhoneNumberTests.cs
¦   +-- Exceptions/
¦       +-- DomainExceptionTests.cs
¦
+-- CleanArchitecture.ApiTemplate.Application.UnitTests/
¦   +-- Features/
¦   ¦   +-- SampleData/
¦   ¦       +-- Queries/
¦   ¦       ¦   +-- GetApiDataQueryHandlerTests.cs
¦   ¦       ¦   +-- GetApiDataByIdQueryHandlerTests.cs
¦   ¦       +-- Commands/
¦   ¦           +-- CreateSampleDataCommandHandlerTests.cs
¦   +-- Behaviors/
¦   ¦   +-- CachingBehaviorTests.cs
¦   ¦   +-- ValidationBehaviorTests.cs
¦   ¦   +-- LoggingBehaviorTests.cs
¦   +-- Common/
¦       +-- ResultTests.cs
¦
+-- CleanArchitecture.ApiTemplate.Infrastructure.IntegrationTests/
¦   +-- Persistence/
¦   ¦   +-- ApplicationDbContextTests.cs
¦   ¦   +-- RepositoryTests.cs
¦   +-- Services/
¦   ¦   +-- ApiIntegrationServiceTests.cs
¦   ¦   +-- CacheServiceTests.cs
¦   +-- TestFixtures/
¦       +-- DatabaseFixture.cs
¦
+-- CleanArchitecture.ApiTemplate.Web.FunctionalTests/
¦   +-- Controllers/
¦   ¦   +-- SampleDataControllerTests.cs
¦   +-- Pages/
¦   ¦   +-- SampleDataPageTests.cs
¦   +-- TestFixtures/
¦       +-- WebApplicationTestFixture.cs
¦
+-- CleanArchitecture.ApiTemplate.ArchitectureTests/
    +-- ArchitectureTests.cs
```

---

## ?? 1. Domain Layer Unit Tests

### **Purpose**
Test business rules and domain logic in complete isolation.

### **Characteristics**
- ? No dependencies (pure C#)
- ?? Very fast execution
- ?? No mocking needed
- ? Test business rules enforcement

### **Example: Entity Tests**
```csharp
using Xunit;
using FluentAssertions;
using CleanArchitecture.ApiTemplate.Domain.Entities;
using CleanArchitecture.ApiTemplate.Domain.Exceptions;

namespace CleanArchitecture.ApiTemplate.Domain.UnitTests.Entities;

public class SampleEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateEntity()
    {
        // Arrange
        var name = "Test Entity";
        var description = "Test Description";
        
        // Act
        var entity = SampleEntity.Create(name, description);
        
        // Assert
        entity.Should().NotBeNull();
        entity.Id.Should().NotBeEmpty();
        entity.Name.Should().Be(name);
        entity.Description.Should().Be(description);
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowDomainException(string invalidName)
    {
        // Arrange
        var description = "Test Description";
        
        // Act
        Action act = () => SampleEntity.Create(invalidName, description);
        
        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Name cannot be empty*");
    }
    
    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var entity = SampleEntity.Create("Test", "Description");
        
        // Act
        entity.Deactivate();
        
        // Assert
        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateEntity()
    {
        // Arrange
        var entity = SampleEntity.Create("Original Name", "Original Description");
        var newName = "Updated Name";
        var newDescription = "Updated Description";
        
        // Act
        entity.UpdateDetails(newName, newDescription);
        
        // Assert
        entity.Name.Should().Be(newName);
        entity.Description.Should().Be(newDescription);
        entity.UpdatedAt.Should().NotBeNull();
    }
}
```

### **Example: Value Object Tests**
```csharp
public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@company.co.uk")]
    public void Create_WithValidEmail_ShouldCreateEmail(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);
        
        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    public void Create_WithInvalidEmail_ShouldThrowDomainException(string invalidEmail)
    {
        // Act
        Action act = () => Email.Create(invalidEmail);
        
        // Assert
        act.Should().Throw<DomainException>();
    }
    
    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");
        
        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }
}
```

---

## ?? 2. Application Layer Unit Tests

### **Purpose**
Test use case orchestration, handlers, and pipeline behaviors with mocked dependencies.

### **Characteristics**
- ?? Mock infrastructure dependencies
- ?? Test handler logic
- ?? Test pipeline behaviors
- ? Fast execution

### **Example: Query Handler Tests**
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using CleanArchitecture.ApiTemplate.Application.Features.SampleData.Queries;
using CleanArchitecture.ApiTemplate.Application.Common.Interfaces;
using CleanArchitecture.ApiTemplate.Application.Common.Models;

namespace CleanArchitecture.ApiTemplate.Application.UnitTests.Features.SampleData.Queries;

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
            new() { Id = "1", Name = "Test 1", Description = "Desc 1" },
            new() { Id = "2", Name = "Test 2", Description = "Desc 2" }
        };
        
        _mockApiService
            .Setup(x => x.GetAllDataAsync<List<SampleDataDto>>(It.IsAny<string>()))
            .ReturnsAsync(Result<List<SampleDataDto>>.Ok(expectedData));
        
        var query = new GetApiDataQuery();
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().BeEquivalentTo(expectedData);
        
        _mockApiService.Verify(
            x => x.GetAllDataAsync<List<SampleDataDto>>(It.IsAny<string>()), 
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_FailedApiCall_ReturnsFailureResult()
    {
        // Arrange
        var errorMessage = "API error occurred";
        
        _mockApiService
            .Setup(x => x.GetAllDataAsync<List<SampleDataDto>>(It.IsAny<string>()))
            .ReturnsAsync(Result<List<SampleDataDto>>.Fail(errorMessage));
        
        var query = new GetApiDataQuery();
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.Data.Should().BeNull();
    }
    
    [Fact]
    public async Task Handle_ApiServiceThrowsException_ThrowsException()
    {
        // Arrange
        _mockApiService
            .Setup(x => x.GetAllDataAsync<List<SampleDataDto>>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected error"));
        
        var query = new GetApiDataQuery();
        
        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Unexpected error");
    }
}
```

### **Example: Command Handler Tests**
```csharp
public class CreateSampleDataCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<CreateSampleDataCommandHandler>> _mockLogger;
    private readonly CreateSampleDataCommandHandler _handler;
    
    public CreateSampleDataCommandHandlerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockLogger = new Mock<ILogger<CreateSampleDataCommandHandler>>();
        _handler = new CreateSampleDataCommandHandler(_mockContext.Object, _mockLogger.Object);
        
        // Setup mock DbSet
        var mockDbSet = new Mock<DbSet<SampleEntity>>();
        _mockContext.Setup(x => x.SampleEntities).Returns(mockDbSet.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
    
    [Fact]
    public async Task Handle_WithValidCommand_CreatesEntityAndReturnsId()
    {
        // Arrange
        var command = new CreateSampleDataCommand
        {
            Name = "Test Entity",
            Description = "Test Description"
        };
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Data, out _).Should().BeTrue();
        
        _mockContext.Verify(
            x => x.SampleEntities.AddAsync(It.IsAny<SampleEntity>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        _mockContext.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
```

### **Example: Pipeline Behavior Tests**
```csharp
public class CachingBehaviorTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<CachingBehavior<TestQuery, string>>> _mockLogger;
    private readonly CachingBehavior<TestQuery, string> _behavior;
    
    public CachingBehaviorTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CachingBehavior<TestQuery, string>>>();
        _behavior = new CachingBehavior<TestQuery, string>(_mockCache.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValue()
    {
        // Arrange
        var request = new TestQuery();
        var cachedValue = "Cached Result";
        var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedValue));
        
        _mockCache
            .Setup(x => x.GetAsync(request.CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);
        
        var nextCalled = false;
        Task<string> Next() 
        {
            nextCalled = true;
            return Task.FromResult("Fresh Result");
        }
        
        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);
        
        // Assert
        result.Should().Be(cachedValue);
        nextCalled.Should().BeFalse(); // Handler should not be called
    }
    
    [Fact]
    public async Task Handle_CacheMiss_ExecutesHandlerAndCachesResult()
    {
        // Arrange
        var request = new TestQuery();
        var freshValue = "Fresh Result";
        
        _mockCache
            .Setup(x => x.GetAsync(request.CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[])null);
        
        Task<string> Next() => Task.FromResult(freshValue);
        
        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);
        
        // Assert
        result.Should().Be(freshValue);
        _mockCache.Verify(
            x => x.SetAsync(
                request.CacheKey, 
                It.IsAny<byte[]>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}

// Test query that implements ICacheable
public record TestQuery : IRequest<string>, ICacheable
{
    public string CacheKey => "test-key";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(5);
    public TimeSpan? SlidingExpiration => null;
}
```

---

## ?? 3. Infrastructure Integration Tests

### **Purpose**
Test infrastructure implementations against real external dependencies (database, cache, APIs).

### **Characteristics**
- ?? Use real database (test DB or in-memory)
- ??? Test EF Core configurations
- ?? Test external API integrations
- ?? Slower than unit tests

### **Example: Database Context Tests**
```csharp
using Xunit;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using CleanArchitecture.ApiTemplate.Infrastructure.Persistence;
using CleanArchitecture.ApiTemplate.Domain.Entities;

namespace CleanArchitecture.ApiTemplate.Infrastructure.IntegrationTests.Persistence;

public class ApplicationDbContextTests : IClassFixture<DatabaseFixture>
{
    private readonly ApplicationDbContext _context;
    
    public ApplicationDbContextTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
    }
    
    [Fact]
    public async Task SaveChangesAsync_WithNewEntity_SetsCreatedAt()
    {
        // Arrange
        var entity = SampleEntity.Create("Test", "Description");
        
        // Act
        await _context.SampleEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        
        // Assert
        var savedEntity = await _context.SampleEntities.FindAsync(entity.Id);
        savedEntity.Should().NotBeNull();
        savedEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        savedEntity.UpdatedAt.Should().BeNull();
    }
    
    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_SetsUpdatedAt()
    {
        // Arrange
        var entity = SampleEntity.Create("Test", "Description");
        await _context.SampleEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        
        // Act
        entity.UpdateDetails("Updated Name", "Updated Description");
        await _context.SaveChangesAsync();
        
        // Assert
        var updatedEntity = await _context.SampleEntities.FindAsync(entity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity.UpdatedAt.Should().NotBeNull();
        updatedEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public async Task Query_WithIndex_ShouldExecuteEfficiently()
    {
        // Arrange
        var entity1 = SampleEntity.Create("Alpha", "Description");
        var entity2 = SampleEntity.Create("Beta", "Description");
        await _context.SampleEntities.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _context.SampleEntities
            .Where(e => e.Name == "Alpha")
            .FirstOrDefaultAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Alpha");
    }
}

// Test Fixture
public class DatabaseFixture : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    
    public DatabaseFixture()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
    }
    
    public ApplicationDbContext CreateContext()
    {
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        
        return new ApplicationDbContext(_options, mockDateTime.Object);
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

### **Example: API Integration Service Tests**
```csharp
public class ApiIntegrationServiceIntegrationTests
{
    private readonly ApiIntegrationService _service;
    private readonly HttpClient _httpClient;
    
    public ApiIntegrationServiceIntegrationTests()
    {
        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
        };
        
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);
        
        var logger = new Mock<ILogger<ApiIntegrationService>>();
        var cache = new Mock<IDistributedCache>();
        
        _service = new ApiIntegrationService(
            httpClientFactory.Object, 
            logger.Object, 
            cache.Object);
    }
    
    [Fact]
    public async Task GetAllDataAsync_WithRealApi_ReturnsData()
    {
        // Act
        var result = await _service.GetAllDataAsync<List<JsonPlaceholderPost>>("posts");
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task GetDataByIdAsync_WithValidId_ReturnsData()
    {
        // Act
        var result = await _service.GetDataByIdAsync<JsonPlaceholderPost>("posts", "1");
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(1);
    }
}

public class JsonPlaceholderPost
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
}
```

---

## ?? 4. Web/API Functional Tests

### **Purpose**
Test complete HTTP request/response flows end-to-end.

### **Characteristics**
- ?? Test full HTTP pipeline
- ?? Use WebApplicationFactory
- ?? Test middleware, controllers, handlers
- ?? Slower execution

### **Example: Controller Functional Tests**
```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.ApiTemplate.Web.FunctionalTests.Controllers;

public class SampleDataControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public SampleDataControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetAll_ReturnsSuccessWithData()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/sampledata");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        
        var data = await response.Content.ReadFromJsonAsync<List<SampleDataDto>>();
        data.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetById_WithValidId_ReturnsData()
    {
        // Arrange
        var id = "1";
        
        // Act
        var response = await _client.GetAsync($"/api/v1/sampledata/{id}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var data = await response.Content.ReadFromJsonAsync<SampleDataDto>();
        data.Should().NotBeNull();
        data.Id.Should().Be(id);
    }
    
    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateSampleRequest
        {
            Name = "Test Item",
            Description = "Test Description"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sampledata", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
    
    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSampleRequest
        {
            Name = "", // Invalid: empty name
            Description = "Test Description"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sampledata", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

---

## ??? 5. Architecture Tests

### **Purpose**
Enforce architectural rules and layer dependencies at compile/test time.

### **Characteristics**
- ??? Enforce Clean Architecture rules
- ?? Prevent circular dependencies
- ?? Verify naming conventions
- ?? Check layer isolation
- ? **Enforce aggregate root rules** ?

### **Example: NetArchTest**
```csharp
using NetArchTest.Rules;
using Xunit;
using FluentAssertions;

namespace CleanArchitecture.ApiTemplate.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "CleanArchitecture.ApiTemplate.Domain";
    private const string ApplicationNamespace = "CleanArchitecture.ApiTemplate.Application";
    private const string InfrastructureNamespace = "CleanArchitecture.ApiTemplate.Infrastructure";
    private const string WebNamespace = "CleanArchitecture.ApiTemplate.Web";
    
    [Fact]
    public void Domain_ShouldNotHaveDependencyOnOtherLayers()
    {
        // Arrange
        var assembly = typeof(CleanArchitecture.ApiTemplate.Domain.Entities.BaseEntity).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .And().NotHaveDependencyOn(InfrastructureNamespace)
            .And().NotHaveDependencyOn(WebNamespace)
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Application_ShouldOnlyDependOnDomain()
    {
        // Arrange
        var assembly = typeof(CleanArchitecture.ApiTemplate.Application.DependencyInjection).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .And().NotHaveDependencyOn(WebNamespace)
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Handlers_ShouldHaveDependencyOnMediatR()
    {
        // Arrange
        var assembly = typeof(CleanArchitecture.ApiTemplate.Application.DependencyInjection).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().HaveNameEndingWith("Handler")
            .Should().HaveDependencyOn("MediatR")
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Controllers_ShouldHaveDependencyOnMediatR()
    {
        // Arrange
        var assembly = typeof(Program).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().HaveNameEndingWith("Controller")
            .Should().HaveDependencyOn("MediatR")
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}
```

---

### **Aggregate Root Architecture Tests** ? **NEW**

Enforce DDD aggregate root rules and invariants with architecture tests.

#### **Test 1: All Domain Entities Must Implement IAggregateRoot**

```csharp
using NetArchTest.Rules;
using Xunit;
using FluentAssertions;
using CleanArchitecture.ApiTemplate.Core.Domain.Common;
using CleanArchitecture.ApiTemplate.Core.Domain.Entities;

namespace CleanArchitecture.ApiTemplate.ArchitectureTests;

public class AggregateRootTests
{
    [Fact]
    public void DomainEntities_Should_ImplementIAggregateRoot()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespace("CleanArchitecture.ApiTemplate.Core.Domain.Entities")
            .And().DoNotHaveNameMatching("Base*") // Exclude BaseEntity itself
            .Should().ImplementInterface(typeof(IAggregateRoot))
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All domain entities in Entities folder must implement IAggregateRoot to serve as aggregate roots");
    }
    
    [Fact]
    public void AggregateRoots_Should_InheritFromBaseEntity()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .Should().Inherit(typeof(BaseEntity))
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All aggregate roots should inherit from BaseEntity for consistent audit fields");
    }
}
```

---

#### **Test 2: Aggregate Roots Must Have Domain Events Collection**

```csharp
public class AggregateInvariantTests
{
    [Fact]
    public void AggregateRoots_Should_ExposeDomainEventsCollection()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var hasEventsProperty = type
                .GetProperties()
                .Any(p => p.Name == "DomainEvents" && 
                         p.PropertyType.Name.Contains("IReadOnlyCollection"));
            
            hasEventsProperty.Should().BeTrue(
                $"{type.Name} must expose a DomainEvents property of type IReadOnlyCollection<IDomainEvent>");
        }
    }
    
    [Fact]
    public void AggregateRoots_Should_HaveClearDomainEventsMethod()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var hasClearMethod = type
                .GetMethods()
                .Any(m => m.Name == "ClearDomainEvents" && 
                         m.ReturnType == typeof(void) &&
                         m.GetParameters().Length == 0);
            
            hasClearMethod.Should().BeTrue(
                $"{type.Name} must implement ClearDomainEvents() method for event cleanup");
        }
    }
}
```

---

#### **Test 3: Aggregate Roots Must Encapsulate Collections**

```csharp
public class EncapsulationTests
{
    [Fact]
    public void AggregateRoots_Should_NotExposeSettableCollections()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var settableCollections = type
                .GetProperties()
                .Where(p => 
                    (p.PropertyType.IsGenericType && 
                     p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) &&
                    p.SetMethod?.IsPublic == true)
                .ToList();
            
            settableCollections.Should().BeEmpty(
                $"{type.Name} should not expose List<T> properties with public setters. " +
                $"Use IReadOnlyCollection<T> instead to protect invariants. " +
                $"Found: {string.Join(", ", settableCollections.Select(p => p.Name))}");
        }
    }
    
    [Fact]
    public void AggregateRoots_Should_ExposeReadOnlyCollections()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var collectionProperties = type
                .GetProperties()
                .Where(p => 
                    p.PropertyType.IsGenericType && 
                    p.PropertyType.Name.Contains("Collection"))
                .ToList();
            
            foreach (var prop in collectionProperties)
            {
                var isReadOnly = prop.PropertyType.Name.Contains("IReadOnly");
                
                isReadOnly.Should().BeTrue(
                    $"{type.Name}.{prop.Name} should be IReadOnlyCollection<T> or IReadOnlyList<T> " +
                    $"to enforce encapsulation and protect aggregate invariants");
            }
        }
    }
}
```

---

#### **Test 4: Aggregate Roots Must Have Factory Methods**

```csharp
public class FactoryMethodTests
{
    [Fact]
    public void AggregateRoots_Should_HaveStaticCreateMethod()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var hasCreateMethod = type
                .GetMethods(System.Reflection.BindingFlags.Public | 
                           System.Reflection.BindingFlags.Static)
                .Any(m => m.Name.StartsWith("Create") && 
                         m.ReturnType == type);
            
            hasCreateMethod.Should().BeTrue(
                $"{type.Name} should have a static Create() factory method " +
                $"for controlled entity creation with validation");
        }
    }
    
    [Fact]
    public void AggregateRoots_Should_HavePrivateOrProtectedConstructor()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var publicParameterlessConstructor = type
                .GetConstructors(System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance)
                .Where(c => c.GetParameters().Length == 0)
                .ToList();
            
            publicParameterlessConstructor.Should().BeEmpty(
                $"{type.Name} should not have a public parameterless constructor. " +
                $"Use private/protected constructor + static Create() factory method instead");
        }
    }
}
```

---

#### **Test 5: Aggregate Roots Must Use Value Objects**

```csharp
public class ValueObjectUsageTests
{
    [Fact]
    public void AggregateRoots_Should_UseEmailValueObject()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var emailProperties = type
                .GetProperties()
                .Where(p => p.Name.ToLower().Contains("email"))
                .ToList();
            
            foreach (var prop in emailProperties)
            {
                var usesValueObject = prop.PropertyType.Name == "Email" ||
                                     prop.PropertyType.Namespace?.Contains("ValueObjects") == true;
                
                usesValueObject.Should().BeTrue(
                    $"{type.Name}.{prop.Name} should use Email value object instead of string " +
                    $"to enforce validation and encapsulation");
            }
        }
    }
    
    [Fact]
    public void AggregateRoots_Should_UseEnumsForStatus()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        var aggregateRootTypes = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .GetTypes();
        
        // Act & Assert
        foreach (var type in aggregateRootTypes)
        {
            var statusProperties = type
                .GetProperties()
                .Where(p => p.Name.ToLower().Contains("status"))
                .ToList();
            
            foreach (var prop in statusProperties)
            {
                var isEnum = prop.PropertyType.IsEnum;
                
                isEnum.Should().BeTrue(
                    $"{type.Name}.{prop.Name} should use an enum (UserStatus, TokenStatus, DataStatus) " +
                    $"instead of string to prevent invalid values and ensure type safety");
            }
        }
    }
}
```

---

#### **Test 6: Aggregate Roots Naming Conventions**

```csharp
public class NamingConventionTests
{
    [Fact]
    public void AggregateRoots_Should_NotHaveManagerOrServiceSuffix()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .Should().NotHaveNameEndingWith("Manager")
            .And().NotHaveNameEndingWith("Service")
            .And().NotHaveNameEndingWith("Helper")
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Aggregate roots should represent domain entities (User, Token, Order), " +
            "not services or managers. Services belong in Application or Domain Services.");
    }
    
    [Fact]
    public void AggregateRoots_Should_BeInEntitiesNamespace()
    {
        // Arrange
        var assembly = typeof(BaseEntity).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().ImplementInterface(typeof(IAggregateRoot))
            .And().DoNotHaveNameMatching("Base*")
            .Should().ResideInNamespace("CleanArchitecture.ApiTemplate.Core.Domain.Entities")
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All aggregate roots must reside in Core.Domain.Entities namespace");
    }
}
```

---

#### **Test 7: Aggregate Consistency Rules**

```csharp
public class ConsistencyRuleTests
{
    [Fact]
    public void User_Should_EnforceRoleInvariant()
    {
        // Arrange
        var userType = typeof(User);
        
        // Act - Check for RemoveRole method
        var removeRoleMethod = userType.GetMethod("RemoveRole");
        
        // Assert
        removeRoleMethod.Should().NotBeNull(
            "User aggregate must have RemoveRole() method to enforce role invariant");
        
        // Verify method throws exception for last role (via reflection on XML docs or tests)
        var methodBody = removeRoleMethod?.GetMethodBody();
        methodBody.Should().NotBeNull(
            "RemoveRole should contain logic to prevent removing the last role");
    }
    
    [Fact]
    public void ApiDataItem_Should_EnforceStatusTransitions()
    {
        // Arrange
        var apiDataItemType = typeof(ApiDataItem);
        
        // Act
        var markAsActiveMethod = apiDataItemType.GetMethod("MarkAsActive");
        var markAsDeletedMethod = apiDataItemType.GetMethod("MarkAsDeleted");
        
        // Assert
        markAsActiveMethod.Should().NotBeNull(
            "ApiDataItem must have MarkAsActive() method for status management");
        
        markAsDeletedMethod.Should().NotBeNull(
            "ApiDataItem must have MarkAsDeleted() method for status management");
        
        // Verify status property is not publicly settable
        var statusProperty = apiDataItemType.GetProperty("Status");
        statusProperty?.SetMethod?.IsPublic.Should().BeFalse(
            "Status property should not be publicly settable - use methods to enforce business rules");
    }
}
```

---

### **Running Architecture Tests**

#### **Test Project Setup**

```xml
<!-- tests/CleanArchitecture.ApiTemplate.ArchitectureTests/CleanArchitecture.ApiTemplate.ArchitectureTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\Domain\CleanArchitecture.ApiTemplate.Core.Domain.csproj" />
    <ProjectReference Include="..\..\Core\Application\CleanArchitecture.ApiTemplate.Core.Application.csproj" />
    <ProjectReference Include="..\..\Infrastructure\CleanArchitecture.ApiTemplate.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

---

#### **CI/CD Integration**

```yaml
# .github/workflows/architecture-tests.yml
name: Architecture Tests

on:
  pull_request:
    branches: [ main, dev ]
  push:
    branches: [ main, dev ]

jobs:
  architecture-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Run Architecture Tests
      run: dotnet test tests/CleanArchitecture.ApiTemplate.ArchitectureTests/CleanArchitecture.ApiTemplate.ArchitectureTests.csproj --logger "console;verbosity=detailed"
    
    - name: Fail build if architecture violations found
      if: failure()
      run: |
        echo "? Architecture tests failed!"
        echo "Please review the test output above for violations."
        exit 1
```

---

### **Benefits of Architecture Tests**

| Benefit | Explanation |
|---------|-------------|
| **Automated Enforcement** | Rules enforced at test time, not code review |
| **Early Detection** | Violations caught in CI/CD before merge |
| **Documentation** | Tests serve as executable architecture documentation |
| **Consistency** | Ensures all developers follow same patterns |
| **Refactoring Safety** | Tests prevent architectural drift during changes |
| **Onboarding** | New developers learn rules through failing tests |

---

### **Example Test Output**

```
? FAILED: AggregateRoots_Should_NotExposeSettableCollections
   Expected settableCollections to be empty because User should not expose 
   List<T> properties with public setters. Use IReadOnlyCollection<T> instead 
   to protect invariants. Found: Roles, but found 1 item(s).

? PASSED: DomainEntities_Should_ImplementIAggregateRoot
? PASSED: AggregateRoots_Should_InheritFromBaseEntity
? PASSED: AggregateRoots_Should_ExposeReadOnlyCollections
? PASSED: User_Should_EnforceRoleInvariant

Total: 5 tests, 4 passed, 1 failed
