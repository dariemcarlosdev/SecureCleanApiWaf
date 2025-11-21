# SecureCleanApiWaf.Web Project (Presentation Layer)

> *"The user interface is the part of the system that the user sees and interacts with. Make it clean, make it intuitive, make it delightful."*  
> � **Martin Fowler**, Software Architecture and Design

---

**📚 New to Clean Architecture or DDD?**  
Read **[Architecture Patterns Explained](../ARCHITECTURE_PATTERNS_EXPLAINED.md)** first to understand how Clean Architecture and Domain-Driven Design work together in this project.

---

## 📖 Overview
The **Presentation Layer** (Web project) is the entry point for users and external systems. This layer contains Blazor Server components, API controllers, middleware, and all UI/API concerns. It orchestrates the Application layer to fulfill user requests.

---

## 🎯 Purpose
- Serve Blazor Server UI for interactive web applications
- Expose REST API endpoints for external consumption
- Handle HTTP requests and responses
- Configure middleware pipeline
- Manage authentication and authorization
- Implement API versioning
- Configure Swagger/OpenAPI documentation
- Handle dependency injection registration

---

## 📁 Project Structure

```
SecureCleanApiWaf.Web/
+-- Components/                       # Blazor components
�   +-- Layout/
�   �   +-- MainLayout.razor         # Main application layout
�   �   +-- NavMenu.razor            # Navigation menu
�   �   +-- MainLayout.razor.css     # Layout styles
�   �
�   +-- Pages/                        # Routable page components
�   �   +-- Home.razor               # Home page
�   �   +-- SampleData.razor         # Data display page
�   �   +-- Error.razor              # Error page
�   �   +-- _Imports.razor           # Component imports
�   �
�   +-- Shared/                       # Reusable components
�   �   +-- LoadingSpinner.razor
�   �   +-- ErrorMessage.razor
�   �   +-- DataTable.razor
�   �
�   +-- App.razor                     # Root component
�   +-- Routes.razor                  # Routing configuration
�
+-- Controllers/                      # REST API controllers
�   +-- v1/
�   �   +-- SampleDataController.cs  # Sample data endpoints
�   �   +-- HealthController.cs      # Health check endpoints
�   +-- v2/
�       +-- SampleDataController.cs  # v2 endpoints (breaking changes)
�
+-- Extensions/                       # Startup configuration extensions
�   +-- WebApplicationExtensions.cs  # HTTP pipeline configuration
�   +-- ServiceCollectionExtensions.cs # DI registration
�
+-- Middleware/                       # Custom middleware
�   +-- ExceptionHandlingMiddleware.cs
�   +-- RequestLoggingMiddleware.cs
�   +-- ApiKeyAuthenticationMiddleware.cs
�
+-- Models/                           # Request/Response models (API contracts)
�   +-- Requests/
�   �   +-- CreateSampleRequest.cs
�   �   +-- UpdateSampleRequest.cs
�   +-- Responses/
�       +-- SampleDataResponse.cs
�       +-- ErrorResponse.cs
�
+-- wwwroot/                          # Static files
�   +-- css/
�   �   +-- app.css                  # Global styles
�   �   +-- bootstrap/               # Bootstrap files
�   +-- js/
�   �   +-- site.js                  # JavaScript files
�   +-- favicon.ico
�
+-- appsettings.json                  # Configuration (non-sensitive)
+-- appsettings.Development.json      # Development configuration
+-- appsettings.Production.json       # Production configuration
+-- Program.cs                        # Application entry point
+-- SecureCleanApiWaf.Web.csproj

```

---

## 📖 Key Components

### 1. **Program.cs (Application Entry Point)**

```csharp
using SecureCleanApiWaf.Application;
using SecureCleanApiWaf.Infrastructure;
using SecureCleanApiWaf.Infrastructure.Azure;
using SecureCleanApiWaf.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
// Add Azure Key Vault (production)
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault();
}

// Add Azure App Configuration (optional)
// builder.Configuration.AddAzureAppConfiguration();

// ===== Services Registration =====
// Add layer services (Clean Architecture layers)
builder.Services.AddApplication();           // Application layer (CQRS, MediatR)
builder.Services.AddInfrastructure(builder.Configuration);  // Infrastructure layer
builder.Services.AddAzureInfrastructure(builder.Configuration); // Azure-specific

// Add Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add API Controllers with versioning
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "SecureClean API",
        Description = "SecureCleanApiWaf REST API for sample data management",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "softevolutionsl@gmail.com",
            Url = new Uri("https://github.com/dariemcarlosdev/SecureCleanApiWaf")
        }
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddUrlGroup(new Uri(builder.Configuration["ThirdPartyApi:BaseUrl"]), "Third Party API");

// Add CORS (if needed for API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== Middleware Pipeline =====
var app = builder.Build();

// Exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecureClean API v1"));
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();
app.UseAntiforgery();

// CORS (for API)
app.UseCors("AllowSpecificOrigins");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapControllers();              // API endpoints
app.MapHealthChecks("/health");    // Health check endpoint
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
```

