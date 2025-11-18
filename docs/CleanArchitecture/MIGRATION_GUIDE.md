# Migration Guide: Current Structure 🏛️ Clean Architecture

Migration transformate your SecureCleanApiWaf codebase to Clean Architecture incrementally! **Good luck with your migration!** ??

## 📑 Table of Contents

### **Quick Navigation**
1. [Overview](#-overview)
2. [Migration Strategy](#-migration-strategy)
3. [Current vs Target State](#-current-vs-target-state)
   - [Current Structure](#current-structure)
   - [Target Structure (Multi-Project)](#target-structure-multi-project)
4. [Phase 1: Extract Interfaces (Week 1)](#-phase-1-extract-interfaces-week-1)
   - [Step 1.1: Create Interface for ApiIntegrationService](#step-11-create-interface-for-apiintegrationservice)
   - [Step 1.2: Create Cache Service Interface](#step-12-create-cache-service-interface)
   - [Step 1.3: Create DateTime Abstraction](#step-13-create-datetime-abstraction)
   - [Step 1.4: Verify Everything Still Works](#step-14-verify-everything-still-works)
   - [Phase 1 Checklist](#-phase-1-checklist)
5. [Phase 2: Reorganize Folders (Week 2)](#-phase-2-reorganize-folders-week-2)
   - [Step 2.1: Create Layer Folders](#step-21-create-layer-folders)
   - [Step 2.2: Move Files to Appropriate Layers](#step-22-move-files-to-appropriate-layers)
   - [Step 2.3: Update Namespaces](#step-23-update-namespaces)
   - [Step 2.4: Update Using Statements](#step-24-update-using-statements)
   - [Step 2.5: Verify Build](#step-25-verify-build)
   - [Phase 2 Checklist](#-phase-2-checklist)
6. [Phase 3: Multi-Project Structure (Weeks 3-4)](#-phase-3-multi-project-structure-weeks-3-4)
   - [Step 3.1: Create New Solution](#step-31-create-new-solution)
   - [Step 3.2: Create Domain Project](#step-32-create-domain-project)
   - [Step 3.3: Create Application Project](#step-33-create-application-project)
   - [Step 3.4: Create Infrastructure Project](#step-34-create-infrastructure-project)
   - [Step 3.5: Create Infrastructure.Azure Project](#step-35-create-infrastructureazure-project)
   - [Step 3.6: Update Web Project](#step-36-update-web-project)
   - [Step 3.7: Move Code to Appropriate Projects](#step-37-move-code-to-appropriate-projects)
   - [Step 3.8: Create DependencyInjection Extension Methods](#step-38-create-dependencyinjection-extension-methods)
   - [Step 3.9: Update Program.cs](#step-39-update-programcs)
   - [Step 3.10: Build and Test](#step-310-build-and-test)
   - [Phase 3 Checklist](#-phase-3-checklist)
7. [Phase 4: Add Tests (Ongoing)](#-phase-4-add-tests-ongoing)
   - [Step 4.1: Create Test Projects](#step-41-create-test-projects)
   - [Step 4.2: Add Testing Packages](#step-42-add-testing-packages)
   - [Step 4.3: Write Tests](#step-43-write-tests)
   - [Phase 4 Checklist](#-phase-4-checklist)
8. [Common Issues & Solutions](#-common-issues--solutions)
   - [Issue 1: Circular Dependencies](#issue-1-circular-dependencies)
   - [Issue 2: Namespace Conflicts](#issue-2-namespace-conflicts)
   - [Issue 3: Missing Using Statements](#issue-3-missing-using-statements)
   - [Issue 4: DI Registration Missing](#issue-4-di-registration-missing)
9. [Migration Checklist Summary](#-migration-checklist-summary)
10. [Tips for Success](#-tips-for-success)
11. [Next Steps](#-next-steps)
12. [Contact](#contact)

---

## 📖 Overview

This guide provides step-by-step instructions for migrating your existing SecureCleanApiWaf codebase to Clean Architecture. The migration is designed to be **incremental**, allowing you to maintain a working application throughout the process.

---

## 🎯 Migration Strategy

We'll use a **phased approach** to minimize risk:

1. **Phase 1**: Extract interfaces (Dependency Inversion)
2. **Phase 2**: Reorganize within current project
3. **Phase 3**: Create multi-project structure (optional)
4. **Phase 4**: Add comprehensive tests

Each phase can be completed independently, and you can stop at any phase based on your needs.

---

## 🔄 Current vs Target State

### **Current Structure**
```
SecureCleanApiWaf/
??? Controllers/
??? Services/
??? Caching/
??? Features/
??? Models/
??? PipelineBehaviors/
??? Extensions/
??? Components/
??? Pages/
??? Program.cs
```

### **Target Structure (Multi-Project)**
```
SecureCleanApiWaf.sln
??? src/
?   ??? Core/
?   ?   ??? SecureCleanApiWaf.Domain/
?   ?   ??? SecureCleanApiWaf.Application/
?   ??? Infrastructure/
?   ?   ??? SecureCleanApiWaf.Infrastructure/
?   ?   ??? SecureCleanApiWaf.Infrastructure.Azure/
?   ??? Presentation/
?       ??? SecureCleanApiWaf.Web/
??? tests/
    ??? Domain.UnitTests/
    ??? Application.UnitTests/
    ??? Infrastructure.IntegrationTests/
    ??? Web.FunctionalTests/
```

---

## ✅ Phase 1: Extract Interfaces (Week 1)

**Goal**: Implement Dependency Inversion without changing structure.

### **Step 1.1: Create Interface for ApiIntegrationService**

1. Create new folder: `Application/Common/Interfaces/`

2. Create `IApiIntegrationService.cs`:
```csharp
namespace SecureCleanApiWaf.Application.Common.Interfaces;

/// <summary>
/// Interface for third-party API integration service
/// </summary>
public interface IApiIntegrationService
{
    Task<Result<T>> GetAllDataAsync<T>(string apiUrl);
    Task<Result<T>> GetDataByIdAsync<T>(string apiUrl, string id);
}
```

3. Update `ApiIntegrationService.cs` to implement the interface:
```csharp
namespace SecureCleanApiWaf.Services;

public class ApiIntegrationService : IApiIntegrationService
{
    // ...existing implementation...
}
```

4. Update service registration in DI:
```csharp
// Before
builder.Services.AddSingleton<ApiIntegrationService>();

// After
builder.Services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();
```

5. Update all usages to depend on interface:
```csharp
// In GetApiDataQueryHandler.cs
public class GetApiDataQueryHandler : IRequestHandler<GetApiDataQuery, Result<SampleDataDto>>
{
    private readonly IApiIntegrationService _apiService; // Changed from ApiIntegrationService
    
    public GetApiDataQueryHandler(IApiIntegrationService apiService) // Changed parameter type
    {
        _apiService = apiService;
    }
    
    // ...rest of implementation...
}
```

### **Step 1.2: Create Cache Service Interface**

1. Create `ICacheService.cs` in `Application/Common/Interfaces/`:
```csharp
namespace SecureCleanApiWaf.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
```

2. Update `SampleCache.cs` to implement interface or create new `CacheService.cs`.

### **Step 1.3: Create DateTime Abstraction**

1. Create `IDateTime.cs`:
```csharp
namespace SecureCleanApiWaf.Application.Common.Interfaces;

public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
```

2. Create implementation `DateTimeService.cs`:
```csharp
namespace SecureCleanApiWaf.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
```

3. Register in DI:
```csharp
builder.Services.AddSingleton<IDateTime, DateTimeService>();
```

### **Step 1.4: Verify Everything Still Works**

```bash
# Build the solution
dotnet build

# Run the application
dotnet run

# Test API endpoints
# Navigate to http://localhost:5006/swagger
```

### **✅ Phase 1 Checklist**
- [ ] IApiIntegrationService created and implemented
- [ ] All handlers updated to use interface
- [ ] ICacheService created (optional)
- [ ] IDateTime created (for testability)
- [ ] All services registered in DI
- [ ] Application builds without errors
- [ ] Application runs successfully
- [ ] API endpoints work correctly

---

## 📁 Phase 2: Reorganize Folders (Week 2)

**Goal**: Reorganize code within current project to follow Clean Architecture layers.

### **Step 2.1: Create Layer Folders**

1. Create folder structure:
```bash
mkdir -p Core/Domain/Entities
mkdir -p Core/Domain/ValueObjects
mkdir -p Core/Domain/Exceptions
mkdir -p Core/Application/Common/Behaviors
mkdir -p Core/Application/Common/Interfaces
mkdir -p Core/Application/Common/Models
mkdir -p Core/Application/Features
mkdir -p Infrastructure/Persistence
mkdir -p Infrastructure/Services
mkdir -p Infrastructure/Caching
mkdir -p Infrastructure/Azure
mkdir -p Presentation/Controllers
mkdir -p Presentation/Components
mkdir -p Presentation/Extensions
```

### **Step 2.2: Move Files to Appropriate Layers**

#### **Move to Core/Application**
```bash
# Move Result.cs
Move-Item Services/Result.cs Core/Application/Common/Models/

# Move pipeline behaviors
Move-Item PipelineBehaviors/CachingBehavior.cs Core/Application/Common/Behaviors/
Move-Item PipelineBehaviors/ICacheable.cs Core/Application/Common/Behaviors/

# Move Features
Move-Item Features/GetData Core/Application/Features/
```

#### **Move to Infrastructure**
```bash
# Move services
Move-Item Services/ApiIntegrationService.cs Infrastructure/Services/

# Move caching
Move-Item Caching/SampleCache.cs Infrastructure/Caching/
```

#### **Move to Presentation**
```bash
# Move controllers
Move-Item Controllers/* Presentation/Controllers/

# Move extensions
Move-Item Extensions/* Presentation/Extensions/

# Keep Components where they are (already in presentation)
```

### **Step 2.3: Update Namespaces**

Update all moved files to use new namespaces:

```csharp
// Before
namespace SecureCleanApiWaf.Services;

// After
namespace SecureCleanApiWaf.Infrastructure.Services;
```

Use Find & Replace in Visual Studio:
- Find: `namespace SecureCleanApiWaf.Services`
- Replace: `namespace SecureCleanApiWaf.Infrastructure.Services`

### **Step 2.4: Update Using Statements**

Update all files that reference moved types:

```csharp
// Add new using statements
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Infrastructure.Services;
```

### **Step 2.5: Verify Build**

```bash
dotnet clean
dotnet build
```

Fix any compilation errors related to namespaces.

### **✅ Phase 2 Checklist**
- [ ] All folders created
- [ ] Files moved to appropriate layers
- [ ] Namespaces updated
- [ ] Using statements updated
- [ ] Solution builds successfully
- [ ] No broken references

---

## 🏗️ Phase 3: Multi-Project Structure (Weeks 3-4)

**Goal**: Split into separate projects with enforced dependencies.

### **Step 3.1: Create New Solution**

```bash
# Create a new solution file
dotnet new sln -n SecureCleanApiWaf

# Backup current project
Copy-Item -Recurse SecureCleanApiWaf SecureCleanApiWaf.Backup
```

### **Step 3.2: Create Domain Project**

```bash
# Create project
dotnet new classlib -n SecureCleanApiWaf.Domain -o src/Core/SecureCleanApiWaf.Domain

# Add to solution
dotnet sln add src/Core/SecureCleanApiWaf.Domain/SecureCleanApiWaf.Domain.csproj

# Configure project
# Edit SecureCleanApiWaf.Domain.csproj:
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <!-- No external dependencies! Domain must remain pure -->
</Project>
```

### **Step 3.3: Create Application Project**

```bash
# Create project
dotnet new classlib -n SecureCleanApiWaf.Application -o src/Core/SecureCleanApiWaf.Application

# Add to solution
dotnet sln add src/Core/SecureCleanApiWaf.Application/SecureCleanApiWaf.Application.csproj

# Add reference to Domain
dotnet add src/Core/SecureCleanApiWaf.Application reference src/Core/SecureCleanApiWaf.Domain
```

Configure Application project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- MediatR for CQRS -->
    <PackageReference Include="MediatR" Version="12.2.0" />
    
    <!-- FluentValidation -->
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
    
    <!-- Caching -->
    <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="8.0.0" />
    
    <!-- Logging -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\SecureCleanApiWaf.Domain\SecureCleanApiWaf.Domain.csproj" />
  </ItemGroup>
</Project>
```

### **Step 3.4: Create Infrastructure Project**

```bash
# Create project
dotnet new classlib -n SecureCleanApiWaf.Infrastructure -o src/Infrastructure/SecureCleanApiWaf.Infrastructure

# Add to solution
dotnet sln add src/Infrastructure/SecureCleanApiWaf.Infrastructure/SecureCleanApiWaf.Infrastructure.csproj

# Add reference to Application
dotnet add src/Infrastructure/SecureCleanApiWaf.Infrastructure reference src/Core/SecureCleanApiWaf.Application
```

Configure Infrastructure project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- EF Core -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    
    <!-- Caching -->
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
    
    <!-- HTTP Client -->
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\Core\SecureCleanApiWaf.Application\SecureCleanApiWaf.Application.csproj" />
  </ItemGroup>
</Project>
```

### **Step 3.5: Create Infrastructure.Azure Project**

```bash
# Create project
dotnet new classlib -n SecureCleanApiWaf.Infrastructure.Azure -o src/Infrastructure/SecureCleanApiWaf.Infrastructure.Azure

# Add to solution
dotnet sln add src/Infrastructure/SecureCleanApiWaf.Infrastructure.Azure/SecureCleanApiWaf.Infrastructure.Azure.csproj

# Add reference to Application
dotnet add src/Infrastructure/SecureCleanApiWaf.Infrastructure.Azure reference src/Core/SecureCleanApiWaf.Application
```

Configure Azure Infrastructure project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Azure Key Vault -->
    <PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.0" />
    <PackageReference Include="Azure.Identity" Version="1.10.0" />
    <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.5.0" />
    
    <!-- Azure Blob Storage -->
    <PackageReference Include="Azure.Storage.Blobs" Version="12.19.0" />
    
    <!-- Azure Service Bus -->
    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.17.0" />
    
    <!-- Application Insights -->
    <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.21.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\Core\SecureCleanApiWaf.Application\SecureCleanApiWaf.Application.csproj" />
  </ItemGroup>
</Project>
```

### **Step 3.6: Update Web Project**

```bash
# Rename existing project folder
Rename-Item SecureCleanApiWaf src/Presentation/SecureCleanApiWaf.Web

# Update solution file to point to new location
dotnet sln remove SecureCleanApiWaf.csproj
dotnet sln add src/Presentation/SecureCleanApiWaf.Web/SecureCleanApiWaf.Web.csproj

# Add project references
dotnet add src/Presentation/SecureCleanApiWaf.Web reference src/Core/SecureCleanApiWaf.Application
dotnet add src/Presentation/SecureCleanApiWaf.Web reference src/Infrastructure/SecureCleanApiWaf.Infrastructure
dotnet add src/Presentation/SecureCleanApiWaf.Web reference src/Infrastructure/SecureCleanApiWaf.Infrastructure.Azure
```

### **Step 3.7: Move Code to Appropriate Projects**

#### **Move to Domain Project**
```bash
# If you have domain entities (currently you don't have pure domain entities)
# Create sample entities in Domain project
```

Example Domain Entity:
```csharp
// src/Core/SecureCleanApiWaf.Domain/Entities/SampleEntity.cs
namespace SecureCleanApiWaf.Domain.Entities;

public class SampleEntity : BaseEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    
    private SampleEntity() { } // For EF Core
    
    public static SampleEntity Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty");
            
        return new SampleEntity
        {
            Name = name,
            Description = description,
            IsActive = true
        };
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
}
```

#### **Move to Application Project**
```bash
# Copy from old project Core/Application folders
Copy-Item -Recurse Core/Application/* src/Core/SecureCleanApiWaf.Application/

# Update namespaces to SecureCleanApiWaf.Application.*
```

#### **Move to Infrastructure Project**
```bash
# Copy services and caching
Copy-Item -Recurse Infrastructure/Services/* src/Infrastructure/SecureCleanApiWaf.Infrastructure/Services/
Copy-Item -Recurse Infrastructure/Caching/* src/Infrastructure/SecureCleanApiWaf.Infrastructure/Caching/

# Update namespaces to SecureCleanApiWaf.Infrastructure.*
```

#### **Move Azure-specific code**
```bash
# Extract Azure Key Vault configuration from Program.cs
# Create KeyVaultExtensions in Infrastructure.Azure
```

### **Step 3.8: Create DependencyInjection Extension Methods**

#### **Application/DependencyInjection.cs**
```csharp
namespace SecureCleanApiWaf.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}
```

#### **Infrastructure/DependencyInjection.cs**
```csharp
namespace SecureCleanApiWaf.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Services
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();
        services.AddSingleton<ICacheService, CacheService>();
        
        // HttpClient
        services.AddHttpClient("ThirdPartyApiClient", client =>
        {
            client.BaseAddress = new Uri(configuration["ThirdPartyApi:BaseUrl"]);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Caching
        services.AddDistributedMemoryCache();
        
        return services;
    }
}
```

#### **Infrastructure.Azure/DependencyInjection.cs**
```csharp
namespace SecureCleanApiWaf.Infrastructure.Azure;

public static class DependencyInjection
{
    public static IServiceCollection AddAzureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Application Insights
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
        });
        
        return services;
    }
}
```

### **Step 3.9: Update Program.cs**

```csharp
using SecureCleanApiWaf.Application;
using SecureCleanApiWaf.Infrastructure;
using SecureCleanApiWaf.Infrastructure.Azure;
using SecureCleanApiWaf.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add layer services (Clean Architecture)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAzureInfrastructure(builder.Configuration);

// Add Blazor and API services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
```

### **Step 3.10: Build and Test**

```bash
# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Build entire solution
dotnet build

# Run application
cd src/Presentation/SecureCleanApiWaf.Web
dotnet run
```

Fix any compilation errors related to:
- Missing using statements
- Incorrect project references
- Namespace mismatches

### **✅ Phase 3 Checklist**
- [ ] All projects created
- [ ] Project references configured correctly
- [ ] NuGet packages added
- [ ] Code moved to appropriate projects
- [ ] Namespaces updated
- [ ] DependencyInjection extensions created
- [ ] Program.cs updated
- [ ] Solution builds successfully
- [ ] Application runs correctly
- [ ] All features work as before

---

## 🧪 Phase 4: Add Tests (Ongoing)

**Goal**: Achieve comprehensive test coverage across all layers.

### **Step 4.1: Create Test Projects**

```bash
# Domain Unit Tests
dotnet new xunit -n SecureCleanApiWaf.Domain.UnitTests -o tests/SecureCleanApiWaf.Domain.UnitTests
dotnet add tests/SecureCleanApiWaf.Domain.UnitTests reference src/Core/SecureCleanApiWaf.Domain
dotnet sln add tests/SecureCleanApiWaf.Domain.UnitTests

# Application Unit Tests
dotnet new xunit -n SecureCleanApiWaf.Application.UnitTests -o tests/SecureCleanApiWaf.Application.UnitTests
dotnet add tests/SecureCleanApiWaf.Application.UnitTests reference src/Core/SecureCleanApiWaf.Application
dotnet sln add tests/SecureCleanApiWaf.Application.UnitTests

# Infrastructure Integration Tests
dotnet new xunit -n SecureCleanApiWaf.Infrastructure.IntegrationTests -o tests/SecureCleanApiWaf.Infrastructure.IntegrationTests
dotnet add tests/SecureCleanApiWaf.Infrastructure.IntegrationTests reference src/Infrastructure/SecureCleanApiWaf.Infrastructure
dotnet sln add tests/SecureCleanApiWaf.Infrastructure.IntegrationTests

# Web Functional Tests
dotnet new xunit -n SecureCleanApiWaf.Web.FunctionalTests -o tests/SecureCleanApiWaf.Web.FunctionalTests
dotnet add tests/SecureCleanApiWaf.Web.FunctionalTests reference src/Presentation/SecureCleanApiWaf.Web
dotnet sln add tests/SecureCleanApiWaf.Web.FunctionalTests
```

### **Step 4.2: Add Testing Packages**

Add to all test projects:

```bash
# Add common testing packages
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.NET.Test.Sdk
```

Add to Web.FunctionalTests:

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

Add to Infrastructure.IntegrationTests:

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

### **Step 4.3: Write Tests**

See [Testing Strategy](docs/CleanArchitecture/Projects/06-Testing-Strategy.md) for detailed examples.

### **✅ Phase 4 Checklist**
- [ ] All test projects created
- [ ] Testing packages installed
- [ ] Domain unit tests added
- [ ] Application unit tests added
- [ ] Infrastructure integration tests added
- [ ] Web functional tests added
- [ ] All tests pass
- [ ] Code coverage > 70%

---

## 🔧 Common Issues & Solutions

### **Issue 1: Circular Dependencies**

**Problem**: Project A references B, and B references A.

**Solution**: One project is in the wrong layer. Review dependency rules:
- Domain ? Nothing
- Application ? Domain
- Infrastructure ? Application
- Presentation ? All

### **Issue 2: Namespace Conflicts**

**Problem**: `The type 'X' exists in both 'ProjectA' and 'ProjectB'`.

**Solution**: Ensure unique namespaces per project:
- Domain: `SecureCleanApiWaf.Domain.*`
- Application: `SecureCleanApiWaf.Application.*`
- Infrastructure: `SecureCleanApiWaf.Infrastructure.*`

### **Issue 3: Missing Using Statements**

**Problem**: Build errors about types not found.

**Solution**: Add appropriate using statements:
```csharp
using SecureCleanApiWaf.Application.Common.Interfaces;
using SecureCleanApiWaf.Application.Common.Models;
```

### **Issue 4: DI Registration Missing**

**Problem**: `No service for type 'IMyService' has been registered`.

**Solution**: Ensure service is registered in DependencyInjection extension method.

---

## ✅ Migration Checklist Summary

### **Phase 1: Interfaces**
- [ ] IApiIntegrationService extracted
- [ ] ICacheService extracted
- [ ] IDateTime created
- [ ] All services use interfaces
- [ ] Application builds and runs

### **Phase 2: Folder Reorganization**
- [ ] Layer folders created
- [ ] Files moved to correct folders
- [ ] Namespaces updated
- [ ] Application builds and runs

### **Phase 3: Multi-Project**
- [ ] Domain project created
- [ ] Application project created
- [ ] Infrastructure projects created
- [ ] Web project updated
- [ ] Code moved to projects
- [ ] DependencyInjection extensions created
- [ ] Application builds and runs

### **Phase 4: Testing**
- [ ] Test projects created
- [ ] Unit tests added
- [ ] Integration tests added
- [ ] Functional tests added
- [ ] All tests pass

---

## 💡 Tips for Success

1. **Take it slow** - Complete one phase before moving to the next
2. **Commit frequently** - Commit after each successful step
3. **Run tests** - Verify application works after each change
4. **Ask for help** - Use GitHub issues if you get stuck
5. **Document changes** - Update README as you go

---

## 🚀 Next Steps

After completing migration:

1. **Add more tests** - Aim for >80% code coverage
2. **Implement new features** - Use Clean Architecture patterns
3. **Refine domain model** - Move more logic into Domain layer
4. **Add architecture tests** - Enforce layer dependencies automatically
5. **Share your work** - Update portfolio with Clean Architecture implementation

---

## 🆘 Contact

**Need Help?**

- 📖 **Documentation:** Start with the deployment guides above
- 🐛 **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
- 📧 **Email:** softevolutionsl@gmail.com
- 🐙 **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)


