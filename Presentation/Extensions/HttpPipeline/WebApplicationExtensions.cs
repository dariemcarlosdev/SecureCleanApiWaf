using AspNetCoreRateLimit;
using CleanArchitecture.ApiTemplate.Components;
using CleanArchitecture.ApiTemplate.Infrastructure.Middleware;

namespace CleanArchitecture.ApiTemplate.Presentation.Extensions.HttpPipeline
{
    /// <summary>
    /// HTTP pipeline configuration extensions for the Web application with security middleware
    /// </summary>
    /// <remarks>
    /// The HTTP request pipeline defines how ASP.NET Core processes HTTP requests.
    /// Middleware components are executed in the order they're added.
    /// 
    /// ?? ORDER IS CRITICAL! Each middleware:
    /// 1. Can process the request before calling next middleware
    /// 2. Calls the next middleware in the pipeline
    /// 3. Can process the response after next middleware completes
    /// 
    /// Request flow: Top ? Bottom ? Endpoint
    /// Response flow: Endpoint ? Bottom ? Top
    /// 
    /// Best practice ordering (used here):
    /// 1. Exception handling (catch all errors)
    /// 2. HTTPS redirection (secure all traffic)
    /// 3. Static files (bypass auth for CSS/JS)
    /// 4. Routing (determine endpoint)
    /// 5. CORS (cross-origin validation)
    /// 6. Rate limiting (throttle requests)
    /// 7. Authentication (who are you?)
    /// 8. JWT blacklist validation (is your token still valid?)
    /// 9. Authorization (what can you do?)
    /// 10. Custom middleware (security headers, etc.)
    /// 11. Endpoint mapping (execute business logic)
    /// </remarks>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Configures the application's HTTP request pipeline in a security-first, production-ready order and maps endpoints.
        /// </summary>
        /// <returns>The same <see cref="WebApplication"/> instance after configuring middleware, endpoints, and security headers.</returns>
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            // ===========================================================================================
            // HTTP REQUEST PIPELINE CONFIGURATION (ORDER MATTERS!)
            // ===========================================================================================
            
            // ===== STEP 1: EXCEPTION HANDLING (MUST BE FIRST) =====
            // Catches all unhandled exceptions from middleware below
            // Provides user-friendly error pages
            // Prevents sensitive error details from leaking to clients
            
            if (!app.Environment.IsDevelopment())
            {
                // ===== PRODUCTION EXCEPTION HANDLING =====
                // Redirect to error page without detailed error information
                // Prevents leaking stack traces, file paths, connection strings, etc.
                // 
                // createScopeForErrors: true
                // - Creates a new DI scope for error handling
                // - Prevents issues with disposed dependencies
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                
                // ===== HSTS (HTTP Strict Transport Security) =====
                // Forces browsers to use HTTPS for all future requests
                // Prevents man-in-the-middle attacks and protocol downgrade attacks
                // 
                // Default: max-age=2592000 (30 days)
                // Production recommendation: max-age=31536000 (1 year)
                // 
                // How it works:
                // 1. Server sends HSTS header in response
                // 2. Browser remembers to use HTTPS for specified duration
                // 3. Browser automatically converts HTTP requests to HTTPS
                // 4. Protects against SSL stripping attacks
                // 
                // Note: Once enabled, you can't easily disable it (users' browsers remember)
                app.UseHsts();
            }
            else
            {
                // ===== DEVELOPMENT EXCEPTION HANDLING =====
                // Shows detailed error pages with stack traces
                // Useful for debugging but NEVER use in production
                // Implicitly enabled, no need to add explicitly
                
                // ===== SWAGGER UI (DEVELOPMENT ONLY) =====
                // Interactive API documentation and testing interface
                // Provides:
                // - Endpoint discovery
                // - Request/response schemas
                // - Try-it-out functionality
                // - Model definitions
                // - Authentication testing (JWT bearer tokens)
                
                // Enable Swagger JSON endpoint
                // Serves the OpenAPI specification at /swagger/v1/swagger.json
                app.UseSwagger();
                
                // Enable Swagger UI
                // Interactive web interface at /swagger
                app.UseSwaggerUI(c => 
                {
                    // Swagger JSON endpoint (OpenAPI specification)
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlueTread API v1");
                    
                    // URL route for Swagger UI
                    // Access at: https://localhost:7000/swagger
                    c.RoutePrefix = "swagger";
                    
                    // Display request duration in milliseconds
                    // Helps identify slow endpoints during development
                    c.DisplayRequestDuration();
                    
                    // Enable deep linking to specific operations
                    // Allows sharing direct links to API endpoints
                    // Example: /swagger/index.html#/SampleController/GetAllData
                    c.EnableDeepLinking();
                });
            }