---

### 2. **API Controller Example**

```csharp
/// <summary>
/// Sample Data API Controller (v1)
/// Demonstrates REST API design with MediatR and CQRS
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class SampleDataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SampleDataController> _logger;
    
    public SampleDataController(
        IMediator mediator,
        ILogger<SampleDataController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all sample data
    /// </summary>
    /// <returns>List of sample data</returns>
    /// <response code="200">Returns the list of sample data</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<SampleDataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all sample data");
        
        var query = new GetApiDataQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        if (!result.Success)
        {
            _logger.LogError("Failed to retrieve data: {Error}", result.Error);
            return StatusCode(500, new ErrorResponse { Message = result.Error });
        }
        
        return Ok(result.Data);
    }
    
    /// <summary>
    /// Get sample data by ID
    /// </summary>
    /// <param name="id">The sample data identifier</param>
    /// <returns>Sample data item</returns>
    /// <response code="200">Returns the sample data item</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SampleDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sample data by ID: {Id}", id);
        
        var query = new GetApiDataByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);
        
        if (!result.Success)
        {
            _logger.LogError("Failed to retrieve data by ID {Id}: {Error}", id, result.Error);
            
            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ErrorResponse { Message = result.Error });
            }
            
            return StatusCode(500, new ErrorResponse { Message = result.Error });
        }
        
        return Ok(result.Data);
    }
    
    /// <summary>
    /// Create new sample data
    /// </summary>
    /// <param name="request">The create request</param>
    /// <returns>Created sample data ID</returns>
    /// <response code="201">Item created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSampleRequest request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating sample data: {Name}", request.Name);
        
        var command = new CreateSampleDataCommand
        {
            Name = request.Name,
            Description = request.Description
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.Success)
        {
            _logger.LogError("Failed to create data: {Error}", result.Error);
            return BadRequest(new ErrorResponse { Message = result.Error });
        }
        
        return CreatedAtAction(
            nameof(GetById), 
            new { id = result.Data }, 
            new { id = result.Data });
    }
}
```

---

### 3. **Blazor Component Example**

```razor
@page "/sample-data"
@using SecureCleanApiWaf.Application.Features.SampleData.Queries
@using MediatR
@inject IMediator Mediator
@inject ILogger<SampleData> Logger

<PageTitle>Sample Data</PageTitle>

<h1>Sample Data</h1>

@if (isLoading)
{
    <LoadingSpinner />
}
else if (errorMessage != null)
{
    <ErrorMessage Message="@errorMessage" />
}
else if (data != null)
{
    <div class="table-responsive">
        <table class="table table-striped">
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Name</th>
                    <th>Description</th>
                    <th>Created At</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in data)
                {
                    <tr>
                        <td>@item.Id</td>
                        <td>@item.Name</td>
                        <td>@item.Description</td>
                        <td>@item.CreatedAt.ToString("yyyy-MM-dd HH:mm")</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}

@code {
    private List<SampleDataDto>? data;
    private bool isLoading = true;
    private string? errorMessage;
    
    protected override async Task OnInitializedAsync()
    {
        try
        {
            Logger.LogInformation("Loading sample data");
            
            var query = new GetApiDataQuery();
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                data = result.Data;
            }
            else
            {
                errorMessage = result.Error;
                Logger.LogError("Failed to load sample data: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            errorMessage = "An unexpected error occurred";
            Logger.LogError(ex, "Error loading sample data");
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

---

### 4. **Custom Middleware**

#### **Exception Handling Middleware**
```csharp
/// <summary>
/// Global exception handling middleware
/// Catches unhandled exceptions and returns consistent error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        var response = new ErrorResponse
        {
            Message = exception.Message,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow
        };
        
        return context.Response.WriteAsJsonAsync(response);
    }
}
```

#### **Request Logging Middleware**
```csharp
/// <summary>
/// Middleware for logging all HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation(
            "HTTP {Method} {Path} started at {StartTime}",
            context.Request.Method,
            context.Request.Path,
            startTime);
        
        await _next(context);
        
        var duration = DateTime.UtcNow - startTime;
        
        _logger.LogInformation(
            "HTTP {Method} {Path} completed with {StatusCode} in {Duration}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            duration.TotalMilliseconds);
    }
}
```

---

### 5. **API Request/Response Models**

```csharp
/// <summary>
/// Request model for creating sample data
/// </summary>
public class CreateSampleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(500)]
    public string Description { get; set; }
}

