using System;
using SecureCleanApiWaf.Components;
using SecureCleanApiWaf.Presentation.Extensions.DependencyInjection;
using SecureCleanApiWaf.Presentation.Extensions.HttpPipeline;
using Azure.Identity;

// ===========================================================================================
// PROGRAM.CS - APPLICATION ENTRY POINT
// ===========================================================================================
// This is the main entry point for the ASP.NET Core application
// Uses top-level statements (C# 9.0+) - no Main method needed
// 
// Execution flow:
// 1. Create WebApplicationBuilder (configure services and configuration)
// 2. Register services (Dependency Injection setup)
// 3. Build WebApplication (create DI container, configure services)
// 4. Configure HTTP request pipeline (middleware)
// 5. Run application (start Kestrel web server, listen for requests)
// ===========================================================================================

// ===== STEP 1: CREATE WEB APPLICATION BUILDER =====
// WebApplicationBuilder provides:
// - Configuration system (appsettings.json, environment variables, etc.)
// - Dependency Injection container
// - Logging configuration
// - Environment detection (Development, Staging, Production)
// 
// args: Command-line arguments passed to application
// Can override configuration: --urls "https://localhost:5001" --environment "Production"
var builder = WebApplication.CreateBuilder(args);

// ===========================================================================================
// AZURE KEY VAULT CONFIGURATION (PRODUCTION ONLY)
// ===========================================================================================
// Secure secret management for production deployments
// Secrets stored in Azure Key Vault, not in configuration files
// 
// Why Azure Key Vault?
// - Centralized secret storage
// - Access control and auditing
// - Automatic secret rotation
// - Hardware Security Module (HSM) backed encryption
// - Prevents secrets in source control
// 
// Environment Detection:
// - Development: Uses appsettings.Development.json (simpler secrets)
// - Production: Uses Azure Key Vault (secure secrets)

