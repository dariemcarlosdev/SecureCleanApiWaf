# ?? Layer Integration Guide - CleanArchitecture.ApiTemplate

**Clean Architecture + DDD Hybrid Approach**

> *"In Clean Architecture, the integration between layers happens through abstractions, dependency injection, and clear contracts."*

---

## 📑 Table of Contents

1. [Overview](#-overview)
2. [Integration Architecture](#-integration-architecture)
3. [Layer Integration Points](#-layer-integration-points)
   - [Domain ? Application](#1-domain--application)
   - [Application ? Infrastructure](#2-application--infrastructure)
   - [Application ? Presentation](#3-application--presentation)
   - [Infrastructure ? Presentation](#4-infrastructure--presentation)
4. [Cross-Layer Integration Patterns](#-cross-layer-integration-patterns)
5. [Dependency Injection Flow](#-dependency-injection-flow)
6. [Request Flow Examples](#-request-flow-examples)
7. [Key Integration Files](#-key-integration-files)
8. [Anti-Patterns to Avoid](#-anti-patterns-to-avoid)
9. [Reference Documentation](#-reference-documentation)

---

## 📖 Overview

CleanArchitecture.ApiTemplate implements **Clean Architecture with Domain-Driven Design (DDD)** in a single-project structure. This guide explains:

- **HOW** layers integrate with each other
- **WHY** specific integration patterns are used
- **WHAT** files contain the integration logic

### **Integration Principles**

? **Dependencies flow inward** - Outer layers depend on inner layers, never the reverse  
? **Abstractions over implementations** - Layers communicate through interfaces  
✅ **Dependency Injection** - Runtime wiring of concrete implementations  
✅ **Single Responsibility** - Each integration point has one clear purpose  

---

## ??? Integration Architecture

### **Visual Integration Map**

```
+------------------------------------------------------------+
�                    PRESENTATION LAYER                      �
�  � API Controllers (AuthController, SampleController)      �
�  � Blazor Components (Home.razor, Layout)                  �
�  � Middleware (JwtBlacklistValidationMiddleware)           �
�  � DI Configuration (Program.cs, Extensions)               �
�                                                            �
�  Integration: Sends Commands/Queries ? MediatR            �
�               Registers Services ? DI Container            �
+------------------------------------------------------------+
                        �
                        � ? Dependency Injection
                        � ? MediatR Request Dispatch
                        � ? Service Resolution
                        ?
+------------------------------------------------------------+
�      INFRASTRUCTURE LAYER         �  INFRASTRUCTURE.AZURE  �
�  � ApiIntegrationService          �  � KeyVaultService     �
�  � TokenBlacklistService          �  � BlobStorageService  �
�  � CacheService                   �  � ServiceBusService   �
�  � JwtTokenGenerator              �                        �
�                                   �                        �
�  Integration: Implements ? Application Interfaces         �
�               Uses ? Domain Entities/Value Objects        �
+------------------------------------------------------------+
                        �
                        � ? Interface Implementation
                        � ? Domain Entity Usage
                        � ? External Service Calls
                        ?
+------------------------------------------------------------+
�                   APPLICATION LAYER                        �
�  � CQRS Commands (LoginUserCommand, BlacklistTokenCommand)�
�  � CQRS Queries (IsTokenBlacklistedQuery, GetApiDataQuery)�
�  � Handlers (LoginUserCommandHandler, etc.)                �
�  � Pipeline Behaviors (Caching, Logging, Validation)      �
�  � Interface Definitions (IApiIntegrationService, etc.)    �
�                                                            �
�  Integration: Orchestrates ? Domain Logic                 �
�               Defines ? Infrastructure Contracts          �
�               Uses ? MediatR Pipeline                     �
+------------------------------------------------------------+
                        �
                        � ? Entity Creation/Manipulation
                        � ? Value Object Validation
                        � ? Business Rule Enforcement
                        ?
+------------------------------------------------------------+
�                     DOMAIN LAYER                           �
�  � Entities (User, Token, ApiDataItem)                     �
�  � Value Objects (Email, Role)                             �
�  � Enums (UserStatus, TokenStatus, TokenType, DataStatus)  �
�  � Domain Exceptions (DomainException)                     �
�                                                            �
�  Integration: NONE - Pure business logic, no dependencies �
+------------------------------------------------------------+
```

---

## 🔗 Layer Integration Points

### **1. Domain ? Application**

**Integration Type:** Direct Usage (No Abstractions Needed)

#### **How They Integrate**

Application layer **directly uses** Domain entities, value objects, and exceptions:

```csharp
// Application Layer Handler
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(...)
    {
        // ? Direct use of Domain Value Object
        var email = Email.Create(request.Email);
        
        if (email.IsFailure)
            return Result<LoginResponse>.Fail(email.Error);
        
        // ? Direct use of Domain Entity
        var user = User.Create(
            username: request.Username,
            email: email.Value,
            passwordHash: hashedPassword
        );
        
        // ? Direct use of Domain Enum
        user.AssignRole(Role.User);
        
        // Business logic orchestration...
    }
}
```

#### **Why This Pattern**

- ? Application layer **orchestrates** domain logic
- ? Domain entities **enforce** business rules
- ? No abstractions needed - Domain is pure, no dependencies
- ? Type safety via value objects (Email vs string)

#### **Key Files**

| Domain Files | Application Files | Purpose |
|--------------|-------------------|---------|
| `Core/Domain/Entities/User.cs` | `Core/Application/Features/Authentication/Commands/LoginUserCommand.cs` | User entity creation and manipulation |
| `Core/Domain/ValueObjects/Email.cs` | All handlers using email validation | Email validation and type safety |
| `Core/Domain/ValueObjects/Role.cs` | Authentication handlers | Role assignment and authorization |
| `Core/Domain/Enums/TokenStatus.cs` | Token management handlers | Token lifecycle management |

?? **Detailed Docs:**
- [Domain Layer Guide](Projects/01-Domain-Layer.md) - Entity and value object patterns
- [Application Layer Guide](Projects/02-Application-Layer.md) - Handler implementations

---

### **2. Application ? Infrastructure**

**Integration Type:** Interface Abstraction + Dependency Injection

#### **How They Integrate**

Application defines **interfaces** (contracts), Infrastructure provides **implementations**:

```csharp
// ????????????????????????????????????????????????????????????
// APPLICATION LAYER - Defines the contract
// ????????????????????????????????????????????????????????????

// Core/Application/Common/Interfaces/IApiIntegrationService.cs
public interface IApiIntegrationService
{
    Task<Result<T>> GetAllDataAsync<T>(string apiUrl);
    Task<Result<T>> GetDataByIdAsync<T>(string apiUrl, string id);
}

// ????????????????????????????????????????????????????????????
// INFRASTRUCTURE LAYER - Implements the contract
// ????????????????????????????????????????????????????????????

// Infrastructure/Services/ApiIntegrationService.cs
public class ApiIntegrationService : IApiIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiIntegrationService> _logger;
    
    public ApiIntegrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiIntegrationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task<Result<T>> GetAllDataAsync<T>(string apiUrl)
    {
        var client = _httpClientFactory.CreateClient("ThirdPartyApiClient");
        // Implementation with Polly resilience, logging, etc.
    }
}

// ????????????????????????????????????????????????????????????
// APPLICATION LAYER - Handler uses the interface
// ????????????????????????????????????????????????????????????

// Core/Application/Features/SampleData/Queries/GetApiDataQueryHandler.cs
public class GetApiDataQueryHandler : IRequestHandler<GetApiDataQuery, Result<List<SampleDataDto>>>
{
    private readonly IApiIntegrationService _apiService; // ? Depends on interface
    
    public GetApiDataQueryHandler(IApiIntegrationService apiService)
    {
        _apiService = apiService; // ? Injected at runtime
    }
    
    public async Task<Result<List<SampleDataDto>>> Handle(...)
    {
        // Uses abstraction, doesn't know about HttpClient, Polly, etc.
        var result = await _apiService.GetAllDataAsync<List<SampleDataDto>>("api/data");
        return result;
    }
}

// ????????????????????????????????????????????????????????????
// PRESENTATION LAYER - Dependency Injection wiring
// ????????????????????????????????????????????????????????????

// Presentation/Extensions/DependencyInjection/InfrastructureServiceExtensions.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    // ? Register interface ? implementation mapping
    services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
    
    return services;
}
```

#### **Why This Pattern**

- ? **Dependency Inversion** - Application doesn't depend on Infrastructure code
- ✅ **Testability** - Easy to mock interfaces in unit tests
- ✅ **Flexibility** - Swap implementations without changing handlers
- ? **Clean Architecture Compliance** - Proper dependency flow

#### **Key Interfaces**

| Interface | Implementation | Purpose |
|-----------|----------------|---------|
| `IApiIntegrationService` | `ApiIntegrationService` | External API calls with resilience |
| `ICacheService` | `CacheService` | Distributed caching abstraction |
| `ITokenBlacklistService` | `TokenBlacklistService` | Token blacklist management |
| `IJwtTokenGenerator` | `JwtTokenGenerator` | JWT creation and validation |

?? **Detailed Docs:**
- [Interface Abstractions Summary](INTERFACE_ABSTRACTIONS_SUMMARY.md) - All interfaces explained
- [Infrastructure Layer Guide](Projects/03-Infrastructure-Layer.md) - Service implementations

---

### **3. Application ? Presentation**

**Integration Type:** MediatR Request Dispatch

#### **How They Integrate**

Presentation layer sends **Commands/Queries** to Application via MediatR:

```csharp
// ????????????????????????????????????????????????????????????
// APPLICATION LAYER - Command Definition
// ????????????????????????????????????????????????????????????

// Core/Application/Features/Authentication/Commands/LoginUserCommand.cs
public record LoginUserCommand(
    string Username,
    string Password,
    string Role
) : IRequest<Result<LoginResponse>>; // ? MediatR request

// Handler
public class LoginUserCommandHandler 
    : IRequestHandler<LoginUserCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        // Business logic here...
    }
}

// ????????????????????????????????????????????????????????????
// PRESENTATION LAYER - Controller sends command via MediatR
// ????????????????????????????????????????????????????????????

// Presentation/Controllers/v1/AuthController.cs
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator; // ? Only dependency
    
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // ? Send command to MediatR, which routes to handler
        var command = new LoginUserCommand(
            request.Username,
            request.Password,
            request.Role
        );
        
        var result = await _mediator.Send(command);
        
        // Handle result
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        
        return Ok(result.Data);
    }
}
```

#### **Why This Pattern**

- ? **Thin Controllers** - Controllers only handle HTTP concerns
- ? **Business logic Isolation** - All logic in handlers
- ? **Automatic Behaviors** - Logging, caching, validation via pipeline
- ✅ **Testability** - Test handlers independently of HTTP

#### **MediatR Pipeline Flow**

```
Controller
  ? Send(command)
MediatR Dispatcher
  ?
LoggingBehavior (logs request)
  ?
ValidationBehavior (validates with FluentValidation)
  ?
CachingBehavior (checks cache if ICacheable)
  ?
Command/Query Handler (executes business logic)
  ?
Response (back through pipeline)
  ?
Controller (HTTP response)
```

?? **Detailed Docs:**
- [Application Layer Guide](Projects/02-Application-Layer.md) - CQRS pattern and handlers
- [Web/Presentation Layer Guide](Projects/05-Web-Presentation-Layer.md) - Controller patterns

---

### **4. Infrastructure ? Presentation**

**Integration Type:** Middleware + Configuration

#### **How They Integrate**

Infrastructure components (like middleware) are **registered and configured** in Presentation:

```csharp
// ????????????????????????????????????????????????????????????
// INFRASTRUCTURE LAYER - Middleware Implementation
// ????????????????????????????????????????????????????????????

// Infrastructure/Middleware/JwtBlacklistValidationMiddleware.cs
public class JwtBlacklistValidationMiddleware
{
    private readonly RequestDelegate _next;
    
    public JwtBlacklistValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(
        HttpContext context,
        IMediator mediator) // ? Uses MediatR to query blacklist
    {
        // Extract JWT from Authorization header
        var token = ExtractToken(context);
        
        if (!string.IsNullOrEmpty(token))
        {
            // ? Query Application layer via MediatR
            var query = new IsTokenBlacklistedQuery(token);
            var result = await mediator.Send(query);
            
            if (result.Data) // Token is blacklisted
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token has been revoked");
                return;
            }
        }
        
        await _next(context);
    }
}

// ????????????????????????????????????????????????????????????
// PRESENTATION LAYER - Middleware Registration
// ????????????????????????????????????????????????????????????

// Presentation/Extensions/HttpPipeline/WebApplicationExtensions.cs
public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // ? Register middleware in pipeline
        app.UseMiddleware<JwtBlacklistValidationMiddleware>();
        
        app.UseAuthentication(); // After blacklist check
        app.UseAuthorization();
        
        return app;
    }
}

// Program.cs
var app = builder.Build();

app.ConfigureMiddleware(); // ? Applies middleware
```

#### **Why This Pattern**

- ? **HTTP Pipeline Integration** - Middleware runs on every request
- ? **Cross-Cutting Concerns** - Security, logging, error handling
- ? **Reusability** - Middleware can call Application layer via MediatR
- ? **Configuration Centralization** - All setup in Program.cs/Extensions

?? **Detailed Docs:**
- [API Security Implementation Guide](../AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md) - Middleware security patterns
- [JWT Authentication CQRS Architecture](../AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md) - Middleware integration with CQRS

---

## 🤝 Cross-Layer Integration Patterns

### **Pattern 1: CQRS Authentication Flow**

**Complete integration across all layers:**

```
+-------------------------------------------------------------+
� 1. PRESENTATION - API Request                               �
�    POST /api/v1/auth/login                                  �
�    Body: { username, password, role }                       �
+-------------------------------------------------------------+
             �
             � AuthController dispatches command
             ?
+-------------------------------------------------------------+
� 2. APPLICATION - MediatR Command                            �
�    LoginUserCommand ? LoginUserCommandHandler               �
�    � Validates credentials                                  �
�    � Creates User entity (Domain)                           �
�    � Generates Token entity (Domain)                        �
+-------------------------------------------------------------+
             �
             � Handler calls Infrastructure service
             ?
+-------------------------------------------------------------+
� 3. INFRASTRUCTURE - Token Generation                        �
�    IJwtTokenGenerator.GenerateToken()                       �
�    � Creates JWT with user claims                           �
�    � Signs with secret key                                  �
�    � Returns token string                                   �
+-------------------------------------------------------------+
             �
             � Response flows back
             ?
+-------------------------------------------------------------+
� 4. APPLICATION - Handler returns Result<LoginResponse>      �
�    { token, expiresAt, username, role }                     �
+-------------------------------------------------------------+
             �
             � Controller transforms to HTTP response
             ?
+-------------------------------------------------------------+
� 5. PRESENTATION - HTTP 200 OK                               �
�    Response: { token: "eyJ...", expiresAt: "2025-..." }    �
+-------------------------------------------------------------+
```

**Files Involved:**
1. `Presentation/Controllers/v1/AuthController.cs` - HTTP endpoint
2. `Core/Application/Features/Authentication/Commands/LoginUserCommand.cs` - CQRS command
3. `Core/Domain/Entities/User.cs` - User entity validation
4. `Core/Domain/Entities/Token.cs` - Token entity creation
5. `Infrastructure/Security/JwtTokenGenerator.cs` - Token generation

?? **Detailed Docs:**
- [CQRS Login Implementation](../AuthenticationAuthorization/CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md)

---

### **Pattern 2: Token Blacklisting Flow**

**Integration with dual-cache strategy:**

```
+-------------------------------------------------------------+
� 1. PRESENTATION - Logout Request                            �
�    POST /api/v1/auth/logout                                 �
�    Headers: Authorization: Bearer <token>                   �
+-------------------------------------------------------------+
             �
             � Controller dispatches command
             ?
+-------------------------------------------------------------+
� 2. APPLICATION - MediatR Command                            �
�    BlacklistTokenCommand ? BlacklistTokenCommandHandler     �
�    � Validates token format                                 �
�    � Creates Token entity                                   �
�    � Marks as revoked                                       �
+-------------------------------------------------------------+
             �
             � Handler calls Infrastructure service
             ?
+-------------------------------------------------------------+
� 3. INFRASTRUCTURE - Dual-Cache Storage                      �
�    ITokenBlacklistService.BlacklistTokenAsync()             �
�    � Stores in Memory Cache (fast)                          �
�    � Stores in Distributed Cache (persistent)               �
�    � Sets expiration = token lifetime                       �
+-------------------------------------------------------------+
             �
             � Every subsequent request with that token
             ?
+-------------------------------------------------------------+
� 4. INFRASTRUCTURE - Middleware Validation                   �
�    JwtBlacklistValidationMiddleware                         �
�    � Extracts token from header                             �
�    � Queries: IsTokenBlacklistedQuery (MediatR)             �
�    � Checks dual cache (Memory ? Distributed)               �
�    � Returns 401 if blacklisted                             �
+-------------------------------------------------------------+
```

**Files Involved:**
1. `Presentation/Controllers/v1/AuthController.cs` - Logout endpoint
2. `Core/Application/Features/Authentication/Commands/BlacklistTokenCommand.cs` - Blacklist command
3. `Core/Application/Features/Authentication/Queries/IsTokenBlacklistedQuery.cs` - Validation query
4. `Infrastructure/Services/TokenBlacklistService.cs` - Dual-cache implementation
5. `Infrastructure/Middleware/JwtBlacklistValidationMiddleware.cs` - HTTP pipeline validation

?? **Detailed Docs:**
- [CQRS Logout Implementation](../AuthenticationAuthorization/CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md)

---

### **Pattern 3: API Data Retrieval with Caching**

**Integration with MediatR pipeline caching:**

```
+-------------------------------------------------------------+
� 1. PRESENTATION - API Request                               �
�    GET /api/v1/sample/data                                  �
+-------------------------------------------------------------+
             �
             � Controller dispatches query
             ?
+-------------------------------------------------------------+
� 2. APPLICATION - MediatR Pipeline                           �
�    GetApiDataQuery (implements ICacheable)                  �
�      ?                                                       �
�    CachingBehavior checks cache                             �
�      ?                                                       �
�    Cache MISS ? Handler executes                            �
�      ?                                                       �
�    GetApiDataQueryHandler                                   �
+-------------------------------------------------------------+
             �
             � Handler calls Infrastructure service
             ?
+-------------------------------------------------------------+
� 3. INFRASTRUCTURE - API Integration                         �
�    IApiIntegrationService.GetAllDataAsync()                 �
�    � HttpClientFactory creates client                       �
�    � ApiKeyHandler adds API key                             �
�    � Polly retries on failure                               �
�    � Returns Result<List<SampleDataDto>>                    �
+-------------------------------------------------------------+
             �
             � Response flows back
             ?
+-------------------------------------------------------------+
� 4. APPLICATION - CachingBehavior stores result              �
�    � Caches response for 5 minutes                          �
�    � Next request = Cache HIT (no API call)                 �
+-------------------------------------------------------------+
             �
             � Controller returns HTTP response
             ?
+-------------------------------------------------------------+
� 5. PRESENTATION - HTTP 200 OK                               �
�    Response: [{ id, name, description }]                    �
+-------------------------------------------------------------+
```

**Files Involved:**
1. `Presentation/Controllers/v1/SampleController.cs` - API endpoint
2. `Core/Application/Features/SampleData/Queries/GetApiDataQuery.cs` - Query with ICacheable
3. `Core/Application/PipelineBehaviors/CachingBehavior.cs` - Automatic caching
4. `Infrastructure/Services/ApiIntegrationService.cs` - External API calls
5. `Infrastructure/Handlers/ApiKeyHandler.cs` - API key injection

?? **Detailed Docs:**
- [Application Layer Guide](Projects/02-Application-Layer.md) - Pipeline behaviors
- [API Design Guide](../APIDesign/API_DESIGN_GUIDE.md) - Caching strategies

---

## 🔄 Dependency Injection Flow

**Registration happens in Presentation, resolution happens at runtime:**

```csharp
// ????????????????????????????????????????????????????????????
// Program.cs - Orchestrates all DI registrations
// ????????????????????????????????????????????????????????????

var builder = WebApplication.CreateBuilder(args);

// 1. Application Layer Services (MediatR, Behaviors, Validators)
builder.Services.AddApplicationServices();

// 2. Infrastructure Layer Services (API, Cache, Auth)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Infrastructure.Azure Services (Key Vault, Blob Storage)
builder.Services.AddAzureInfrastructureServices(builder.Configuration);

// 4. Presentation Layer Services (Controllers, Swagger, JWT)
builder.Services.AddPresentationServices(builder.Configuration);

// ????????????????????????????????????????????????????????????
// Extension Methods - Layer-specific registrations
// ????????????????????????????????????????????????????????????

// Application Services
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<GetApiDataQuery>();
        
        // Pipeline behaviors (order matters!)
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    });
    
    services.AddValidatorsFromAssemblyContaining<LoginUserCommandValidator>();
    
    return services;
}

// Infrastructure Services
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ? Interface ? Implementation mappings
    services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
    services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    
    // HttpClient with Polly policies
    services.AddHttpClient<IApiIntegrationService, ApiIntegrationService>(client =>
    {
        client.BaseAddress = new Uri(configuration["ThirdPartyApi:BaseUrl"]);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ApiKeyHandler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    // Distributed caching
    services.AddDistributedMemoryCache(); // Replace with Redis in production
    
    return services;
}
```

**DI Container Resolution Flow:**

```
Controller Constructor
  ? (DI resolves IMediator)
MediatR Instance
  ? (MediatR resolves handler)
Command/Query Handler Constructor
  ? (DI resolves IApiIntegrationService)
ApiIntegrationService Instance
  ? (DI resolves IHttpClientFactory)
HttpClient with configured policies
```

?? **Detailed Docs:**
- [Web/Presentation Layer Guide](Projects/05-Web-Presentation-Layer.md) - DI configuration patterns

---

## 🔄 Request Flow Examples

### **Example 1: Protected API Endpoint**

```
1. HTTP Request arrives: GET /api/v1/sample/data
   Headers: Authorization: Bearer eyJ...
   
2. JwtBlacklistValidationMiddleware
   � Extracts token
   � Sends IsTokenBlacklistedQuery via MediatR
   � CachingBehavior checks cache (fast lookup)
   � Validates token not blacklisted
   
3. UseAuthentication() middleware
   � Validates JWT signature
   � Populates ClaimsPrincipal
   
4. UseAuthorization() middleware
   � Checks [Authorize(Roles = "User,Admin")]
   � Allows request to proceed
   
5. Controller receives request
   � SampleController.GetAllData()
   � Dispatches GetApiDataQuery via MediatR
   
6. MediatR Pipeline
   � LoggingBehavior logs request
   � CachingBehavior checks cache
   � Cache MISS ? Handler executes
   
7. GetApiDataQueryHandler
   � Calls IApiIntegrationService
   � Service uses HttpClientFactory
   � ApiKeyHandler adds API key
   � Polly retries on failure
   
8. Response flows back
   � Handler returns Result<List<SampleDataDto>>
   � CachingBehavior caches response
   � Controller returns HTTP 200 OK
```

### **Example 2: User Logout**

```
1. HTTP Request arrives: POST /api/v1/auth/logout
   Headers: Authorization: Bearer eyJ...
   
2. Middleware validates token (not yet blacklisted)
   
3. AuthController.Logout()
   � Extracts token from header
   � Creates BlacklistTokenCommand
   � Dispatches via MediatR
   
4. BlacklistTokenCommandHandler
   � Validates token format
   � Creates Token entity (Domain)
   � Marks Status = Revoked
   � Calls ITokenBlacklistService
   
5. TokenBlacklistService
   � Stores in Memory Cache (fast)
   � Stores in Distributed Cache (persistent)
   � Sets expiration = token lifetime
   
6. Response: HTTP 200 OK
   { message: "Logout successful" }
   
7. Subsequent requests with that token
   � JwtBlacklistValidationMiddleware intercepts
   � IsTokenBlacklistedQuery returns true
   � HTTP 401 Unauthorized (token revoked)
```

---

## 📁 Key Integration Files

### **Dependency Injection Configuration**

| File | Purpose | Layer Integration |
|------|---------|-------------------|
| `Program.cs` | Master DI orchestration | All layers |
| `Presentation/Extensions/DependencyInjection/InfrastructureServiceExtensions.cs` | Infrastructure services registration | Infrastructure ? Application |
| `Presentation/Extensions/DependencyInjection/PresentationServiceExtensions.cs` | Presentation services (JWT, Swagger) | Presentation ? Application |

### **MediatR Integration**

| File | Purpose | Layer Integration |
|------|---------|-------------------|
| `Core/Application/Features/Authentication/Commands/LoginUserCommand.cs` | CQRS command | Application ? Domain ? Infrastructure |
| `Core/Application/PipelineBehaviors/CachingBehavior.cs` | Automatic caching | Application ? Infrastructure |
| `Infrastructure/Middleware/JwtBlacklistValidationMiddleware.cs` | Middleware using MediatR | Infrastructure ? Application |

### **Interface Abstractions**

| File | Purpose | Layer Integration |
|------|---------|-------------------|
| `Core/Application/Common/Interfaces/IApiIntegrationService.cs` | API service contract | Application ? Infrastructure |
| `Core/Application/Common/Interfaces/ICacheService.cs` | Cache service contract | Application ? Infrastructure |
| `Core/Application/Common/Interfaces/ITokenBlacklistService.cs` | Blacklist service contract | Application ? Infrastructure |

---

## ❌ Anti-Patterns to Avoid

### **? Domain Referencing Outer Layers**

```csharp
// ? WRONG - Domain entity using Infrastructure
namespace CleanArchitecture.ApiTemplate.Core.Domain.Entities;

using CleanArchitecture.ApiTemplate.Infrastructure.Services; // ? NEVER DO THIS

public class User : BaseEntity
{
    private readonly ApiIntegrationService _apiService; // ? WRONG
}
```

**✅ CORRECT:**
Domain entities should have zero dependencies. All external calls go through Application handlers.

---

### **? Application Using Concrete Infrastructure Classes**

```csharp
// ? WRONG - Handler using concrete implementation
namespace CleanArchitecture.ApiTemplate.Core.Application.Features;

using CleanArchitecture.ApiTemplate.Infrastructure.Services; // ? WRONG

public class GetApiDataQueryHandler
{
    private readonly ApiIntegrationService _apiService; // ? Should be IApiIntegrationService
}
```

**✅ CORRECT:**
Always depend on interfaces defined in Application layer.

---

### **? Presentation Calling Infrastructure Directly**

```csharp
// ? WRONG - Controller bypassing Application layer
[ApiController]
public class SampleController : ControllerBase
{
    private readonly ApiIntegrationService _apiService; // ? WRONG
    
    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        var data = await _apiService.GetAllDataAsync(...); // ? WRONG
        return Ok(data);
    }
}
```

**✅ CORRECT:**
Controllers should only send MediatR commands/queries.

---

### **? Skipping MediatR for Cross-Layer Calls**

```csharp
// ? WRONG - Middleware calling service directly
public class JwtBlacklistValidationMiddleware
{
    public async Task InvokeAsync(
        HttpContext context,
        ITokenBlacklistService blacklistService) // ? WRONG
    {
        var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(token); // ? WRONG
    }
}
```

**✅ CORRECT:**
Use MediatR to maintain consistent flow through Application layer.

```csharp
// ? CORRECT
public async Task InvokeAsync(
    HttpContext context,
    IMediator mediator) // ? CORRECT
{
    var query = new IsTokenBlacklistedQuery(token);
    var result = await mediator.Send(query); // ? CORRECT
}
```

---

## 📚 Reference Documentation

### **Layer-Specific Guides**

- **[01-Domain-Layer.md](Projects/01-Domain-Layer.md)** - Entities, value objects, domain logic
- **[02-Application-Layer.md](Projects/02-Application-Layer.md)** - CQRS, handlers, pipeline behaviors
- **[03-Infrastructure-Layer.md](Projects/03-Infrastructure-Layer.md)** - Services, caching, external APIs
- **[04-Infrastructure-Azure-Layer.md](Projects/04-Infrastructure-Azure-Layer.md)** - Azure Key Vault, Blob Storage
- **[05-Web-Presentation-Layer.md](Projects/05-Web-Presentation-Layer.md)** - Controllers, middleware, DI

### **Pattern-Specific Guides**

- **[INTERFACE_ABSTRACTIONS_SUMMARY.md](INTERFACE_ABSTRACTIONS_SUMMARY.md)** - All interfaces explained
- **[CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md](../AuthenticationAuthorization/CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md)** - Login flow integration
- **[CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md](../AuthenticationAuthorization/CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md)** - Logout flow integration
- **[JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](../AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)** - Complete authentication architecture

### **Architecture Guides**

- **[CLEAN_ARCHITECTURE_GUIDE.md](CLEAN_ARCHITECTURE_GUIDE.md)** - Master architecture guide
- **[ARCHITECTURE_PATTERNS_EXPLAINED.md](ARCHITECTURE_PATTERNS_EXPLAINED.md)** - Clean Architecture + DDD patterns
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Step-by-step layer extraction

---

## 📝 Summary

### **Integration Principles**

1. **Domain Layer** - Integrates with nothing (pure business logic)
2. **Application Layer** - Uses Domain directly, defines Infrastructure contracts
3. **Infrastructure Layer** - Implements Application interfaces, uses Domain entities
4. **Presentation Layer** - Orchestrates all layers via DI, sends MediatR requests

### **Key Integration Mechanisms**

- **Interface Abstractions** - Application defines, Infrastructure implements
- **Dependency Injection** - Runtime wiring in Presentation layer
- **MediatR Dispatch** - Controllers send commands/queries to Application
- **Pipeline Behaviors** - Cross-cutting concerns (logging, caching, validation)
- **Middleware** - HTTP pipeline integration using MediatR

### **Benefits of This Approach**

✅ **Testability** - Each layer testable in isolation  
✅ **Flexibility** - Swap implementations without code changes  
✅ **Maintainability** - Clear boundaries and responsibilities  
✅ **Scalability** - Easy to add features following patterns  
? **Team Collaboration** - Multiple developers work on different layers  

---

## 🆘 Support

**Questions about layer integration?**

- 📖 **Documentation:** See layer-specific guides linked above
- 🐛 **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)
- 📧 **Email:** softevolutionsl@gmail.com
- 🐙 **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**Repository:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev)  
**Status:** ? Current & Maintained

---

*"In Clean Architecture, layers integrate through abstractions and clear contracts, not tight coupling."* ???