/// <summary>
/// Response model for sample data
/// </summary>
public class SampleDataResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

---

## 📦 Dependencies

### **NuGet Packages**
```xml
<ItemGroup>
  <!-- Blazor Server -->
  <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
  
  <!-- API Versioning -->
  <PackageReference Include="Asp.Versioning.Mvc" Version="8.0.0" />
  <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.0.0" />
  
  <!-- Swagger/OpenAPI -->
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  
  <!-- Health Checks -->
  <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.0" />
  
  <!-- Serilog (optional structured logging) -->
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
</ItemGroup>

<!-- Project References -->
<ItemGroup>
  <ProjectReference Include="..\SecureCleanApiWaf.Application\SecureCleanApiWaf.Application.csproj" />
  <ProjectReference Include="..\SecureCleanApiWaf.Infrastructure\SecureCleanApiWaf.Infrastructure.csproj" />
  <ProjectReference Include="..\SecureCleanApiWaf.Infrastructure.Azure\SecureCleanApiWaf.Infrastructure.Azure.csproj" />
</ItemGroup>
```

---

## ⚙️ Configuration Files

### **appsettings.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SecureCleanApiWafDb;Trusted_Connection=True;"
  },
  "ThirdPartyApi": {
    "BaseUrl": "https://api.example.com/"
  },
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

### **appsettings.Development.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SecureCleanApiWafDb_Dev;Trusted_Connection=True;"
  },
  "ThirdPartyApi": {
    "BaseUrl": "https://dev-api.example.com/"
  }
}
```

---

## 🧪 Testing Strategy

### **Functional Tests**
```csharp
public class SampleDataControllerFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public SampleDataControllerFunctionalTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/sampledata");
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
    
    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateSampleRequest
        {
            Name = "Test",
            Description = "Test Description"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sampledata", request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }
}
```

---

## ✅ Presentation Layer Checklist

- [ ] Blazor Server configured for interactive components
- [ ] API controllers with versioning
- [ ] Swagger/OpenAPI documentation
- [ ] Custom middleware for logging and error handling
- [ ] Health check endpoints
- [ ] CORS configured (if needed)
- [ ] Authentication and authorization
- [ ] Request/response models separate from domain
- [ ] Proper HTTP status codes
- [ ] XML comments for API documentation

---

## ✅ Best Practices

### ? DO
- Use MediatR to decouple controllers from handlers
- Return appropriate HTTP status codes
- Validate input with data annotations + FluentValidation
- Use DTOs for API contracts (don't expose domain entities)
- Implement global exception handling
- Log all requests and responses
- Use API versioning
- Document APIs with Swagger
- Keep components small and focused

### ? DON'T
- Put business logic in controllers or components
- Return domain entities directly
- Skip validation
- Hard-code configuration values
- Expose internal errors to clients
- Create god controllers
- Mix UI and API concerns without separation

---

## 📖 Migration from Current Structure

```
Current Structure 🏛️ Clean Architecture Web Layer

Components/                    ? Web/Components/
Controllers/SampleController.cs ? Web/Controllers/v1/SampleDataController.cs
Extensions/                    ? Web/Extensions/
Program.cs                     ? Web/Program.cs (refactored)
Models/SampleModel.cs          ? Web/Models/Responses/SampleDataResponse.cs
```

---

## 📝 Summary

The Presentation Layer:
- **Entry point** for HTTP requests
- **Orchestrates** Application layer via MediatR
- **Returns** HTTP responses with appropriate status codes
- **Implements** middleware pipeline
- **Configures** DI and services
- **Remains thin** (no business logic)

This layer is the **interface** to your application, keeping concerns separated and maintainable.