            // ===== STEP 2: HTTPS REDIRECTION =====
            // Automatically redirects HTTP requests to HTTPS
            // Essential for security - protects data in transit
            // 
            // How it works:
            // - HTTP request to port 80 ? redirects to HTTPS port 443
            // - Returns 307 (Temporary Redirect) or 308 (Permanent Redirect)
            // - Preserves request method and body
            // 
            // Production requirements:
            // - Valid SSL/TLS certificate installed
            // - Certificate from trusted CA (Let's Encrypt, DigiCert, etc.)
            // - Automatic certificate renewal configured
            app.UseHttpsRedirection();
            
            // ===== STEP 3: STATIC FILES =====
            // Serves static files (CSS, JavaScript, images, etc.) from wwwroot
            // Bypasses authentication/authorization for performance
            // 
            // Why before routing and auth?
            // - Static files don't need authentication
            // - Improves performance (skips unnecessary middleware)
            // - Reduces server load
            // 
            // Default behavior:
            // - Serves files from wwwroot directory
            // - Sets appropriate Content-Type headers
            // - Enables browser caching
            // - Returns 404 for non-existent files
            app.UseStaticFiles();

            // ===== STEP 4: ROUTING =====
            // Matches incoming requests to endpoints (controllers, Blazor pages, etc.)
            // 
            // Two-part process:
            // 1. UseRouting: Analyzes request and selects endpoint
            // 2. UseEndpoints: Executes the selected endpoint
            // 
            // Middleware between UseRouting and endpoint execution can:
            // - Access selected endpoint metadata
            // - Make decisions based on route data
            // - Apply policies to specific routes
            app.UseRouting();

            // ===== STEP 5: CORS (CROSS-ORIGIN RESOURCE SHARING) =====
            // Controls which domains can make requests to your API
            // Prevents unauthorized websites from accessing your API
            // 
            // Security model:
            // 1. Browser sends preflight request (OPTIONS) with Origin header
            // 2. Server checks if origin is in AllowedOrigins list
            // 3. Server responds with allowed origins, methods, headers
            // 4. Browser allows or blocks the actual request
            // 
            // Must be after UseRouting but before UseAuthorization
            // "AllowSpecificOrigins" policy defined in PresentationServiceExtensions.cs
            // 
            // Configuration in appsettings.json:
            // - Development: Allows localhost origins
            // - Production: Should only allow specific production domains
            app.UseCors("AllowSpecificOrigins");

            // ===== STEP 6: RATE LIMITING =====
            // IP-based throttling to prevent abuse and DDoS attacks
            // Tracks requests per IP address and enforces limits
            // 
            // Protection against:
            // - Brute force attacks (repeated login attempts)
            // - API scraping/harvesting
            // - Denial of Service attacks
            // - Resource exhaustion
            // 
            // Configuration in appsettings.json:
            // - Development: 200 req/min, 5000 req/hour (permissive)
            // - Production: 60 req/min, 1000 req/hour (restrictive)
            // 
            // Response when limit exceeded:
            // - HTTP 429 (Too Many Requests)
            // - Retry-After header indicates when to retry
            // 
            // Uses AspNetCoreRateLimit library
            app.UseIpRateLimiting();

            // ===== STEP 7: AUTHENTICATION =====
            // Identifies the user making the request
            // Answers: "Who are you?"
            // 
            // Process:
            // 1. Extract credentials from request (JWT token from Authorization header)
            // 2. Validate credentials (signature, expiration, issuer, audience)
            // 3. If valid: Set User.Identity (ClaimsPrincipal)
            // 4. If invalid: User.Identity.IsAuthenticated = false
            // 
            // JWT Bearer Token Authentication:
            // - Stateless (no server-side session storage)
            // - Token in Authorization header: "Bearer {token}"
            // - Configured in PresentationServiceExtensions.cs
            // 
            // Must be AFTER UseRouting, BEFORE UseAuthorization
            app.UseAuthentication();

