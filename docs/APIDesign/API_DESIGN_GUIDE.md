# API Design Guide - CleanArchitecture.ApiTemplate

> "Thoughtful API design is the backbone of scalable, secure, and maintainable applications—embrace RESTful principles and .NET 8 best practices for long-term success."

## Overview

This guide documents the API design principles, patterns, and best practices implemented in CleanArchitecture.ApiTemplate. The API follows RESTful conventions, Clean Architecture principles, and modern .NET 8 standards for building secure, scalable, and maintainable web APIs.

---

## Table of Contents

1. [API Architecture](#api-architecture)
   - [Clean API Architecture Flow](#clean-api-architecture-flow)
   - [Protected API Request Workflow](#protected-api-request-workflow)
     - [Request Pipeline Layers](#request-pipeline-layers)
     - [Key Principles](#key-principles)
     - [Detailed Request Workflow (15 Steps)](#detailed-request-workflow-15-steps)
     - [Request Scenarios & Entry Points](#request-scenarios--entry-points)
     - [Request Headers Expected by API](#request-headers-expected-by-api)
     - [Performance Metrics by Request Type](#performance-metrics-by-request-type)
     - [Key Request Flow Principles](#key-request-flow-principles)
     - [Real-World Request Example](#real-world-request-example-complete-flow)
   - [Protected API Response Workflow](#protected-api-response-workflow)
     - [Response Pipeline Flow](#response-pipeline-flow)
     - [Response Pipeline Layers](#response-pipeline-layers)
     - [Key Response Principles](#key-response-principles)
     - [Detailed Response Workflow (15 Steps)](#detailed-response-workflow-15-steps)
     - [Response Scenarios & HTTP Status Codes](#response-scenarios--http-status-codes)
     - [Response Headers Applied by Middleware](#response-headers-applied-by-middleware)
     - [Performance Metrics by Response Type](#performance-metrics-by-response-type)
     - [Key Response Flow Principles](#key-response-flow-principles)
     - [Real-World Response Example](#real-world-response-example-success-with-cache)
2. [API Versioning](#api-versioning)
3. [RESTful Conventions](#restful-conventions)
   - [HTTP Methods](#http-methods)
   - [Resource Naming](#resource-naming)
   - [HTTP Status Codes](#http-status-codes)
4. [Request/Response Patterns](#requestresponse-patterns)
   - [Result Pattern](#result-pattern)
   - [Standard Response Formats](#standard-response-formats)
   - [Collaboration with UI Developers: Defining API Contracts](#collaboration-with-ui-developers-defining-api-contracts)
5. [Model Validation](#model-validation)
   - [Data Annotations](#data-annotations)
   - [Common Validation Attributes](#common-validation-attributes)
   - [Automatic Validation](#automatic-validation)
6. [Query Parameters](#query-parameters)
   - [Supported Features](#supported-features)
   - [Implementation Pattern](#implementation-pattern)
   - [Pagination Response](#pagination-response)
7. [Caching Strategy](#caching-strategy)
   - [In-Memory Cache (Cache-Aside Pattern)](#1-in-memory-cache-cache-aside-pattern)
   - [MediatR Pipeline Caching](#2-mediatr-pipeline-caching)
   - [Cache Benefits](#cache-benefits)
   - [Production Considerations](#production-considerations)
8. [Performance Optimization](#performance-optimization)
   - [Latency Metrics](#1-latency-metrics)
   - [Async/Await Best Practices](#2-asyncawait-best-practices)
   - [HTTP Client Factory](#3-http-client-factory)
9. [Error Handling](#error-handling)
   - [Global Exception Handling](#global-exception-handling)
   - [Structured Logging](#structured-logging)
10. [Security](#security)
    - [Authentication & Authorization](#authentication--authorization)
11. [Code Examples](#code-examples)
    - [Complete Controller Example](#complete-controller-example)
    - [CQRS Query & Handler Example](#cqrs-query--handler-example)
12. [Reference Files](#reference-files)
13. [Additional Resources](#additional-resources)
14. [Contact](#-contact)

---

## API Architecture

### **Clean API Architecture Flow**

#### **Protected API Request Workflow**

```
Client ? Middleware ? API Controller ? MediatR ? Handler ? Service ? External API
         (HTTP Pipeline)  (Presentation)   (Application)      (Infrastructure)
              ?
    JWT Blacklist
     Validation
```

**Request Pipeline Layers:**

1. **Middleware (HTTP Pipeline)** - `JwtBlacklistValidationMiddleware`
   - Validates JWT tokens before reaching controllers
   - Checks token blacklist via `IsTokenBlacklistedQuery`
   - Returns 401 Unauthorized if token is blacklisted
   - Executes before authorization policies

2. **API Controller (Presentation)** - `SampleController`, `AuthController`
   - Handles HTTP request/response
   - Validates model state
   - Sends commands/queries to MediatR
   - Returns appropriate HTTP status codes

3. **MediatR (Application)** - CQRS pattern
   - Routes commands/queries to handlers
   - Executes pipeline behaviors (logging, caching, validation)
   - Provides clean separation between presentation and business logic

4. **Handler (Application)** - `GetApiDataQueryHandler`, `LoginUserCommandHandler`
   - Contains business logic
   - Calls infrastructure services
   - Returns `Result<T>` pattern for consistent error handling

5. **Service (Infrastructure)** - `ApiIntegrationService`, `TokenBlacklistService`
   - Implements external integrations
   - Handles HTTP clients, databases, caching
   - Uses `IHttpClientFactory` with Polly resilience policies

6. **External API / Resources** - Third-party APIs, databases, cache
   - Azure Key Vault for secrets
   - Distributed cache (Redis) for token blacklist
   - Third-party RESTful APIs

**Key Principles:**
- ? **Separation of Concerns** - Middleware handles security, controllers handle HTTP, handlers handle business logic
- ? **Dependency Inversion** - Depend on abstractions (IMediator, IApiIntegrationService, ITokenBlacklistService)
- ? **Single Responsibility** - Each component has one clear purpose
- ? **Testability** - All layers can be tested independently
- ? **Security by Design** - Token validation happens before any business logic executes

**Detailed Request Workflow (15 Steps)**

```
1. Client sends GET /api/v1/sample with JWT token
   ?
2. JwtBlacklistValidationMiddleware extracts token
   ?
3. Middleware sends IsTokenBlacklistedQuery via MediatR
   ?
4. IsTokenBlacklistedQueryHandler checks cache (Memory ? Distributed)
   ?
5. If blacklisted ? return 401 Unauthorized (request stops here)
   If valid ? continue to next middleware
   ?
6. Authorization middleware checks JWT claims & policies
   ?
7. Request reaches SampleController.GetAllData()
   ?
8. Controller sends GetApiDataQuery via MediatR
   ?
9. CachingBehavior checks cache (pipeline behavior)
   ?
10. GetApiDataQueryHandler calls ApiIntegrationService
    ?
11. ApiKeyHandler adds API key (DelegatingHandler)
    ?
12. Polly applies retry + circuit breaker policies
    ?
13. External API returns data
    ?
14. Response flows back through pipeline
    ?
15. Client receives 200 OK with data
```

**Request Scenarios & Entry Points**

**1. Authenticated Request with Valid Token**
```
Client ? JWT Bearer Token (valid, not blacklisted)
         ?
Middleware validates token ? PASS
         ?
Authorization checks claims ? PASS
         ?
Controller processes request
         ?
Business logic executes
```

**2. Authenticated Request with Blacklisted Token**
```
Client ? JWT Bearer Token (blacklisted)
         ?
Middleware checks blacklist ? FAIL (token revoked)
         ?
Request terminated at middleware (401 Unauthorized)
         ?
Controller NEVER reached (early termination)
```

**3. Unauthenticated Request**
```
Client ? No JWT token or invalid token
         ?
ASP.NET Core Authentication middleware ? FAIL
         ?
Request terminated (401 Unauthorized)
         ?
JwtBlacklistValidationMiddleware NOT executed
```

**4. Authenticated Request with Insufficient Permissions**
```
Client ? Valid JWT token (User role)
         ?
Middleware validates token ? PASS
         ?
Controller checks [Authorize(Policy = "AdminOnly")]
         ?
Authorization policy check ? FAIL (User != Admin)
         ?
Request terminated (403 Forbidden)
```

**5. Request with Cache Hit (Fast Path)**
```
Client ? Valid request
         ?
Authentication & Authorization ? PASS
         ?
Controller ? MediatR ? CachingBehavior
         ?
Cache HIT! (data found in cache)
         ?
Handler SKIPPED, Service SKIPPED, External API SKIPPED
         ?
Cached data returned immediately (~10ms response)
```

**Request Headers Expected by API**

```http
GET /api/v1/sample HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
Accept: application/json
User-Agent: MyApp/1.0
X-Request-Id: abc123-def456-ghi789

// Optional Headers
Cache-Control: no-cache
Accept-Encoding: gzip, deflate, br
```

**Performance Metrics by Request Type**

| Request Type | Layers Executed | Average Time | Cache Status |
|--------------|----------------|--------------|--------------|
| **Cached Request (Best)** | Up to CachingBehavior | ~10-20ms | HIT ? |
| **Uncached Request** | All layers + External API | ~150-300ms | MISS ? |
| **Blacklisted Token** | Middleware only | ~5-10ms | N/A |
| **Invalid Token** | Authentication middleware | ~2-5ms | N/A |
| **Insufficient Permissions** | Up to Authorization | ~3-8ms | N/A |

**Key Request Flow Principles**

- ? **Security First** - Token validation happens before any business logic
- ? **Early Termination** - Invalid requests stop at middleware (don't waste resources)
- ? **CQRS Everywhere** - Even middleware uses CQRS (IsTokenBlacklistedQuery)
- ? **Automatic Caching** - Cache check happens transparently via pipeline behavior
- ? **Dual-Cache Strategy** - Memory cache (fast) + Distributed cache (shared)
- ? **Resilience Patterns** - Polly retry and circuit breaker for external calls

**Real-World Request Example (Complete Flow)**

```csharp
// 1. Client sends HTTP request with JWT token
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);
var response = await client.GetAsync("https://api.example.com/api/v1/sample");

/ Reached controller
[HttpGet]
[Authorize] // Requires valid JWT token
public async Task<IActionResult> GetAllData()
{
    _logger.LogInformation(
        "User {UserId} requested all data at {Time}",
        User.Identity?.Name, DateTime.UtcNow);
    
    // 4. Controller sends query to MediatR
    var result = await _mediator.Send(new GetApiDataQuery());
    
    // 5. CachingBehavior checks cache first
    // If cache miss, handler is executed
    // If cache hit, handler is skipped
    
    return result.Success 
        ? Ok(result.Data) 
        : BadRequest(new { error = result.Error });
}

// 6. MediatR routing and pipeline behaviors
// LoggingBehavior ? logs request
// ValidationBehavior ? validates query (if applicable)
// CachingBehavior ? checks cache
//   - Cache HIT: return cached data (~10ms total)
//   - Cache MISS: execute handler (~250ms total)

// 7. Handler executes (on cache miss)
public async Task<Result<List<SampleDto>>> Handle(
    GetApiDataQuery request, 
    CancellationToken cancellationToken)
{
    _logger.LogInformation("Executing GetApiDataQuery - Cache MISS");
    
    // 8. Handler calls service
    return await _apiService.GetAllDataAsync<List<SampleDto>>("api/data");
}

// 9. Service makes external API call with resilience
// ApiKeyHandler adds X-API-Key header
// Polly retries on transient failures (3 attempts, exponential backoff)
// Circuit breaker prevents cascading failures

// 10. Response flows back through pipeline (see Response Workflow)
```
---

#### **Protected API Response Workflow**

**Complete Round-Trip Journey: Request ? Processing ? Response**

This workflow demonstrates how responses flow back through the architecture layers, including caching, serialization, and HTTP formatting.

**Response Pipeline Flow:**

```
External API ? Service ? Handler ? MediatR ? Controller ? Middleware ? Client
(Infrastructure) (Application)  (Presentation) (HTTP Pipeline)
       ?
   Result<T>
   Wrapping
```

**Response Pipeline Layers:**

1. **External API / Resources** - Third-party APIs, databases, cache
   - Returns JSON data or error responses
   - Azure Key Vault provides secrets
   - Distributed cache (Redis) stores token blacklist and cached responses
   - Third-party RESTful APIs return business data

2. **Service (Infrastructure)** - `ApiIntegrationService`, `TokenBlacklistService`
   - Receives HTTP responses from external APIs
   - Deserializes JSON to strongly-typed DTOs
   - Wraps data in `Result<T>` pattern for consistent error handling
   - Handles API errors and exceptions gracefully

3. **Handler (Application)** - `GetApiDataQueryHandler`, `LoginUserCommandHandler`
   - Receives `Result<T>` from service
   - Validates business rules
   - Returns `Result<T>` to MediatR
   - Logs execution status and errors

4. **MediatR (Application)** - CQRS pattern
   - Receives response from handler
   - Executes reverse pipeline behaviors (caching writes, response logging)
   - CachingBehavior stores successful results in cache
   - Returns `Result<T>` to controller

5. **API Controller (Presentation)** - `SampleController`, `AuthController`
   - Checks `Result<T>.Success` property
   - Returns `Ok(result.Data)` for success (200 OK)
   - Returns `BadRequest(result.Error)` for failures (400 Bad Request)
   - Adds response metadata and correlation IDs

6. **Middleware (HTTP Pipeline)** - Response middleware stack
   - CORS middleware adds `Access-Control-*` headers
   - Security Headers middleware adds `X-Content-Type-Options`, `X-Frame-Options`
   - Compression middleware compresses response body (if enabled)
   - Logging middleware records response time and status

**Key Response Principles:**
- ? **Result Pattern** - No exception-based control flow for error handling
- ? **Type Safety** - Strong typing maintained from service to controller (Result\<T\>)
- ? **Automatic Caching** - Successful responses automatically cached by pipeline behavior
- ? **Security Headers** - Added automatically by middleware on every response
- ? **Structured Logging** - Response status, duration, and cache status logged
- ? **Content Negotiation** - ASP.NET Core serializes to JSON automatically

**Detailed Response Workflow (15 Steps)**

```
1. External API returns JSON data
   ?
2. ApiIntegrationService receives HTTP response
   ?
3. Service deserializes JSON to strongly-typed DTO
   ?
4. Service wraps data in Result<T> pattern
   Result<List<SampleDto>>.Ok(data)
   ?
5. Result<T> returned to GetApiDataQueryHandler
   ?
6. Handler validates result and returns to MediatR
   ?
7. MediatR Pipeline - CachingBehavior intercepts response
   ?
8. If query implements ICacheable ? store in cache
   Memory Cache (fast access) + Distributed Cache (shared)
   ?
9. MediatR returns Result<T> to Controller
   ?
10. Controller checks Result<T>.Success
    ?
11. If success ? return Ok(result.Data) with 200 status
    If failure ? return BadRequest(result.Error) with 400 status
    ?
12. ASP.NET Core serializes response to JSON
    Content-Type: application/json
    ?
13. Response flows back through middleware pipeline
    - CORS middleware adds Access-Control headers
    - Security Headers middleware adds X-Content-Type-Options, X-Frame-Options
    - Compression middleware (if enabled) compresses response
    ?
14. HTTP response sent over network
    Status: 200 OK
    Headers: Content-Type, Content-Length, Security Headers
    Body: JSON data
    ?
15. Client receives complete HTTP response
    {
      "id": 123,
      "name": "Sample Data",
      "createdAt": "2024-01-15T10:30:00Z"
    }
```

**Response Scenarios & HTTP Status Codes**

**1. Success Response (200 OK)**
```
Handler returns: Result<T>.Ok(data)
         ?
Controller returns: Ok(result.Data)
         ?
Client receives: 200 OK
{
  "id": 123,
  "name": "Sample Data",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**2. Validation Error (400 Bad Request)**
```
Handler returns: Result<T>.Fail("Validation failed")
         ?
Controller returns: BadRequest(new { error = result.Error })
         ?
Client receives: 400 Bad Request
{
  "error": "Validation failed",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**3. Unauthorized (401 Unauthorized)**
```
Middleware detects: Token is blacklisted
         ?
Middleware returns: 401 Unauthorized (request stops here)
         ?
Client receives: 401 Unauthorized
{
  "error": "Token has been revoked",
  "message": "Please log in again"
}
```

**4. External API Failure (500 Internal Server Error)**
```
ApiIntegrationService: External API returns 500
         ?
Service returns: Result<T>.Fail("External API error")
         ?
Controller catches exception
         ?
Controller returns: StatusCode(500, new { error = "Unexpected error" })
         ?
Client receives: 500 Internal Server Error
{
  "error": "An unexpected error occurred.",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**5. Cached Response (200 OK - Fast Path)**
```
CachingBehavior: Cache HIT!
         ?
Handler SKIPPED (not executed)
         ?
Service SKIPPED (not executed)
         ?
External API SKIPPED (not called)
         ?
Cached Result<T> returned directly to Controller
         ?
Controller returns: Ok(cachedData)
         ?
Client receives: 200 OK (served from cache - ultra-fast!)
Response Time: ~10ms (vs ~200ms without cache)
```

**Response Headers Applied by Middleware**

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Content-Length: 1234
Date: Mon, 15 Jan 2024 10:30:00 GMT

// Security Headers (Applied by SecurityHeadersMiddleware)
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin

// CORS Headers (Applied by CORS Middleware)
Access-Control-Allow-Origin: https://app.example.com
Access-Control-Allow-Credentials: true

// Caching Headers (Optional - for client-side caching)
Cache-Control: private, max-age=300

// Custom Headers (Optional - for tracking)
X-Request-Id: abc123-def456-ghi789
X-Response-Time: 45ms
X-Cache-Status: HIT
```

**Performance Metrics by Response Type**

| Response Type | Layers Executed | Average Time | Cache Status |
|---------------|----------------|--------------|--------------|
| **Cached (Best Case)** | Middleware ? Controller ? MediatR ? CachingBehavior | ~10-20ms | HIT ? |
| **Uncached (Normal)** | All layers + External API | ~150-300ms | MISS ? |
| **Auth Failure** | Middleware only (early termination) | ~5-10ms | N/A |
| **Validation Error** | Up to Controller (no service call) | ~20-30ms | N/A |

**Key Response Flow Principles**

- ? **Result Pattern** - Consistent error handling without exceptions
- ? **Early Termination** - Auth failures stop at middleware (don't reach controller)
- ? **Automatic Caching** - Transparent caching via MediatR pipeline behavior
- ? **Security Headers** - Automatically added by middleware on every response
- ? **Structured Logging** - Response status, duration, and cache status logged
- ? **Type Safety** - Strong typing maintained throughout (Result\<T\> ? IActionResult)

**Real-World Response Example (Success with Cache)**

```csharp
// 1. External API returns data
var httpResponse = await httpClient.GetAsync("https://api.example.com/data");

// 2. ApiIntegrationService processes response
var data = await httpResponse.Content.ReadFromJsonAsync<List<SampleDto>>();
var result = Result<List<SampleDto>>.Ok(data);

// 3. Handler returns Result<T>
return result; // Success = true, Data = [...], Error = null

// 4. CachingBehavior caches the result
await _cache.SetAsync("api-data-all", result, TimeSpan.FromMinutes(5));

// 5. Controller checks result
if (result.Success)
{
    _logger.LogInformation("Successfully retrieved {Count} items", result.Data.Count);
    return Ok(result.Data); // 200 OK
}

// 6. Client receives JSON response
// HTTP/1.1 200 OK
// Content-Type: application/json
// X-Cache-Status: MISS (first request)
// X-Response-Time: 245ms
//
// [
//   { "id": 1, "name": "Item 1" },
//   { "id": 2, "name": "Item 2" }
// ]

// 7. Subsequent request (within 5 minutes) uses cache
// HTTP/1.1 200 OK
// X-Cache-Status: HIT
// X-Response-Time: 12ms (20x faster!)
```

---

## API Versioning

### **URL-Based Versioning**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class SampleController : ControllerBase
{
    // v1 endpoints
}

[ApiController]
[Route("api/v2/[controller]")]
public class SampleV2Controller : ControllerBase
{
    // v2 endpoints with new features
}
```

**URL Patterns:**

| Version | Pattern | Example |
|---------|---------|---------|
| v1 | `/api/v1/{resource}` | `/api/v1/sample` |
| v2 | `/api/v2/{resource}` | `/api/v2/sample` |

**Benefits:**
- Clear version identification
- Multiple versions coexist
- Clients upgrade at their own pace
- No breaking changes for existing clients

---

## RESTful Conventions

### **HTTP Methods**

| Method | Usage | Idempotent | Safe |
|--------|-------|------------|------|
| **GET** | Retrieve resource(s) | ? Yes | ? Yes |
| **POST** | Create new resource | ? No | ? No |
| **PUT** | Update/Replace entire resource | ? Yes | ? No |
| **PATCH** | Partial update | ? No | ? No |
| **DELETE** | Remove resource | ? Yes | ? No |

### **Resource Naming**

**Rules:**
- Use **nouns**, not verbs
- Use **plural** for collections
- Use **lowercase** with hyphens
- Use **hierarchical structure** for relationships

**Examples:**

```
? Good:
GET    /api/v1/users               # Get all users
GET    /api/v1/users/123           # Get user by ID
POST   /api/v1/users               # Create new user
PUT    /api/v1/users/123           # Update user
DELETE /api/v1/users/123           # Delete user
GET    /api/v1/users/123/orders    # Get user's orders

? Bad:
GET    /api/v1/getUsers             # Verb in URL
POST   /api/v1/user                 # Singular instead of plural
GET    /api/v1/users_list           # Underscore instead of hyphen
```

### **HTTP Status Codes**

#### **Success (2xx)**
- **200 OK** - Successful GET, PUT, PATCH, DELETE
- **201 Created** - Successful POST (resource created)
- **204 No Content** - Successful DELETE (no response body)

#### **Client Errors (4xx)**
- **400 Bad Request** - Invalid input, validation errors
- **401 Unauthorized** - Missing or invalid authentication token
- **403 Forbidden** - Authenticated but not authorized
- **404 Not Found** - Resource doesn't exist
- **429 Too Many Requests** - Rate limit exceeded

#### **Server Errors (5xx)**
- **500 Internal Server Error** - Unexpected server error
- **503 Service Unavailable** - Server temporarily unavailable

---

## Request/Response Patterns as well known as API Contracts

### **Result Pattern**

CleanArchitecture.ApiTemplate uses the Result Pattern for consistent error handling without exceptions.

**Implementation:**

```csharp
public class Result<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    
    public static Result<T> Ok(T data) => new() { Success = true, Data = data };
    public static Result<T> Fail(string error) => new() { Success = false, Error = error };
}
```

**Controller Usage:**

```csharp
[HttpGet]
public async Task<IActionResult> GetAllData()
{
    var result = await _mediator.Send(new GetApiDataQuery());
    
    if (!result.Success)
        return BadRequest(new { error = result.Error });
    
    return Ok(result.Data);
}
```

**Benefits:**
- ? No exception-based control flow
- ? Type-safe error handling
- ? Consistent response pattern
- ? Easy to compose operations

### **Standard Response Formats**

**Success Response:**
```json
{
  "id": 123,
  "name": "Sample Data",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Response:**
```json
{
  "error": "Resource not found",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Validation Error Response:**
```json
{
  "errors": {
    "Name": ["Name is required"],
    "Email": ["Invalid email format"]
  }
}
```

### Collaboration with UI Developers: Defining API Contracts

Effective API contract specification requires close collaboration between backend and UI developers.
Early alignment on request payloads, response formats, and error structures ensures seamless integration and reduces rework. Use shared API documentation (e.g., OpenAPI/Swagger), example payloads, and regular feedback cycles to clarify expectations. Agree on field names, data types, and validation rules before implementation. This approach helps UI developers design forms and data flows that match backend requirements, while backend teams can anticipate frontend needs and edge cases. Iterative review and mock API responses further streamline development and testing.

#### Practical Example

Suppose the UI team needs a user registration endpoint. I as backend developer and UI developers meet to define the contract:

- **Request Payload:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "P@ssw0rd!"
}
```
- **Response Payload (Success):**
```json
{
  "id": 101,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "createdAt": "2024-01-15T10:30:00Z"
}
```
- **Response Payload (Validation Error):**
```json
{
  "errors": {
    "Email": ["Invalid email format"],
    "Password": ["Password must be at least 8 characters"]
  }
}
```

Both teams document this contract in the shared OpenAPI/Swagger spec and use it for development and testing.

#### Reference
- See [`docs/APIDesign/API_CONTRACTS_EXAMPLES.md`](./API_CONTRACTS_EXAMPLES.md) for more contract samples and collaboration tips.
- [OpenAPI Specification](https://swagger.io/specification/)

---

## Model Validation

### **Data Annotations**

```csharp
public class CreateSampleDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }
    
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }
    
    [Range(1, 1000)]
    public int Value { get; set; }
}
```

### **Common Validation Attributes**

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[Required]` | Field must have value | `[Required]` |
| `[StringLength]` | Min/max string length | `[StringLength(100, MinimumLength = 3)]` |
| `[Range]` | Numeric range | `[Range(1, 100)]` |
| `[EmailAddress]` | Valid email format | `[EmailAddress]` |
| `[RegularExpression]` | Custom pattern | `[RegularExpression(@"^\d{3}-\d{3}-\d{4}$")]` |

### **Automatic Validation**

ASP.NET Core automatically validates models before controller actions:

```csharp
[HttpPost]
public IActionResult Create([FromBody] CreateSampleDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    // Process valid data
    return Created();
}
```

---

## Query Parameters

### **Supported Features**

**1. Filtering**
```
GET /api/v1/users?status=active&role=admin
```

**2. Sorting**
```
GET /api/v1/users?sortBy=name&order=asc
```

**3. Pagination**
```
GET /api/v1/users?page=1&pageSize=10
```

**4. Searching**
```
GET /api/v1/users?search=john
```

### **Implementation Pattern**

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] string? search,
    [FromQuery] string? sortBy,
    [FromQuery] string? order = "asc",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var query = new GetPagedDataQuery(search, sortBy, order, page, pageSize);
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### **Pagination Response**

```json
{
  "data": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalRecords": 100,
    "totalPages": 10,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

---

## Caching Strategy

### **1. In-Memory Cache (Cache-Aside Pattern)**

**Characteristics:**
- ? Microsecond latency
- ?? Process-local (not shared across instances)
- ?? Volatile (lost on restart)

**Implementation:**

```csharp
public async Task<T?> GetOrSetAsync<T>(
    string key, 
    Func<Task<T>> factory, 
    TimeSpan? expiration = null)
{
    // Check cache first
    var cached = await _cache.GetStringAsync(key);
    if (cached != null)
        return JsonSerializer.Deserialize<T>(cached);
    
    // Cache miss - execute factory
    var data = await factory();
    
    // Store in cache
    await _cache.SetStringAsync(key, JsonSerializer.Serialize(data));
    return data;
}
```

### **2. MediatR Pipeline Caching**

**Query with Caching:**

```csharp
public record GetApiDataQuery : IRequest<Result<List<SampleDto>>>, ICacheable
{
    public string CacheKey => "api-data-all";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

**Pipeline Behavior:**

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is ICacheable cacheable)
        {
            var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey);
            if (cached != null) return cached;
            
            var response = await next();
            await _cache.SetAsync(cacheable.CacheKey, response, cacheable.Expiration);
            return response;
        }
        
        return await next();
    }
}
```

### **Cache Benefits**

? **Performance** - Reduces API calls and database queries  
? **Scalability** - Handles more requests with same resources  
? **Cost Savings** - Fewer third-party API calls  
? **Resilience** - Serve cached data if external API is down  

### **Production Considerations**

?? **Limitation:** In-memory cache is NOT shared across instances

**Production Recommendations:**
- Use **Redis** for distributed caching
- Use **SQL Server distributed cache** for persistent caching
- Implement **cache warming** for critical data

**Redis Migration:**

```csharp
// Development - In-memory
services.AddDistributedMemoryCache();

// Production - Redis
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:ConnectionString"];
    options.InstanceName = "CleanArchitecture.ApiTemplate:";
});
```

---

## Performance Optimization

### **1. Latency Metrics**

```csharp
var stopwatch = Stopwatch.StartNew();
var result = await _mediator.Send(query);
stopwatch.Stop();

_logger.LogInformation(
    "API call completed in {Duration}ms - Cache: {CacheStatus}",
    stopwatch.ElapsedMilliseconds,
    cacheHit ? "HIT" : "MISS");
```

### **2. Async/Await Best Practices**

```csharp
// ? Good - Non-blocking
public async Task<IActionResult> GetData()
{
    var result = await _apiService.GetDataAsync();
    return Ok(result);
}

// ? Bad - Blocking
public IActionResult GetData()
{
    var result = _apiService.GetData().Result; // Blocks thread!
    return Ok(result);
}
```

### **3. HTTP Client Factory**

```csharp
services.AddHttpClient<IApiIntegrationService, ApiIntegrationService>(client =>
{
    client.BaseAddress = new Uri(configuration["ThirdPartyApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddHttpMessageHandler<ApiKeyHandler>();
```

**Benefits:**
- ? Reuses connections (prevents socket exhaustion)
- ? Respects DNS TTL
- ? Built-in resilience with Polly
- ? Easy to test and mock

---

## Error Handling

### **Global Exception Handling**

```csharp
[HttpGet]
public async Task<IActionResult> GetData()
{
    try
    {
        _logger.LogInformation("User {UserId} requested data", User.Identity?.Name);
        
        var result = await _mediator.Send(new GetApiDataQuery());
        
        if (!result.Success)
        {
            _logger.LogWarning("Failed: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }
        
        return Ok(result.Data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error occurred");
        return StatusCode(500, new { error = "An unexpected error occurred." });
    }
}
```

### **Structured Logging**

```csharp
// ? Good - Structured
_logger.LogInformation(
    "Processing {RequestId} for {Endpoint} by {UserId}",
    requestId, endpoint, userId);

// ? Bad - String interpolation
_logger.LogInformation($"Processing {requestId} for {endpoint}");
```

---

## Security

### **Authentication & Authorization**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // All endpoints require authentication
public class SampleController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() 
    { 
        // Requires authentication
    }
    
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")] // Requires Admin role
    public IActionResult GetAdminData() 
    { 
        // Requires Admin role
    }
    
    [HttpGet("status")]
    [AllowAnonymous] // Public endpoint
    public IActionResult GetStatus() 
    { 
        // No authentication required
    }
}
```

**For complete security documentation, see:**
- [`API-SECURITY-IMPLEMENTATION-GUIDE.md`](../API-SECURITY-IMPLEMENTATION-GUIDE.md)
- [`TEST_AUTHENTICATION_GUIDE.md`](../AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)

---

## Domain Entity to DTO Mapping ??

### **Using AutoMapper for API Responses**

All API endpoints return DTOs mapped from domain entities using AutoMapper, following Clean Architecture principles.

#### **Mapping Configuration**

**Location:** `Core/Application/Common/Profiles/ApiDataMappingProfile.cs`

**Purpose:** Transform domain entities to DTOs for API responses, keeping domain logic separate from presentation concerns.

**Example Mapping:**
```csharp
public class ApiDataMappingProfile : Profile
{
    public ApiDataMappingProfile()
    {
        // Domain Entity ? Response DTO
        CreateMap<ApiDataItem, ApiDataItemDto>()
            .ForMember(dest => dest.Status, 
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.IsFresh, 
                opt => opt.MapFrom(src => src.IsFresh(TimeSpan.FromHours(1))))
            .ForMember(dest => dest.Age, 
                opt => opt.MapFrom(src => src.GetAge()))
            // Extract metadata from domain entity
            .ForMember(dest => dest.Category, 
                opt => opt.MapFrom(src => src.GetMetadata<string>("category")))
            .ForMember(dest => dest.Price, 
                opt => opt.MapFrom(src => src.GetMetadata<decimal?>("price")))
            .ForMember(dest => dest.Rating, 
                opt => opt.MapFrom(src => src.GetMetadata<double?>("rating")));
    }
}
```

#### **Controller Example**

**Handler with Domain Entities:**
```csharp
public class GetApiDataWithMappingQueryHandler 
    : IRequestHandler<GetApiDataWithMappingQuery, Result<List<ApiDataItemDto>>>
{
    private readonly IApiDataItemRepository _repository;
    private readonly IMapper _autoMapper;
    
    public async Task<Result<List<ApiDataItemDto>>> Handle(...)
    {
        // 1. Fetch domain entities from repository
        var domainEntities = await _repository.GetItemsBySourceUrlAsync(
            request.ApiUrl, cancellationToken);
        
        // 2. Use domain entity business logic
        var activeItems = domainEntities
            .Where(item => item.Status == DataStatus.Active && 
                          !item.NeedsRefresh(TimeSpan.FromHours(1)))
            .ToList();
        
        // 3. Map domain entities to DTOs using AutoMapper
        var responseDtos = _autoMapper.Map<List<ApiDataItemDto>>(activeItems);
        
        return Result<List<ApiDataItemDto>>.Ok(responseDtos);
    }
}
```

**Controller Usage:**
```csharp
[HttpGet("data-with-mapping")]
[ProducesResponseType(typeof(List<ApiDataItemDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> GetDataWithMapping(
    [FromQuery] string apiUrl,
    [FromQuery] bool useAutoMapper = true)
{
    var query = new GetApiDataWithMappingQuery(apiUrl, useAutoMapper);
    var result = await _mediator.Send(query);
    
    if (!result.Success)
        return BadRequest(new ProblemDetails { Detail = result.Error });
    
    return Ok(result.Data); // Returns List<ApiDataItemDto>
}
```

#### **Response DTO Structure**

**Complete DTO Example:**
```csharp
/// <summary>
/// DTO for ApiDataItem responses in controllers.
/// Used for API endpoints to return clean, structured data.
/// </summary>
public class ApiDataItemDto
{
    /// <summary>
    /// Internal unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// External system identifier.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Item name/title.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Item description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Source API URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Last synchronization timestamp.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    /// <summary>
    /// Data status (Active, Stale, Deleted).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if data is fresh (< 1 hour old).
    /// </summary>
    public bool IsFresh { get; set; }

    /// <summary>
    /// Age of the data since last sync.
    /// </summary>
    public TimeSpan Age { get; set; }

    /// <summary>
    /// Item category (from metadata).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Item price (from metadata).
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Item rating (from metadata).
    /// </summary>
    public double? Rating { get; set; }

    /// <summary>
    /// Item tags (from metadata).
    /// </summary>
    public string[]? Tags { get; set; }
}
```

**Sample JSON Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "externalId": "12345",
  "name": "Sample Product",
  "description": "Product description",
  "sourceUrl": "https://api.example.com/products/12345",
  "lastSyncedAt": "2025-01-17T10:30:00Z",
  "status": "Active",
  "isFresh": true,
  "age": "00:15:30",
  "category": "Electronics",
  "price": 299.99,
  "rating": 4.7,
  "tags": ["new", "featured"]
}
```

#### **Benefits of This Approach**

? **Domain Logic Protection** - Domain entities never exposed directly to API consumers  
? **Type Safety** - Compile-time validation with AutoMapper profiles  
? **Metadata Extraction** - Flexible key-value metadata transformed to typed properties  
? **Computed Properties** - Domain methods (IsFresh, GetAge) exposed as DTO properties  
? **Clean Separation** - Presentation concerns separated from business logic  
? **Versioning Support** - Multiple DTO versions without changing domain entities  

#### **Complete Request-Response Flow**

```
1. Client Request
   GET /api/v1/data-with-mapping?apiUrl=https://api.test.com&useAutoMapper=true
   ?
2. Controller receives request
   ?
3. Controller sends GetApiDataWithMappingQuery via MediatR
   ?
4. Handler queries repository for domain entities (ApiDataItem)
   ?
5. Handler applies domain logic:
   - item.NeedsRefresh(threshold)
   - item.Status == DataStatus.Active
   - item.IsFresh(TimeSpan.FromHours(1))
   ?
6. Handler maps domain entities to DTOs via AutoMapper
   - Transforms Status enum to string
   - Calculates IsFresh and Age
   - Extracts metadata to typed properties
   ?
7. Controller returns 200 OK with List<ApiDataItemDto>
   ?
8. Client receives JSON response with clean, structured data
```

#### **Mapping Configuration**

**AutoMapper Registration:**
```csharp
// In ApplicationServiceExtensions.cs
services.AddAutoMapper(applicationAssembly);
```

**Profile Validation Test:**
```csharp
[Fact]
public void ApiDataMappingProfile_Configuration_IsValid()
{
    var config = new MapperConfiguration(cfg =>
        cfg.AddProfile<ApiDataMappingProfile>());
    
    // Validates all mappings at compile time
    config.AssertConfigurationIsValid();
}
```

?? **Related Documentation:**
- [Hybrid Mapping Strategy](../CleanArchitecture/HYBRID-MAPPING-STRATEGY.md) - Complete mapping guide
- [Application Layer Guide](../CleanArchitecture/Projects/02-Application-Layer.md) - CQRS and mapping patterns
- [Domain Layer Guide](../CleanArchitecture/Projects/01-Domain-Layer.md) - Domain entity implementation

---

## Code Examples

### **Complete Controller Example**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class SampleController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SampleController> _logger;

    public SampleController(IMediator mediator, ILogger<SampleController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all data from external API
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllData()
    {
        try
        {
            _logger.LogInformation("User {UserId} requested all data", 
                User.Identity?.Name ?? "Anonymous");
            
            var result = await _mediator.Send(
                new GetApiDataQuery<SampleDtoModel>("api-endpoint"));
            
            if (result.Success)
            {
                _logger.LogInformation("Successfully retrieved data");
                return Ok(result.Data);
            }
            
            _logger.LogWarning("Failed to retrieve data: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Get data by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDataById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Invalid ID provided");
            return BadRequest(new { error = "ID parameter is required." });
        }

        _logger.LogInformation("User {UserId} requested data for ID: {Id}", 
            User.Identity?.Name, id);
        
        var result = await _mediator.Send(
            new GetApiDataByIdQuery<SampleDtoModel>("api-endpoint", id));
        
        if (result.Success)
            return Ok(result.Data);
        
        _logger.LogWarning("Failed to retrieve data for ID {Id}: {Error}", 
            id, result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus() => 
        Ok(new { status = "Operational", timestamp = DateTime.UtcNow });
}
```

### **CQRS Query & Handler Example**

```csharp
// Query
public record GetApiDataQuery : IRequest<Result<List<SampleDto>>>, ICacheable
{
    public string CacheKey => "api-data-all";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

// Handler
public class GetApiDataQueryHandler 
    : IRequestHandler<GetApiDataQuery, Result<List<SampleDto>>>
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

    public async Task<Result<List<SampleDto>>> Handle(
        GetApiDataQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing GetApiDataQuery");
        return await _apiService.GetAllDataAsync<List<SampleDto>>("api/data");
    }
}
```

---

## Reference Files

**Controllers:**
- ?? `Presentation/Controllers/v1/SampleController.cs` - Main API controller
- ?? `Presentation/Controllers/v1/AuthController.cs` - Authentication endpoints

**CQRS:**
- ?? `Core/Application/Features/GetData/Queries/GetApiDataQuery.cs`
- ?? `Core/Application/Features/GetData/Queries/GetApiDataQueryHandler.cs`

**Caching:**
- ?? `Core/Application/Common/Behaviors/CachingBehavior.cs`
- ?? `Core/Application/Common/Behaviors/ICacheable.cs`
- ?? `Infrastructure/Caching/SampleCache.cs`

**Services:**
- ?? `Infrastructure/Services/ApiIntegrationService.cs`
- ?? `Infrastructure/Handlers/ApiKeyHandler.cs`

**Configuration:**
- ?? `Presentation/Extensions/DependencyInjection/PresentationServiceExtensions.cs`
- ?? `Presentation/Extensions/HttpPipeline/WebApplicationExtensions.cs`

---

## Additional Resources

- **[API Testing Guide](./TEST_API_ENDPOINTS_GUIDE.md)** - Complete guide for testing APIs locally, with Docker, Swagger UI, and Postman
- **[Clean Architecture Guide](../CleanArchitecture/CLEAN_ARCHITECTURE_GUIDE.md)** - Architecture principles and patterns
- **[Security Guide](../API-SECURITY-IMPLEMENTATION-GUIDE.md)** - Complete security implementation
- **[Authentication Testing Guide](../AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)** - Detailed authentication and authorization testing
- **[Deployment Guide](../DEPLOYMENT_GUIDE.md)** - Azure deployment instructions

---

## ?? Contact

**Need Help?**

- ?? **Documentation:** Start with the deployment guides above
- ?? **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)
- ?? **Email:** softevolutionsl@gmail.com
- ?? **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)


