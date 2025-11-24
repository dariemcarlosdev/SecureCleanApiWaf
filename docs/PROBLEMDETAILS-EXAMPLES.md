# ProblemDetails Examples - Before & After Customization

## ?? Table of Contents

### **Quick Navigation**
1. [Overview](#-overview)
2. [Example 1: 400 Bad Request (Invalid Input)](#example-1-400-bad-request-invalid-input)
   - [Before Customization](#-before-customization)
   - [After Customization](#-after-customization)
3. [Example 2: 401 Unauthorized (Missing Token)](#example-2-401-unauthorized-missing-token)
   - [Before Customization](#-before-customization-1)
   - [After Customization](#-after-customization-1)
4. [Example 3: 403 Forbidden (Insufficient Permissions)](#example-3-403-forbidden-insufficient-permissions)
   - [Before Customization](#-before-customization-2)
   - [After Customization](#-after-customization-2)
5. [Example 4: 404 Not Found (Resource Doesn't Exist)](#example-4-404-not-found-resource-doesnt-exist)
   - [Before Customization](#-before-customization-3)
   - [After Customization](#-after-customization-3)
6. [Example 5: 429 Too Many Requests (Rate Limit Exceeded)](#example-5-429-too-many-requests-rate-limit-exceeded)
   - [Before Customization](#-before-customization-4)
   - [After Customization](#-after-customization-4)
7. [Example 6: 500 Internal Server Error (Unhandled Exception)](#example-6-500-internal-server-error-unhandled-exception)
   - [Before Customization](#-before-customization-5)
   - [After Customization](#-after-customization-5)
8. [Comparison Table](#-comparison-table)
9. [How to Test](#-how-to-test)
   - [1. Test in Swagger UI](#1-test-in-swagger-ui)
   - [2. Test with cURL](#2-test-with-curl)
   - [3. Test with Postman](#3-test-with-postman)
   - [4. Check Swagger Schema](#4-check-swagger-schema)
10. [Benefits of Customization](#-benefits-of-customization)
    - [For Developers](#for-developers)
    - [For Support Teams](#for-support-teams)
    - [For Clients/Frontend](#for-clientsfrontend)
11. [Security Considerations](#-security-considerations)
    - [Safe to Include](#-safe-to-include-already-added)
    - [Never Include](#-never-include)
    - [Environment-Specific Data](#-environment-specific-data)
12. [Production Recommendations](#-production-recommendations)
    - [1. Remove Development-Only Properties](#1-remove-development-only-properties)
    - [2. Log All Errors](#2-log-all-errors)
13. [Summary](#-summary)
14. [Contact](#contact)

---

## ?? Overview

This document shows real examples of how your API error responses look **before** and **after** the ProblemDetails customization.

---

## Example 1: 400 Bad Request (Invalid Input)

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "traceId": "00-abc123def456-xyz789-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request - Invalid Input",
  "status": 400,
  "timestamp": "2024-01-15T10:30:00.123Z",
  "traceId": "00-abc123def456-xyz789-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/sample/",
  "method": "GET",
  "userId": "testuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

---

## Example 2: 401 Unauthorized (Missing Token)

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-def456abc123-uvw456-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized - Authentication Required",
  "status": 401,
  "timestamp": "2024-01-15T10:31:15.456Z",
  "traceId": "00-def456abc123-uvw456-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/sample/123",
  "method": "GET",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

**Note**: No `userId` property since the user is not authenticated.

---

## Example 3: 403 Forbidden (Insufficient Permissions)

### Scenario
User token trying to access admin-only endpoint

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "traceId": "00-ghi789jkl012-mno345-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden - Insufficient Permissions",
  "status": 403,
  "timestamp": "2024-01-15T10:32:30.789Z",
  "traceId": "00-ghi789jkl012-mno345-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/sample/admin",
  "method": "GET",
  "userId": "testuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

---

## Example 4: 404 Not Found (Resource Doesn't Exist)

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-pqr345stu678-vwx901-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found - Resource Does Not Exist",
  "status": 404,
  "timestamp": "2024-01-15T10:33:45.012Z",
  "traceId": "00-pqr345stu678-vwx901-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/nonexistent",
  "method": "GET",
  "userId": "adminuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

---

## Example 5: 429 Too Many Requests (Rate Limit Exceeded)

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "traceId": "00-yza012bcd345-efg678-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests - Rate Limit Exceeded",
  "status": 429,
  "timestamp": "2024-01-15T10:34:50.234Z",
  "traceId": "00-yza012bcd345-efg678-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/sample",
  "method": "GET",
  "userId": "testuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

---

## Example 6: 500 Internal Server Error (Unhandled Exception)

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-hij234klm567-nop890-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error - Something Went Wrong",
  "status": 500,
  "timestamp": "2024-01-15T10:35:55.567Z",
  "traceId": "00-hij234klm567-nop890-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/sample/123",
  "method": "POST",
  "userId": "adminuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki"
}
```

---

## Example 7: 302 Found (Redirect)

### Scenario
User requested a resource that is temporarily located at a different URI.

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.3.3",
  "title": "Found",
  "status": 302,
  "traceId": "00-redirect123-abc456-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.3.3",
  "title": "Found - Resource Moved Temporarily",
  "status": 302,
  "timestamp": "2024-01-15T10:36:30.789Z",
  "traceId": "00-redirect123-abc456-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/old-route",
  "method": "POST",
  "userId": "adminuser",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki",
  "redirectUrl": "/api/v1/new-route"
}
```

**Note**: This example includes a custom `redirectUrl` property to indicate the new location of the resource.

---

## Example 8: 503 Service Unavailable (Temporary Overload)

### Scenario
The server is temporarily unable to handle the request.

### ? Before Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.7",
  "title": "Service Unavailable",
  "status": 503,
  "traceId": "00-overload123-efg456-00"
}
```

### ? After Customization

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.7",
  "title": "Service Unavailable - Try Again Later",
  "status": 503,
  "timestamp": "2024-01-15T10:37:45.012Z",
  "traceId": "00-overload123-efg456-00",
  "machineName": "WEBSERVER01",
  "environment": "Development",
  "apiVersion": "v1",
  "path": "/api/v1/endpoint",
  "method": "GET",
  "supportContact": "softevolutionsl@gmail.com",
  "documentationUrl": "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki",
  "retryAfter": "120"
}
```

**Note**: This example includes a custom `retryAfter` property to suggest when the client can retry the request.

---

## ?? Comparison Table

| Property | Before | After |
|----------|--------|-------|
| `type` | ? Included | ? Included |
| `title` | ? Generic | ? **Customized & User-Friendly** |
| `status` | ? Included | ? Included |
| `traceId` | ? Included | ? Included |
| `timestamp` | ? Missing | ? **Added** |
| `machineName` | ? Missing | ? **Added** |
| `environment` | ? Missing | ? **Added** |
| `apiVersion` | ? Missing | ? **Added** |
| `path` | ? Missing | ? **Added** |
| `method` | ? Missing | ? **Added** |
| `userId` | ? Missing | ? **Added (if authenticated)** |
| `supportContact` | ? Missing | ? **Added** |
| `documentationUrl` | ? Missing | ? **Added** |
| `redirectUrl` | ? Missing | ? **Added (if applicable)** |
| `retryAfter` | ? Missing | ? **Added (if applicable)** |

---

## ?? How to Test

### 1. Test in Swagger UI

1. Run the app: `dotnet run`
2. Open: `https://localhost:7000/swagger`
3. Try any endpoint with invalid data
4. Check the response in the "Response body" section

### 2. Test with cURL

```bash
# Test 400 Bad Request (empty ID)
curl -X GET "https://localhost:7000/api/v1/sample/" \
  -H "Authorization: Bearer $(curl -s -X GET 'https://localhost:7000/api/v1/auth/token?type=user' | jq -r '.token')" \
  -i

# Test 401 Unauthorized (no token)
curl -X GET "https://localhost:7000/api/v1/sample/123" -i

# Test 403 Forbidden (user token on admin endpoint)
curl -X GET "https://localhost:7000/api/v1/sample/admin" \
  -H "Authorization: Bearer $(curl -s -X GET 'https://localhost:7000/api/v1/auth/token?type=user' | jq -r '.token')" \
  -i

# Test 404 Not Found (non-existent endpoint)
curl -X GET "https://localhost:7000/api/v1/nonexistent" \
  -H "Authorization: Bearer $(curl -s -X GET 'https://localhost:7000/api/v1/auth/token?type=user' | jq -r '.token')" \
  -i

# Test 302 Found (redirect)
curl -X POST "https://localhost:7000/api/v1/old-route" \
  -H "Authorization: Bearer $(curl -s -X GET 'https://localhost:7000/api/v1/auth/token?type=admin' | jq -r '.token')" \
  -i

# Test 503 Service Unavailable (try again later)
curl -X GET "https://localhost:7000/api/v1/endpoint" \
  -H "Authorization: Bearer $(curl -s -X GET 'https://localhost:7000/api/v1/auth/token?type=user' | jq -r '.token')" \
  -i
```

### 3. Test with Postman

1. **Collection**: Import CleanArchitecture.ApiTemplate collection
2. **Get Token**: Call `GET /api/v1/auth/token?type=user`
3. **Test Endpoints**: Try various endpoints with:
   - Missing required parameters
   - Invalid authentication
   - Insufficient permissions
   - Non-existent resources

### 4. Check Swagger Schema

The customized `ProblemDetails` schema will appear in Swagger under **Schemas** section:

**Location**: Scroll down to "Schemas" in Swagger UI ? Find "ProblemDetails"

You'll see all your custom properties listed!

---

## ?? Benefits of Customization

### For Developers
- ? **Better Debugging**: `traceId` correlates with application logs
- ? **Environment Awareness**: Know which environment the error came from
- ? **Request Context**: See the exact path and method that failed
- ? **User Context**: Identify which user experienced the error

### For Support Teams
- ? **Quick Triage**: `timestamp` and `traceId` for log correlation
- ? **User Identification**: `userId` to identify affected users
- ? **Contact Info**: `supportContact` for escalation
- ? **Documentation**: `documentationUrl` for troubleshooting guides

### For Clients/Frontend
- ? **User-Friendly Titles**: Clear error messages
- ? **Support Links**: Direct links to documentation and support
- ? **Consistent Format**: All errors have the same structure
- ? **Detailed Context**: More information for error handling

---

## ?? Security Considerations

### ? Safe to Include (Already Added)
- Timestamp
- TraceId (correlation only)
- Status code
- Request path (no sensitive data)
- HTTP method
- Support contact
- Documentation URL
- User ID (username only, not sensitive data)

### ? Never Include
- ? Stack traces (only in Development)
- ? Database queries
- ? Connection strings
- ? API keys
- ? Passwords
- ? Internal server paths
- ? Sensitive business data

### ?? Environment-Specific Data

Some properties like `machineName` and `environment` are **only useful in Development**. In production, consider removing or limiting these.

---

## ?? Production Recommendations

### 1. Remove Development-Only Properties

For **production**, modify `PresentationServiceExtensions.cs`:

```csharp
options.CustomizeProblemDetails = ctx =>
{
    var environment = ctx.HttpContext.RequestServices
        .GetRequiredService<IWebHostEnvironment>();
    
    // Always include these (safe for production)
    ctx.ProblemDetails.Extensions.Add("timestamp", DateTime.UtcNow);
    ctx.ProblemDetails.Extensions.Add("traceId", ctx.HttpContext.TraceIdentifier);
    ctx.ProblemDetails.Extensions.Add("supportContact", "softevolutionsl@gmail.com");
    ctx.ProblemDetails.Extensions.Add("documentationUrl", 
        "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/wiki");
    
    // Only in Development
    if (environment.IsDevelopment())
    {
        ctx.ProblemDetails.Extensions.Add("machineName", Environment.MachineName);
        ctx.ProblemDetails.Extensions.Add("environment", environment.EnvironmentName);
        ctx.ProblemDetails.Extensions.Add("path", ctx.HttpContext.Request.Path.Value);
        ctx.ProblemDetails.Extensions.Add("method", ctx.HttpContext.Request.Method);
    }
};
```

### 2. Log All Errors

Always log errors with the same `traceId` for correlation:

```csharp
_logger.LogError("Error occurred. TraceId: {TraceId}, Status: {Status}, User: {UserId}",
    context.TraceIdentifier,
    problemDetails.Status,
    context.User?.Identity?.Name);
```

---

## ?? Summary

Your API now returns **enriched error responses** with:

| Improvement | Benefit |
|-------------|---------|
| **Timestamps** | Track when errors occurred |
| **TraceIds** | Correlate with application logs |
| **User Context** | Identify affected users |
| **Request Context** | See exact path and method |
| **Custom Titles** | User-friendly error messages |
| **Support Info** | Direct contact and documentation links |

These customizations make your API:
- ? **Easier to debug**
- ? **Easier to support**
- ? **Easier to monitor**
- ? **Better for clients**

---

## Contact

For questions, open an issue or contact the maintainer at softevolutionsl@gmail.com or via GitHub: https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate

**Last Updated:** 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)

*Documentation created: November 2025*  
*For: CleanArchitecture.ApiTemplate - ProblemDetails Examples Guide*