            // ===== STEP 8: JWT BLACKLIST VALIDATION =====
            // Validates JWT tokens against the blacklist to handle secure logout
            // Answers: "Is your token still valid (not logged out)?"
            // 
            // Process:
            // 1. Extract JWT token from Authorization header
            // 2. Check if token JTI (JWT ID) is in the blacklist
            // 3. If blacklisted: Return 401 Unauthorized (token was logged out)
            // 4. If not blacklisted: Continue to authorization
            // 
            // Why this order?
            // - AFTER UseAuthentication: We need the token to be parsed first
            // - BEFORE UseAuthorization: We need to reject blacklisted tokens before checking permissions
            // 
            // Security Features:
            // - Fast cache-based lookups (high performance)
            // - Comprehensive security logging
            // - Graceful error handling
            // - No information leakage in error responses
            // 
            // Performance:
            // - Only processes requests with JWT tokens
            // - Skips non-authenticated requests
            // - Uses dual-cache strategy for speed
            app.UseJwtBlacklistValidation();

            // ===== STEP 9: AUTHORIZATION =====
            // Determines what the user can access
            // Answers: "What can you do?"
            // 
            // Process:
            // 1. Check if endpoint requires authorization ([Authorize] attribute)
            // 2. Verify user is authenticated (from UseAuthentication above)
            // 3. Verify token is not blacklisted (from JWT blacklist validation above)
            // 4. Check if user meets requirements (role, policy, claims)
            // 5. If authorized: Continue to endpoint
            // 6. If not authorized: Return 401 (Unauthorized) or 403 (Forbidden)
            // 
            // Authorization decisions:
            // - [AllowAnonymous]: No authentication required
            // - [Authorize]: Authentication required
            // - [Authorize(Roles = "Admin")]: Admin role required
            // - [Authorize(Policy = "AdminOnly")]: Custom policy required
            // 
            // Must be AFTER UseAuthentication and JWT blacklist validation
            app.UseAuthorization();

            // ===== STEP 10: CUSTOM SECURITY HEADERS MIDDLEWARE =====
            // Adds security headers to all responses
            // Protects against common web vulnerabilities
            // 
            // Custom inline middleware (not a separate class)
            // Executes for every request
            app.Use(async (context, next) =>
            {
                // ===== X-Content-Type-Options: nosniff =====
                // Prevents MIME type sniffing attacks
                // Browser must respect Content-Type header (no guessing)
                // 
                // Without this: Browser might execute JSON as JavaScript
                // With this: Browser strictly follows Content-Type
                context.Response.Headers.XContentTypeOptions = "nosniff";
                
                // ===== X-Frame-Options: DENY =====
                // Prevents clickjacking attacks
                // Blocks page from being displayed in iframe/frame/embed
                // 
                // Options:
                // - DENY: Never allow framing
                // - SAMEORIGIN: Allow framing only from same origin
                // - ALLOW-FROM uri: Allow framing from specific URI
                // 
                // Modern alternative: Content-Security-Policy frame-ancestors directive
                context.Response.Headers.XFrameOptions = "DENY";
                
                // ===== X-XSS-Protection: 1; mode=block =====
                // Enables browser's XSS (Cross-Site Scripting) filter
                // 
                // Values:
                // - 0: Disable XSS filtering
                // - 1: Enable filtering (remove unsafe parts)
                // - 1; mode=block: Block entire page if XSS detected
                // 
                // Note: Modern browsers rely more on Content-Security-Policy
                // This header is legacy support for older browsers
                context.Response.Headers.XXSSProtection = "1; mode=block";
                
                // ===== Referrer-Policy: strict-origin-when-cross-origin =====
                // Controls how much referrer information is sent with requests
                // 
                // strict-origin-when-cross-origin:
                // - Same origin: Send full URL
                // - Cross origin: Send only origin (protocol + domain)
                // - HTTPS ? HTTP: Don't send any referrer
                // 
                // Protects:
                // - User privacy (doesn't leak full URLs to third parties)
                // - Sensitive information in URLs (session IDs, tokens)
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                
                // ===== Permissions-Policy (formerly Feature-Policy) =====
                // Controls which browser features can be used
                // Disables potentially dangerous features
                // 
                // Current settings (all disabled):
                // - geolocation: GPS location access
                // - microphone: Microphone access
                // - camera: Camera access
                // 
                // Syntax: feature=(allowed-origins)
                // Empty parentheses = disabled for all origins
                context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                
                // ===== Content-Security-Policy (CSP) =====
                // Powerful security header that controls resource loading
                // Prevents XSS, data injection, and other code injection attacks
                // 
                // Only apply in production (development needs 'unsafe-inline' for hot reload)
                if (!app.Environment.IsDevelopment())
                {
                    // CSP Directives:
                    // - default-src 'self': Only load resources from same origin
                    // - script-src 'self' 'unsafe-inline' 'unsafe-eval': JavaScript sources
                    //   - 'self': Same origin scripts
                    //   - 'unsafe-inline': Inline <script> tags (needed for Blazor)
                    //   - 'unsafe-eval': eval() and new Function() (needed for Blazor)
                    // - style-src 'self' 'unsafe-inline': CSS sources
                    //   - 'self': Same origin stylesheets
                    //   - 'unsafe-inline': Inline <style> tags and style attributes
                    // 
                    // ?? 'unsafe-inline' and 'unsafe-eval' reduce CSP security
                    // TODO: Use nonces or hashes for better security
                    // TODO: Add report-uri to monitor CSP violations
                    context.Response.Headers.ContentSecurityPolicy = 
                        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';";
                }

                // Call next middleware in the pipeline
                // Response headers above are added to the response after endpoint execution
                await next();
            });