if (builder.Environment.IsProduction())
{
    // ===== Load Key Vault URL from Configuration =====
    // Read from appsettings.json "KeyVault:Url" section
    // Format: https://{vault-name}.vault.azure.net/
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    
    // ===== Add Key Vault to Configuration System =====
    // Only if URL is configured (allows optional Key Vault)
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        // AddAzureKeyVault:
        // - Loads all secrets from Key Vault
        // - Makes them available via IConfiguration
        // - Secrets accessible like normal config: configuration["SecretName"]
        // 
        // DefaultAzureCredential:
        // - Automatic authentication (Managed Identity in production)
        // - Tries multiple authentication methods in order:
        //   1. Environment variables (local development)
        //   2. Managed Identity (Azure App Service, Azure VM)
        //   3. Azure CLI (local development)
        //   4. Visual Studio (local development)
        //   5. Azure PowerShell (local development)
        // - No passwords or keys needed in code!
        // 
        // Configuration override order (later overrides earlier):
        // 1. appsettings.json
        // 2. appsettings.{Environment}.json
        // 3. Azure Key Vault (this step)
        // 4. Environment Variables
        // 5. Command-line arguments
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

// ===========================================================================================
// DEPENDENCY INJECTION - LAYER SERVICE REGISTRATION
// ===========================================================================================
// Clean Architecture: Register services by layer
// Each layer has its own extension method for DI setup
// Follows Single Responsibility Principle (each method handles one layer)
// 
// Dependency flow (Clean Architecture rule):
// Presentation ? Application ? Domain
// Presentation ? Infrastructure ? Application ? Domain
// 
// Services are registered in the order they're used
// Inner layers first, outer layers last

// ===== APPLICATION LAYER SERVICES =====
// Business logic and use cases (CQRS, MediatR)
// 
// What's registered:
// - MediatR (query/command handlers)
// - Pipeline Behaviors (caching, validation, logging)
// - FluentValidation validators
// - Application-specific services
// 
// Location: Presentation/Extensions/DependencyInjection/ApplicationServiceExtensions.cs
// 
// Why first?
// Core business logic has no external dependencies
// - Other layers depend on Application interfaces
builder.Services.AddApplicationServices();

// ===== INFRASTRUCTURE LAYER SERVICES =====
// External concerns and technical implementations
// 
// What's registered:
// - API Integration Service (external HTTP calls)
// - Cache Service (distributed caching)
// - JWT Token Generator (authentication)
// - HttpClient with Polly policies (retry, circuit breaker)
// - Legacy services (SampleCache)
// 
// Location: Presentation/Extensions/DependencyInjection/InfrastructureServiceExtensions.cs
// 
// Requires: IConfiguration (passed from builder)
// - Reads settings from appsettings.json
// - Configures HttpClient base URLs, timeouts, etc.
builder.Services.AddInfrastructureServices(builder.Configuration);

// ===== PRESENTATION LAYER SERVICES =====
// Web-specific services (Blazor, API, Swagger, Security)
// 
// What's registered:
// - JWT Bearer Authentication
// - Authorization Policies (ApiUser, AdminOnly)
// - Rate Limiting (IP-based throttling)
// - CORS (cross-origin resource sharing)
// - Blazor Server (interactive components)
// - API Controllers (REST endpoints)
// - Swagger/OpenAPI (API documentation)
// - Health Checks (monitoring endpoints)
// 
// Location: Presentation/Extensions/DependencyInjection/PresentationServiceExtensions.cs
// 
// Requires: IConfiguration (passed from builder)
// - Reads JWT settings
// - Reads rate limiting configuration
// - Reads CORS allowed origins
builder.Services.AddPresentationServices(builder.Configuration);

// ===========================================================================================
// BUILD APPLICATION
// ===========================================================================================
// Builds the WebApplication from configured services
// 
// What happens during Build():
// 1. Validates service registrations (checks for missing dependencies)
// 2. Creates Dependency Injection container
// 3. Resolves all singleton services
// 4. Prepares middleware pipeline
// 5. Configures Kestrel web server
// 
// After Build():
// - Can no longer add services (container is sealed)
// - Can configure middleware pipeline
// - Can map endpoints
var app = builder.Build();

// ===========================================================================================
// HTTP REQUEST PIPELINE CONFIGURATION
// ===========================================================================================
// Configure middleware that processes HTTP requests
// 
// Middleware execution order (CRITICAL):
// 1. Exception handling (catch all errors)
// 2. HTTPS redirection (secure all traffic)
// 3. Static files (serve CSS/JS without auth)
// 4. Routing (match request to endpoint)
// 5. CORS (validate cross-origin requests)
// 6. Rate limiting (throttle excessive requests)
// 7. Authentication (who are you?)
// 8. Authorization (what can you do?)
// 9. Security headers (add protection headers)
// 10. Endpoint execution (business logic)
// 
// Extension method: ConfigurePipeline()
// Location: Presentation/Extensions/HttpPipeline/WebApplicationExtensions.cs
// 
// What's configured:
// - Exception handling (production vs development)
// - Swagger UI (development only)
// - HTTPS redirection
// - Static files (wwwroot)
// - Routing
// - CORS policy
// - Rate limiting middleware
// - Authentication middleware
// - Authorization middleware
// - Security headers middleware
// - Endpoint mapping (Blazor, API, health checks)
// 
// Benefits of extension method:
// - Keeps Program.cs clean and readable
// - Centralizes pipeline configuration
// - Easier to test and maintain
// - Follows Single Responsibility Principle
app.ConfigurePipeline();

// ===========================================================================================
// RUN APPLICATION
// ===========================================================================================
// Starts the Kestrel web server and listens for requests
// 
// What happens during Run():
// 1. Kestrel web server starts
// 2. Listens on configured URLs (from configuration or command-line)
// 3. Default ports:
//    - HTTP: 5000 (if not using HTTPS redirection)
//    - HTTPS: 5001 (development certificate)
//    - HTTPS: 7000 (custom configuration)
// 4. Processes incoming HTTP requests through middleware pipeline
// 5. Runs until:
//    - Ctrl+C pressed
//    - Application receives shutdown signal
//    - Unhandled exception occurs (crashes application)
// 
// Blocking call: Application runs until stopped
// After Run(): Cleanup and shutdown
// 
// Graceful shutdown:
// - Stops accepting new requests
// - Completes in-flight requests (with timeout)
// - Disposes services (IDisposable.Dispose())
// - Logs shutdown messages
app.Run();

// ===========================================================================================
// DEPLOYMENT NOTES
// ===========================================================================================
// 
// LOCAL DEVELOPMENT:
// - dotnet run (starts application)
// - dotnet watch run (auto-restart on file changes)
// - F5 in Visual Studio (debug mode)
// - URLs: https://localhost:7000, https://localhost:5001
// 
// AZURE APP SERVICE:
// - Automatically detects ASP.NET Core application
// - Uses production configuration
// - Managed Identity for Key Vault access
// - Environment variable: ASPNETCORE_ENVIRONMENT=Production
// - Automatic HTTPS with App Service certificate
// 
// DOCKER:
// - Requires Dockerfile (containerizes application)
// - EXPOSE port 80 and/or 443
// - Environment variables for configuration
// - Health check endpoint: /health
// 
// KUBERNETES:
// - Deployment manifest (pods, replicas, load balancing)
// - Service manifest (expose application)
// - ConfigMap (non-sensitive configuration)
// - Secret (sensitive configuration)
// - Liveness probe: /health
// - Readiness probe: /health
// 
// ===========================================================================================
// CONFIGURATION SOURCES (PRIORITY ORDER - LATER OVERRIDES EARLIER)
// ===========================================================================================
// 
// 1. appsettings.json (base configuration)
// 2. appsettings.{Environment}.json (environment-specific)
// 3. User Secrets (development only, not in source control)
// 4. Azure Key Vault (production secrets)
// 5. Environment Variables (container/cloud configuration)
// 6. Command-line arguments (runtime overrides)
// 
// Example override:
// appsettings.json: "JwtSettings:ExpirationMinutes": 60
// Environment variable: JwtSettings__ExpirationMinutes=30 (double underscore)
// Result: 30 minutes (environment variable wins)
// 
// ===========================================================================================
// CLEAN ARCHITECTURE SUMMARY
// ===========================================================================================
// 
// Program.cs orchestrates all layers:
// 
// ???????????????????????????????????????????
// ?         Program.cs (Entry Point)        ?
// ???????????????????????????????????????????
//                   ?
//       ?????????????????????????
//       ?           ?           ?
//       ?           ?           ?
// ???????????? ?????????????? ??????????????
// ?Presentation? ?Application? ?Infrastructure?
// ?  Layer    ? ?   Layer    ? ?   Layer     ?
// ???????????? ?????????????? ???????????????
//       ?           ?           ?
//       ?????????????????????????
//                   ?
//       ?????????????????????????
//       ?   Dependency          ?
//       ?   Injection           ?
//       ?   Container           ?
//       ?????????????????????????
// 
// Benefits:
// - Clean separation of concerns
// - Testable (layers can be tested independently)
// - Maintainable (changes isolated to specific layers)
// - Flexible (swap implementations without changing business logic)
// - Scalable (layers can evolve independently)
// 
// ===========================================================================================
