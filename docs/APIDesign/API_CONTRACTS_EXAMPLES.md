# API Contracts Examples - SecureCleanApiWaf

> "Well-defined API contracts are the foundation of successful collaboration between backend and frontend teams—clear examples drive efficient development and seamless integration."

## Overview

This document provides practical examples of API contracts used in SecureCleanApiWaf, along with collaboration tips for backend and UI developers. These examples demonstrate how to define clear, consistent, and testable API contracts that facilitate smooth integration between different layers of the application.

---

## Table of Contents

1. [User Management Contracts](#user-management-contracts)
2. [Authentication Contracts](#authentication-contracts)
3. [Data Retrieval Contracts](#data-retrieval-contracts)
4. [File Upload Contracts](#file-upload-contracts)
5. [Pagination Contracts](#pagination-contracts)
6. [Error Handling Contracts](#error-handling-contracts)
7. [Collaboration Best Practices](#collaboration-best-practices)
8. [Testing API Contracts](#testing-api-contracts)
9. [Blazor Integration Examples](#blazor-integration-examples)

---

## User Management Contracts

### **Create User**

**Endpoint:** `POST /api/v1/users`

**Request Contract:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "P@ssw0rd123!",
  "role": "User",
  "dateOfBirth": "1990-05-15",
  "preferences": {
    "notifications": true,
    "theme": "dark"
  }
}
```

**Success Response (201 Created):**
```json
{
  "id": 101,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "User",
  "dateOfBirth": "1990-05-15",
  "preferences": {
    "notifications": true,
    "theme": "dark"
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Validation Error Response (400 Bad Request):**
```json
{
  "errors": {
    "Name": ["Name is required", "Name must be between 2 and 100 characters"],
    "Email": ["Invalid email format"],
    "Password": ["Password must be at least 8 characters", "Password must contain uppercase, lowercase, number and special character"]
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### **Update User**

**Endpoint:** `PUT /api/v1/users/{id}`

**Request Contract:**
```json
{
  "name": "John Updated Doe",
  "email": "john.updated@example.com",
  "role": "Admin",
  "preferences": {
    "notifications": false,
    "theme": "light"
  }
}
```

**Success Response (200 OK):**
```json
{
  "id": 101,
  "name": "John Updated Doe",
  "email": "john.updated@example.com",
  "role": "Admin",
  "dateOfBirth": "1990-05-15",
  "preferences": {
    "notifications": false,
    "theme": "light"
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T11:45:00Z"
}
```

---

## Authentication Contracts

### **User Login**

**Endpoint:** `POST /api/v1/auth/login`

**Request Contract:**
```json
{
  "email": "john.doe@example.com",
  "password": "P@ssw0rd123!",
  "rememberMe": true
}
```

**Success Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "def502004b8c9f7e2a...",
  "expiresIn": 3600,
  "user": {
    "id": 101,
    "name": "John Doe",
    "email": "john.doe@example.com",
    "role": "User"
  }
}
```

**Authentication Failure (401 Unauthorized):**
```json
{
  "error": "Invalid email or password",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### **Refresh Token**

**Endpoint:** `POST /api/v1/auth/refresh`

**Request Contract:**
```json
{
  "refreshToken": "def502004b8c9f7e2a..."
}
```

**Success Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123456789def...",
  "expiresIn": 3600
}
```

---

## Data Retrieval Contracts

### **Get All Users (Paginated)**

**Endpoint:** `GET /api/v1/users?page=1&pageSize=10&search=john&sortBy=name&order=asc`

**Success Response (200 OK):**
```json
{
  "data": [
    {
      "id": 101,
      "name": "John Doe",
      "email": "john.doe@example.com",
      "role": "User",
      "createdAt": "2024-01-15T10:30:00Z"
    },
    {
      "id": 102,
      "name": "John Smith",
      "email": "john.smith@example.com",
      "role": "Admin",
      "createdAt": "2024-01-14T09:15:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalRecords": 25,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  },
  "filters": {
    "search": "john",
    "sortBy": "name",
    "order": "asc"
  }
}
```

### **Get User by ID**

**Endpoint:** `GET /api/v1/users/{id}`

**Success Response (200 OK):**
```json
{
  "id": 101,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "User",
  "dateOfBirth": "1990-05-15",
  "preferences": {
    "notifications": true,
    "theme": "dark"
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Not Found Response (404 Not Found):**
```json
{
  "error": "User with ID 999 not found",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## File Upload Contracts

### **Upload Profile Picture**

**Endpoint:** `POST /api/v1/users/{id}/profile-picture`

**Request Contract (multipart/form-data):**
```
Content-Type: multipart/form-data

file: [binary data]
description: "Updated profile picture"
```

**Success Response (200 OK):**
```json
{
  "fileId": "abc123-def456-ghi789",
  "fileName": "profile.jpg",
  "fileSize": 1024576,
  "contentType": "image/jpeg",
  "url": "https://api.example.com/files/abc123-def456-ghi789",
  "uploadedAt": "2024-01-15T10:30:00Z"
}
```

**File Validation Error (400 Bad Request):**
```json
{
  "errors": {
    "File": ["File is required", "File size must not exceed 5MB", "Only image files are allowed"]
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## Pagination Contracts

### **Standard Pagination Response Structure**

All paginated endpoints follow this consistent structure:

```json
{
  "data": [...], // Array of resources
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalRecords": 100,
    "totalPages": 10,
    "hasNext": true,
    "hasPrevious": false,
    "firstPage": 1,
    "lastPage": 10
  },
  "filters": {
    // Applied filters/query parameters
    "search": "searchTerm",
    "sortBy": "fieldName",
    "order": "asc"
  }
}
```

### **Empty Results Response**

```json
{
  "data": [],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalRecords": 0,
    "totalPages": 0,
    "hasNext": false,
    "hasPrevious": false,
    "firstPage": 1,
    "lastPage": 1
  },
  "filters": {
    "search": "nonexistent"
  }
}
```

---

## Error Handling Contracts

### **Validation Errors (400 Bad Request)**

```json
{
  "errors": {
    "FieldName1": ["Error message 1", "Error message 2"],
    "FieldName2": ["Error message 3"],
    "NestedObject.Property": ["Nested validation error"]
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### **Authentication Error (401 Unauthorized)**

```json
{
  "error": "Authentication required. Please provide a valid token.",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### **Authorization Error (403 Forbidden)**

```json
{
  "error": "Insufficient permissions to perform this action.",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### **Server Error (500 Internal Server Error)**

```json
{
  "error": "An unexpected error occurred. Please try again later.",
  "requestId": "abc-123-def-456",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### **Rate Limit Error (429 Too Many Requests)**

```json
{
  "error": "Rate limit exceeded. Please try again in 60 seconds.",
  "retryAfter": 60,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## Collaboration Best Practices

### **1. Contract-First Development**

- **Define contracts before implementation**: Both teams agree on request/response structures
- **Use OpenAPI/Swagger**: Generate documentation and client code from specifications
- **Version your APIs**: Use semantic versioning for breaking changes

### **2. Naming Conventions**

**Field Names:**
- Use **camelCase** for JSON properties
- Be **descriptive** and **consistent**
- Avoid abbreviations unless universally understood

```json
// ? Good
{
  "firstName": "John",
  "lastName": "Doe",
  "createdAt": "2024-01-15T10:30:00Z"
}

// ? Bad
{
  "fName": "John",
  "l_name": "Doe",
  "created": "2024-01-15T10:30:00Z"
}
```

### **3. Data Types and Formats**

**Dates:** Always use ISO 8601 format (`YYYY-MM-DDTHH:mm:ssZ`)
**IDs:** Use consistent type (string UUIDs or numeric)
**Booleans:** Use explicit `true`/`false` values
**Enums:** Use string values for readability

### **4. Mock Data Strategy**

**Backend Team:**
- Provide sample JSON responses for all endpoints
- Include edge cases and error scenarios
- Use consistent test data across examples

**UI Team:**
- Create mock services that return sample data
- Test UI components with various response scenarios
- Validate form validation with error responses

---

## Testing API Contracts

### **Contract Testing Checklist**

**For Backend Developers:**
- [ ] All required fields are validated
- [ ] Optional fields work correctly
- [ ] Error responses match documented format
- [ ] HTTP status codes are appropriate
- [ ] Response timing meets SLA requirements

**For UI Developers:**
- [ ] Forms handle all validation errors gracefully
- [ ] Loading states work with async operations
- [ ] Error messages are user-friendly
- [ ] Pagination controls function correctly
- [ ] File uploads show progress and handle errors

### **Testing Tools**

**Backend Testing:**
- **Postman/Insomnia**: Manual API testing
- **Newman**: Automated Postman collections
- **Unit Tests**: Validate business logic
- **Integration Tests**: End-to-end API testing

**Frontend Testing:**
- **Mock Service Worker (MSW)**: API mocking
- **Cypress/Playwright**: End-to-end testing
- **React/Blazor Testing Library**: Component testing
- **Storybook**: Component documentation and testing

---

## Blazor Integration Examples

### **HTTP Client Service for Blazor**

```csharp
public interface IUserApiService
{
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(UserSearchRequest request);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(int id);
}

public class UserApiService : IUserApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserApiService> _logger;

    public UserApiService(HttpClient httpClient, ILogger<UserApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(UserSearchRequest request)
    {
        try
        {
            var queryString = BuildQueryString(request);
            var response = await _httpClient.GetAsync($"/api/v1/users{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PagedResult<UserDto>>(content, JsonOptions);
                return ApiResponse<PagedResult<UserDto>>.Success(result);
            }
            
            var error = await response.Content.ReadAsStringAsync();
            return ApiResponse<PagedResult<UserDto>>.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return ApiResponse<PagedResult<UserDto>>.Failure("An error occurred while retrieving users");
        }
    }
}
```

### **Blazor Component Example**

```razor
@page "/users"
@inject IUserApiService UserApi
@inject IJSRuntime JSRuntime

<PageTitle>Users</PageTitle>

<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            <h3>User Management</h3>
            
            @if (loading)
            {
                <div class="text-center">
                    <div class="spinner-border" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
            }
            else if (error != null)
            {
                <div class="alert alert-danger" role="alert">
                    <strong>Error:</strong> @error
                </div>
            }
            else
            {
                <UserGrid Users="users" 
                         Pagination="pagination" 
                         OnPageChanged="HandlePageChanged"
                         OnUserEdit="HandleUserEdit"
                         OnUserDelete="HandleUserDelete" />
            }
        </div>
    </div>
</div>

@code {
    private List<UserDto> users = new();
    private PaginationDto? pagination;
    private bool loading = true;
    private string? error;
    
    private UserSearchRequest searchRequest = new()
    {
        Page = 1,
        PageSize = 10
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        loading = true;
        error = null;
        
        var response = await UserApi.GetUsersAsync(searchRequest);
        
        if (response.Success && response.Data != null)
        {
            users = response.Data.Data;
            pagination = response.Data.Pagination;
        }
        else
        {
            error = response.Error ?? "Failed to load users";
        }
        
        loading = false;
    }

    private async Task HandlePageChanged(int page)
    {
        searchRequest.Page = page;
        await LoadUsers();
    }

    private async Task HandleUserEdit(int userId)
    {
        // Navigate to edit user page or show modal
    }

    private async Task HandleUserDelete(int userId)
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this user?"))
        {
            var response = await UserApi.DeleteUserAsync(userId);
            
            if (response.Success)
            {
                await LoadUsers(); // Refresh the list
            }
            else
            {
                error = response.Error ?? "Failed to delete user";
            }
        }
    }
}
```

### **Error Handling in Blazor**

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public static ApiResponse<T> Success(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ApiResponse<T> Failure(string error) => new()
    {
        Success = false,
        Error = error
    };

    public static ApiResponse<T> ValidationFailure(Dictionary<string, string[]> errors) => new()
    {
        Success = false,
        ValidationErrors = errors,
        Error = "Validation failed"
    };
}
```

---

## Sample DTOs and Models

### **User DTOs**

```csharp
// Request DTOs
public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
    
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
    
    public UserPreferencesDto? Preferences { get; set; }
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
    public UserPreferencesDto? Preferences { get; set; }
}

public class UserSearchRequest
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? SortBy { get; set; }
    public string Order { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// Response DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public UserPreferencesDto? Preferences { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserPreferencesDto
{
    public bool Notifications { get; set; }
    public string Theme { get; set; } = "light";
}
```

### **Pagination DTOs**

```csharp
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
    public Dictionary<string, object>? Filters { get; set; }
}

public class PaginationDto
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
    public int FirstPage { get; set; } = 1;
    public int LastPage { get; set; }
}
```

---

## API Documentation Tools

### **OpenAPI/Swagger Configuration**

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SecureCleanApiWaf API",
        Version = "v1",
        Description = "RESTful API for SecureCleanApiWaf",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@SecureCleanApiWaf.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Add JWT authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});
```

---

## References and Resources

### **Documentation Tools**
- [OpenAPI Specification](https://swagger.io/specification/) - Industry standard for API documentation
- [Postman Collections](https://www.postman.com/) - API testing and documentation
- [Insomnia](https://insomnia.rest/) - REST client for API testing

### **Testing Tools**
- [MSW (Mock Service Worker)](https://mswjs.io/) - API mocking for development and testing
- [WireMock](http://wiremock.org/) - Mock server for integration testing
- [Pact](https://pact.io/) - Contract testing framework

### **Related Documentation**
- [`API_DESIGN_GUIDE.md`](./API_DESIGN_GUIDE.md) - Complete API design principles and patterns
- [`../API-SECURITY-IMPLEMENTATION-GUIDE.md`](../API-SECURITY-IMPLEMENTATION-GUIDE.md) - Security implementation guide
- [`../TEST_AUTHENTICATION_GUIDE.md`](../AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md) - Authentication testing strategies

---

## ?? Contact

**Need Help with API Contracts?**

- ?? **Documentation:** Refer to the main API Design Guide
- ?? **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
- ?? **Email:** softevolutionsl@gmail.com
- ?? **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)