            // ===========================================================================================
            // ENDPOINT MAPPING (FINAL STEP)
            // ===========================================================================================
            // Maps requests to actual endpoints (controllers, Blazor components, etc.)
            
            // ===== BLAZOR SERVER COMPONENTS =====
            // Maps Blazor Server components with SignalR for real-time updates
            app.MapRazorComponents<App>()
                // Enable interactive server-side rendering
                // Uses SignalR WebSocket connection for real-time UI updates
                .AddInteractiveServerRenderMode()
                
                // Disable antiforgery validation for Blazor components
                // Blazor has built-in protection via SignalR connection
                // Antiforgery is more relevant for traditional form posts
                .DisableAntiforgery();
            
            // ===== API CONTROLLERS =====
            // Maps REST API endpoints from controllers
            // Default: Require authentication (can override with [AllowAnonymous])
            // 
            // RequireAuthorization():
            // - Adds [Authorize] to all endpoints by default
            // - Individual endpoints can opt-out with [AllowAnonymous]
            // - Secure by default approach (better than remembering to add [Authorize])
            app.MapControllers()
               .RequireAuthorization(); // Require auth by default (can override with [AllowAnonymous])
            
            // ===== BLAZOR HUB (SIGNALR) =====
            // Maps SignalR hub for Blazor Server real-time communication
            // Endpoint: /_blazor
            // 
            // SignalR connection:
            // - WebSocket (preferred, fastest)
            // - Server-Sent Events (SSE) - fallback
            // - Long Polling - fallback for restrictive networks
            app.MapBlazorHub();

            // ===== HEALTH CHECK ENDPOINT =====
            // Simple health check endpoint for monitoring
            // Returns 200 OK if application is running
            // URL to check application health status: https://yourdomain.com/health
            // Uses:
            // - Load balancer health probes
            // - Kubernetes liveness/readiness probes
            // - Monitoring services (Azure Monitor, Application Insights)
            // - Uptime monitoring tools
            // 
            // Endpoint: /health
            // Anonymous access (no authentication required)
            // 
            // Response:
            // - 200 OK: Application is healthy
            // - 503 Service Unavailable: Application has issues (if health checks fail)
            app.MapHealthChecks("/health")
               .AllowAnonymous();

            // Return configured application
            // Ready to handle HTTP requests through the pipeline
            return app;
        }
    }
}