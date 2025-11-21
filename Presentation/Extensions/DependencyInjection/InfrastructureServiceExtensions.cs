using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Infrastructure.Services;
using SecureCleanApiWaf.Infrastructure.Caching;
using SecureCleanApiWaf.Infrastructure.Handlers;
using SecureCleanApiWaf.Infrastructure.Security;
using SecureCleanApiWaf.Infrastructure.Data;
using SecureCleanApiWaf.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

namespace SecureCleanApiWaf.Presentation.Extensions.DependencyInjection
{
    /// <summary>
    /// Dependency injection setup for Infrastructure layer services
    /// Registers concrete implementations of infrastructure concerns
    /// </summary>
    /// <remarks>
    /// This extension method follows the Dependency Inversion Principle:
    /// - Application layer defines interfaces (abstractions)
    /// - Infrastructure layer implements those interfaces (details)
    /// - This allows swapping implementations without changing business logic
    /// 
    /// Services registered here:
    /// - Database Context (Entity Framework Core)
    /// - Repositories (data access layer)
    /// - API Integration (external API calls)
    /// - Caching (distributed and in-memory)
    /// - Security (JWT token generation, API key handling, token blacklisting)
    /// - HTTP Client with resilience policies (Polly)
    /// </remarks>
    public static class InfrastructureServiceExtensions
    {
        /// <summary>
        /// Registers infrastructure dependencies: configures the EF Core DbContext with SQL Server, repository and security services, HTTP clients with resilience policies and handlers, and in-memory/distributed caching.
        /// </summary>
        /// <returns>The same <see cref="IServiceCollection"/> instance with infrastructure services registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown when database configuration from <c>DatabaseSettings</c> is invalid.</exception>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ===== DATABASE CONFIGURATION =====
            // Configure Entity Framework Core with SQL Server
            
            // Read database settings from configuration
            var databaseSettings = new DatabaseSettings();
            configuration.GetSection("DatabaseSettings").Bind(databaseSettings);

            // Validate settings
            if (!databaseSettings.IsValid())
            {
                throw new InvalidOperationException(
                    "Invalid database configuration. Please check your appsettings.json");
            }

            // Register DbContext with SQL Server provider
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // SQL Server connection
                options.UseSqlServer(
                    databaseSettings.ConnectionString,
                    sqlOptions =>
                    {
                        // Enable retry on transient failures
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: databaseSettings.MaxRetryCount,
                            maxRetryDelay: TimeSpan.FromSeconds(databaseSettings.MaxRetryDelay),
                            errorNumbersToAdd: null);

                        // Command timeout
                        sqlOptions.CommandTimeout(databaseSettings.CommandTimeout);

                        // Enable connection resiliency
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                        // Query splitting configuration
                        if (databaseSettings.EnableQuerySplitting)
                        {
                            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        }
                    });

                // Development: Enable detailed logging
                if (databaseSettings.EnableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }

                if (databaseSettings.EnableDetailedErrors)
                {
                    options.EnableDetailedErrors();
                }
            });

            // ===== REPOSITORY REGISTRATION =====
            // Register repositories as scoped services (one instance per HTTP request)
            // Scoped lifetime ensures:
            // - Proper DbContext lifecycle management
            // - Automatic disposal
            // - Thread safety per request
            
            services.AddScoped<IApiDataItemRepository, ApiDataItemRepository>();
            // Register User and Token repositories for authentication and security
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();

            // ===== CORE INFRASTRUCTURE SERVICES =====
            // Register services as singletons for better performance (thread-safe, stateless services)
            
            // API Integration Service: Handles all external HTTP API calls
            // Registered as interface ? implementation (Dependency Inversion Principle)
            // Singleton: One instance shared across the application
            services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();
            
            // Cache Service: Provides distributed caching functionality
            // Singleton: Caching service is stateless and thread-safe
            services.AddSingleton<ICacheService, CacheService>();
            
            // ===== SECURITY SERVICES =====
            
            // JWT Token Generator: Creates authentication tokens for testing/development
            // Scoped: New instance per HTTP request (safer for request-specific data)
            // ?? WARNING: This is for DEVELOPMENT/DEMO only!
            // In production, replace with proper identity provider (Azure AD, IdentityServer, etc.)
            services.AddScoped<JwtTokenGenerator>();
            
            // Token Blacklist Service: Manages JWT token invalidation for secure logout
            // Scoped: New instance per HTTP request for thread safety and request isolation
            // This service handles:
            // - Adding tokens to blacklist on logout
            // - Checking if tokens are blacklisted during authentication
            // - Automatic cleanup of expired blacklist entries
            services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
            
            // ===== LEGACY SERVICES =====
            
            // Legacy cache service maintained for backward compatibility
            // TODO: Migrate existing code to use ICacheService instead
            services.AddSingleton<SampleCache>();
            
            // ===== HTTP MESSAGE HANDLERS =====
            
            // API Key Handler: DelegatingHandler for adding API keys to outgoing requests
            // Transient: New instance per HTTP request (lightweight, no state)
            services.AddTransient<ApiKeyHandler>();
            
            // ===== HTTP CLIENT CONFIGURATION WITH RESILIENCE =====
            // Named HttpClient for external API calls
            // Benefits of IHttpClientFactory:
            // 1. Prevents socket exhaustion by reusing HttpMessageHandler instances
            // 2. Enables centralized configuration
            // 3. Supports named/typed clients for different APIs
            // 4. Integrates with Polly for resilience policies
            
            services.AddHttpClient("ThirdPartyApiClient", client =>
            {
                // ===== Base Configuration =====
                // Read base URL from configuration (appsettings.json)
                var baseUrl = configuration["ThirdPartyApi:BaseUrl"];
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    // Set base address so all requests can use relative paths
                    // Example: client.GetAsync("/users") ? https://api.example.com/users
                    client.BaseAddress = new Uri(baseUrl);
                }
                
                // ===== Security: Timeout Configuration =====
                // Set reasonable timeout to prevent hanging requests
                // Default: 30 seconds (configurable in appsettings.json)
                // Prevents resource exhaustion from slow/unresponsive APIs
                var timeout = configuration.GetValue<int>("ThirdPartyApi:Timeout", 30);
                client.Timeout = TimeSpan.FromSeconds(timeout);
                
                // ===== Security: Required Headers =====
                // Accept: Specifies we expect JSON responses
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                // User-Agent: Identifies our application to the API
                // Good practice: Helps API providers track usage and debug issues
                client.DefaultRequestHeaders.Add("User-Agent", "BlueTreadApp/1.0");
            })
            // ===== HTTP MESSAGE HANDLERS (Execution Order: Top to Bottom) =====
            
            // 1. API Key Handler: Adds authentication headers to every request
            //    - Injects API key from secure configuration
            //    - Centralized authentication logic
            //    - Logs all outgoing requests for monitoring
            .AddHttpMessageHandler<ApiKeyHandler>()
            
            // 2. Retry Policy: Automatically retries failed requests
            //    - Handles transient errors (5xx, 408, 429)
            //    - Uses exponential backoff (1s, 2s, 4s)
            //    - Prevents overwhelming the external API
            .AddPolicyHandler(GetRetryPolicy())
            
            // 3. Circuit Breaker: Prevents cascading failures
            //    - Stops calling failing API after 5 consecutive failures
            //    - Waits 30 seconds before trying again
            //    - Protects both our app and the external API
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // ===== CACHING CONFIGURATION =====
            
            // In-Memory Cache: Fast, local caching for frequently accessed data
            // Used by: CachingBehavior (MediatR pipeline), CacheService, TokenBlacklistService
            // Lifecycle: Application lifetime (data lost on restart)
            services.AddMemoryCache();

            // Distributed Cache: Shared caching across multiple instances
            // Development: Uses in-memory implementation (DistributedMemoryCache)
            // Production: Replace with Redis, SQL Server, or Azure Cache
            // 
            // Benefits of distributed cache:
            // - Shared state across multiple servers
            // - Survives application restarts
            // - Scales horizontally
            // 
            // Used by:
            // - CacheService (general caching)
            // - TokenBlacklistService (shared blacklist across instances)
            // 
            // To use Redis in production:
            // services.AddStackExchangeRedisCache(options => {
            //     options.Configuration = configuration["Redis:ConnectionString"];
            // });
            services.AddDistributedMemoryCache();
            
            return services;
        }

        /// <summary>
        /// Retry policy with exponential backoff for transient HTTP failures
        /// </summary>
        /// <returns>Polly retry policy that handles transient errors</returns>
        /// <remarks>
        /// Transient errors are temporary failures that might succeed if retried:
        /// - 5xx errors (server errors)
        /// - 408 (request timeout)
        /// - 429 (too many requests / rate limiting)
        /// 
        /// Exponential Backoff Strategy:
        /// - Retry 1: Wait 2^1 = 2 seconds
        /// - Retry 2: Wait 2^2 = 4 seconds
        /// - Retry 3: Wait 2^3 = 8 seconds
        /// 
        /// Why exponential backoff?
        /// - Gives the API time to recover
        /// - Reduces server load during outages
        /// - Industry best practice for resilience
        /// <summary>
        /// Creates a retry policy for transient HTTP failures and rate limiting responses.
        /// </summary>
        /// <returns>
        /// An async policy that retries up to 3 times with exponential backoff (2s, 4s, 8s) for transient HTTP errors (5xx and 408) and 429 (Too Many Requests), invoking a retry callback on each attempt.
        /// </returns>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                // Handle transient HTTP errors
                // Automatically handles: 5xx (server errors) and 408 (request timeout)
                .HandleTransientHttpError()
                
                // Additionally handle 429 (Too Many Requests)
                // This is rate limiting - API is telling us to slow down
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                
                // Configure retry behavior
                .WaitAndRetryAsync(
                    // Maximum number of retry attempts
                    retryCount: 3,
                    
                    // Exponential backoff: 2^retryAttempt seconds
                    // Retry 1: 2s, Retry 2: 4s, Retry 3: 8s
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    
                    // Callback invoked on each retry (for logging/monitoring)
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        // Log retry attempts for monitoring and debugging
                        // In production, use proper logging framework (ILogger, Serilog, etc.)
                        // Include: retry count, wait time, status code, request URI
                        Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to {outcome.Result?.StatusCode}");
                        
                        // In production, log to structured logging:
                        // _logger.LogWarning(
                        //     "HTTP retry {RetryCount} after {Delay}ms. Status: {StatusCode}, URI: {RequestUri}",
                        //     retryAttempt, timespan.TotalMilliseconds, outcome.Result?.StatusCode, outcome.Result?.RequestMessage?.RequestUri
                        // );
                    });
        }

        /// <summary>
        /// Circuit breaker policy to prevent cascading failures
        /// </summary>
        /// <returns>Polly circuit breaker policy</returns>
        /// <remarks>
        /// Circuit Breaker Pattern (inspired by electrical circuit breakers):
        /// 
        /// CLOSED State (Normal Operation):
        /// - Requests flow normally to the API
        /// - Tracks failure count
        /// 
        /// OPEN State (Circuit Tripped):
        /// - After 5 consecutive failures, circuit "opens"
        /// - All requests fail immediately (no API calls made)
        /// - Prevents wasting resources on known-failing API
        /// - Protects both our app and the external API
        /// 
        /// HALF-OPEN State (Testing Recovery):
        /// - After 30 seconds, allows one test request through
        /// - If successful: Circuit closes, normal operation resumes
        /// - If fails: Circuit opens again for another 30 seconds
        /// 
        /// Why use Circuit Breaker?
        /// - Fails fast when API is down (no waiting for timeouts)
        /// - Prevents cascading failures across services
        /// - Gives failing services time to recover
        /// - Reduces resource consumption during outages
        /// - Industry standard for microservices resilience
        /// <summary>
        /// Creates a circuit breaker policy that protects HTTP calls by opening the circuit after repeated transient failures.
        /// </summary>
        /// <remarks>
        /// The policy treats transient HTTP errors (5xx and 408) and 429 (Too Many Requests) as failures. When the circuit opens, it remains open for 30 seconds before allowing attempts to resume. Callbacks are invoked on break and reset to surface state changes (e.g., logging or telemetry).
        /// </remarks>
        /// <returns>
        /// An asynchronous circuit breaker policy that opens after 5 consecutive transient HTTP failures and stays open for 30 seconds; invokes onBreak and onReset callbacks when the circuit state changes.
        /// </returns>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                // Handle the same transient errors as retry policy
                .HandleTransientHttpError()
                
                // Configure circuit breaker behavior
                .CircuitBreakerAsync(
                    // Number of consecutive failures before circuit opens
                    // After 5 failures in a row, stop calling the API
                    handledEventsAllowedBeforeBreaking: 5,
                    
                    // How long to wait before attempting to close the circuit
                    // 30 seconds: Gives the external API time to recover
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    
                    // Callback when circuit opens (transitions to OPEN state)
                    onBreak: (outcome, duration) =>
                    {
                        // Log circuit breaker activation
                        // This is a critical event that should trigger alerts
                        Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                        
                        // In production, trigger alerts and monitoring:
                        // _logger.LogError(
                        //     "Circuit breaker OPENED. API unavailable for {Duration}s. Status: {StatusCode}",
                        //     duration.TotalSeconds, outcome.Result?.StatusCode
                        // );
                        // _telemetry.TrackEvent("CircuitBreakerOpened", properties);
                    },
                    
                    // Callback when circuit closes (transitions back to CLOSED state)
                    onReset: () =>
                    {
                        // Log circuit breaker recovery
                        Console.WriteLine("Circuit breaker reset");
                        
                        // In production, log successful recovery:
                        // _logger.LogInformation("Circuit breaker RESET. API recovered and accepting requests.");
                        // _telemetry.TrackEvent("CircuitBreakerReset");
                    });
        }
    }
}