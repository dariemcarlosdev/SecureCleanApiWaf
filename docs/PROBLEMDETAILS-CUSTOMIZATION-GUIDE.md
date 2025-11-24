# ProblemDetails Customization Guide

## ?? Overview

This guide explains how to customize the `ProblemDetails` responses in your CleanArchitecture.ApiTemplate API. `ProblemDetails` is the standard error response format based on [RFC 7807](https://tools.ietf.org/html/rfc7807) that ASP.NET Core uses automatically for API errors.

---

## ?? What is ProblemDetails?

`ProblemDetails` is a **standardized error response format** for HTTP APIs that includes:

| Property | Description | Example |
|----------|-------------|---------|
| `type` | URI reference identifying the problem type | `"https://tools.ietf.org/html/rfc9110#section-15.5.1"` |
| `title` | Short, human-readable summary | `"Bad Request"` |
| `status` | HTTP status code | `400` |
| `detail` | Human-readable explanation | `"The request was invalid"` |
| `instance` | URI reference for this specific occurrence | `"/api/v1/sample/123"` |

---

## ? Default Behavior (No Customization)

**Without any customization**, your API automatically returns `ProblemDetails` for errors:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "traceId": "00-abc123..."
}
```

This happens automatically because of the `[ApiController]` attribute on your controllers.

---

## ? Method 1: CustomizeProblemDetails (Recommended) ?

**Location**: `Presentation/Extensions/DependencyInjection/PresentationServiceExtensions.cs`

### Implementation

This method is **already implemented** in your `PresentationServiceExtensions.cs`:

```csharp
services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // Add custom properties to all ProblemDetails responses
        ctx.ProblemDetails.Extensions.Add("timestamp", DateTime.UtcNow);
        ctx.ProblemDetails.Extensions.Add("traceId", ctx.HttpContext.TraceIdentifier);
        ctx.ProblemDetails.Extensions.Add("machineName", Environment.MachineName);
        ctx.ProblemDetails.Extensions.Add("environment", 
            ctx.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName);
        ctx.ProblemDetails.Extensions.Add("apiVersion", "v1");
        ctx.ProblemDetails.Extensions.Add("path", ctx.HttpContext.Request.Path.Value);
        ctx.ProblemDetails.Extensions.Add("method", ctx.HttpContext.Request.Method);
        
        // Optional: Add user info for authenticated requests
        if (ctx.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            ctx.ProblemDetails.Extensions.Add("userId", 
                ctx.HttpContext.User.Identity.Name ?? "Unknown");
        }
        
        // Customize title based on status code
        ctx.ProblemDetails.Title = ctx.ProblemDetails.Status switch
        {
            400 => "Bad Request - Invalid Input",
            401 => "Unauthorized - Authentication Required",
            403 => "Forbidden - Insufficient Permissions",
            404 => "Not Found - Resource Does Not Exist",
            429 => "Too Many Requests - Rate Limit Exceeded",
            500 => "Internal Server Error - Something Went Wrong",
            _ => ctx.ProblemDetails.Title
        };
        
        // Add support contact
        ctx.ProblemDetails.Extensions.Add("supportContact", "softevolutionsl@gmail.com");
        ctx.ProblemDetails.Extensions.Add("documentationUrl", 
            "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki");
    };
});
```

### Result

Your API will now return enriched error responses:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request - Invalid Input",
  "status": 400,
  "timestamp": "2024-01-15T10:30:00Z",
  "traceId": "00-abc123...",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/sample/123",
  "method": "GET",
  "userId": "testuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

### ? Benefits

- **Global**: Applies to ALL error responses
- **Simple**: One place to configure all customizations
- **Consistent**: All errors have the same structure
- **Automatic**: Works with `[ApiController]` attribute

---

## ?? Method 2: Custom ProblemDetailsFactory

**When to use**: Advanced scenarios where you need complete control over `ProblemDetails` creation.

### Step 1: Create Custom Factory

Create a new file: `Infrastructure/Services/CustomProblemDetailsFactory.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CleanArchitecture.ApiTemplate.Infrastructure.Services;

/// <summary>
/// Custom ProblemDetailsFactory for advanced ProblemDetails customization
/// </summary>
public class CustomProblemDetailsFactory : ProblemDetailsFactory
{
    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        statusCode ??= 500;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance,
        };

        // Add custom properties
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["requestId"] = Guid.NewGuid().ToString();
        
        // Add error code based on status
        problemDetails.Extensions["errorCode"] = statusCode switch
        {
            400 => "INVALID_REQUEST",
            401 => "AUTHENTICATION_REQUIRED",
            403 => "INSUFFICIENT_PERMISSIONS",
            404 => "RESOURCE_NOT_FOUND",
            429 => "RATE_LIMIT_EXCEEDED",
            500 => "INTERNAL_ERROR",
            _ => "UNKNOWN_ERROR"
        };

        return problemDetails;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        statusCode ??= 400;

        var problemDetails = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode,
            Type = type,
            Detail = detail,
            Instance = instance,
        };

        if (title != null)
        {
            problemDetails.Title = title;
        }

        // Add custom properties for validation errors
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["validationErrorCount"] = modelStateDictionary.ErrorCount;

        return problemDetails;
    }
}
```

### Step 2: Register in DI

In `InfrastructureServiceExtensions.cs`:

```csharp
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
{
    // Register custom ProblemDetailsFactory
    services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();
    
    // ...existing code...
}
```

### ? Benefits

- **Full Control**: Control over ProblemDetails and ValidationProblemDetails creation
- **Type Safety**: Strongly-typed custom properties
- **Validation Support**: Customize validation error responses

---

## ?? Method 3: Middleware-Based Customization

**When to use**: When you need to customize based on specific conditions or business logic.

### Step 1: Create Middleware

Create a new file: `Presentation/Middleware/ProblemDetailsMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.ApiTemplate.Presentation.Middleware;

/// <summary>
/// Middleware for advanced ProblemDetails customization based on request context
/// </summary>
public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(
        RequestDelegate next,
        IProblemDetailsService problemDetailsService,
        ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            
            // Handle specific status codes
            if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
            {
                // Custom 404 handling
                var problemDetails = new ProblemDetails
                {
                    Status = 404,
                    Title = "Not Found",
                    Detail = $"The requested resource at '{context.Request.Path}' was not found.",
                    Instance = context.Request.Path
                };
                
                // Add custom properties
                problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
                problemDetails.Extensions["suggestions"] = new[]
                {
                    "Check the URL for typos",
                    "Verify the resource exists",
                    "Contact support if the issue persists"
                };
                
                await _problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = problemDetails
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in ProblemDetailsMiddleware");
            
            if (!context.Response.HasStarted)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = 500,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while processing your request."
                };
                
                // Add exception details (only in development)
                if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                {
                    problemDetails.Extensions["exceptionType"] = ex.GetType().Name;
                    problemDetails.Extensions["exceptionMessage"] = ex.Message;
                    problemDetails.Extensions["stackTrace"] = ex.StackTrace;
                }
                
                problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
                problemDetails.Extensions["traceId"] = context.TraceIdentifier;
                
                await _problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = problemDetails
                });
            }
        }
    }
}

/// <summary>
/// Extension method to register ProblemDetailsMiddleware
/// </summary>
public static class ProblemDetailsMiddlewareExtensions
{
    public static IApplicationBuilder UseProblemDetailsMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ProblemDetailsMiddleware>();
    }
}
```

### Step 2: Register Middleware

In `WebApplicationExtensions.cs` (after `app.UseRouting()`):

```csharp
app.UseRouting();

// Add custom ProblemDetails middleware (after routing, before authentication)
app.UseProblemDetailsMiddleware();

app.UseCors("AllowSpecificOrigins");
```

### ? Benefits

- **Conditional Logic**: Apply different customizations based on request context
- **Business Logic**: Incorporate business rules into error responses
- **Flexibility**: Complete control over when and how to customize

---

## ?? Method 4: Controller-Specific Customization

**When to use**: Different error formats for different controllers or endpoints.

### Example: Custom Error Response in Controller

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetDataById(string id)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        // Option 1: Use Problem() method for automatic ProblemDetails
        return Problem(
            detail: "ID parameter is required and cannot be empty.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid Input"
        );
        
        // Option 2: Create custom ProblemDetails
        var problemDetails = new ProblemDetails
        {
            Status = 400,
            Title = "Invalid Input",
            Detail = "ID parameter is required and cannot be empty.",
            Instance = HttpContext.Request.Path
        };
        
        // Add custom properties
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["parameterName"] = "id";
        problemDetails.Extensions["validationRules"] = new[]
        {
            "ID must not be null",
            "ID must not be empty",
            "ID must not be whitespace only"
        };
        
        return BadRequest(problemDetails);
    }
    
    // ...rest of implementation
}
```

---

## ?? Swagger Integration

Your customized `ProblemDetails` will **automatically appear in Swagger UI** with the additional properties.

### Before Customization

```json
{
  "type": "string",
  "title": "string",
  "status": 0,
  "detail": "string",
  "instance": "string"
}
```

### After Customization

```json
{
  "type": "string",
  "title": "string",
  "status": 0,
  "detail": "string",
  "instance": "string",
  "timestamp": "2024-01-15T10:30:00Z",
  "traceId": "string",
  "machineName": "string",
  "environment": "string",
  "apiVersion": "string",
  "path": "string",
  "method": "string",
  "userId": "string",
  "supportContact": "string",
  "documentationUrl": "string"
}
```

---

## ?? Testing Your Customizations

### 1. Test with Swagger UI

1. Run your application: `dotnet run`
2. Navigate to: `https://localhost:7000/swagger`
3. Try the `/api/v1/sample/{id}` endpoint with an invalid ID
4. Observe the customized error response

### 2. Test with cURL

```bash
# Test 400 Bad Request
curl -X GET "https://localhost:7000/api/v1/sample/" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -i

# Test 401 Unauthorized
curl -X GET "https://localhost:7000/api/v1/sample/123" -i

# Test 403 Forbidden (User token on Admin endpoint)
curl -X GET "https://localhost:7000/api/v1/sample/admin" \
  -H "Authorization: Bearer USER_TOKEN_HERE" \
  -i

# Test 404 Not Found
curl -X GET "https://localhost:7000/api/v1/nonexistent" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -i
```

### 3. Test with Postman

1. Import the CleanArchitecture.ApiTemplate collection
2. Get a token from `/api/v1/auth/token`
3. Try various endpoints with invalid inputs
4. Check the response body for custom properties

---

## ?? Recommended Approach for CleanArchitecture.ApiTemplate

**Use Method 1 (CustomizeProblemDetails)** - Already implemented! ?

This approach is:
- ? Simple to maintain
- ? Consistent across all endpoints
- ? Automatic - no changes needed in controllers
- ? Visible in Swagger UI
- ? Production-ready

---

## ?? Production Best Practices

### 1. Environment-Specific Properties

```csharp
options.CustomizeProblemDetails = ctx =>
{
    var environment = ctx.HttpContext.RequestServices
        .GetRequiredService<IWebHostEnvironment>();
    
    // Always include these
    ctx.ProblemDetails.Extensions.Add("timestamp", DateTime.UtcNow);
    ctx.ProblemDetails.Extensions.Add("traceId", ctx.HttpContext.TraceIdentifier);
    
    // Only in Development
    if (environment.IsDevelopment())
    {
        ctx.ProblemDetails.Extensions.Add("machineName", Environment.MachineName);
        ctx.ProblemDetails.Extensions.Add("environment", environment.EnvironmentName);
    }
    
    // Only in Production
    if (environment.IsProduction())
    {
        ctx.ProblemDetails.Extensions.Add("supportContact", "softevolutionsl@gmail.com");
        ctx.ProblemDetails.Extensions.Add("incidentUrl", 
            $"https://support.example.com/incident/{ctx.HttpContext.TraceIdentifier}");
    }
};
```

### 2. Security Considerations

?? **Never include sensitive information in ProblemDetails:**

- ? Stack traces (only in Development)
- ? Database connection strings
- ? Internal server paths
- ? API keys or secrets
- ? User passwords
- ? Detailed SQL queries

? **Safe to include:**

- ? Timestamp
- ? TraceId (for correlation)
- ? Status code
- ? Generic error messages
- ? Support contact information
- ? Documentation links

### 3. Logging Integration

Always log errors with the same traceId:

```csharp
options.CustomizeProblemDetails = ctx =>
{
    var traceId = ctx.HttpContext.TraceIdentifier;
    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    
    // Log the error with the same traceId
    logger.LogError("Error occurred. TraceId: {TraceId}, Status: {Status}, Path: {Path}", 
        traceId, 
        ctx.ProblemDetails.Status, 
        ctx.HttpContext.Request.Path);
    
    // Add traceId to response
    ctx.ProblemDetails.Extensions.Add("traceId", traceId);
};
```

---

## ?? Additional Resources

- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core Error Handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [Customize ProblemDetails](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling#customize-problem-details)

---

## ?? Summary

Your CleanArchitecture.ApiTemplate now has **customized ProblemDetails responses** with:

| Property | Description | Example |
|----------|-------------|---------|
| `timestamp` | When the error occurred | `"2024-01-15T10:30:00Z"` |
| `traceId` | Correlation ID for logs | `"00-abc123..."` |
| `machineName` | Server that handled the request | `"WEBSERVER01"` |
| `environment` | Development/Production | `"Development"` |
| `apiVersion` | API version | `"v1"` |
| `path` | Request path | `"/api/v1/sample/123"` |
| `method` | HTTP method | `"GET"` |
| `userId` | Authenticated user | `"testuser"` |
| `supportContact` | Support email | `"softevolutionsl@gmail.com"` |
| `documentationUrl` | Documentation link | `"https://github.com/..."` |

These customizations are:
- ? Automatically applied to all API errors
- ? Visible in Swagger UI
- ? Helpful for debugging and support
- ? Production-ready

---

**Questions?** Open an issue or contact: softevolutionsl@gmail.com