using SecureCleanApiWaf.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SecureCleanApiWaf.Core.Application.Features.SampleData.Queries;

namespace SecureCleanApiWaf.Presentation.Controllers.v1
{
    /// <summary>
    /// Sample API controller for demonstration with JWT authentication.
    /// All endpoints require valid JWT bearer token.
    /// </summary>
    /// <remarks>
    /// This controller demonstrates:
    /// - JWT Bearer authentication
    /// - Role-based authorization (User vs Admin)
    /// - CQRS pattern with MediatR
    /// - Structured logging
    /// - Proper HTTP status codes
    /// - Exception handling
    /// - Anonymous endpoints (health checks)
    /// 
    /// Architecture Pattern:
    /// Controller ? MediatR ? Handler ? Service ? External API
    /// 
    /// Clean Architecture:
    /// - Controller (Presentation Layer): HTTP concerns, routing, responses
    /// - MediatR (Application Layer): Business logic orchestration
    /// - No direct service dependencies (loose coupling via MediatR)
    /// </remarks>
    [ApiController]
    [Route("api/v1/sample")]
    [Authorize] // Require authentication for all endpoints (override with [AllowAnonymous])
    [Produces("application/json")]
    public class SampleController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SampleController> _logger;

        /// <summary>
        /// Constructor: Dependency injection of MediatR and ILogger
        /// </summary>
        /// <param name="mediator">MediatR mediator for sending queries/commands</param>
        /// <param name="logger">Logger for structured logging</param>
        /// <remarks>
        /// Dependencies injected:
        /// - IMediator: Routes requests to handlers (CQRS pattern)
        /// - ILogger: Structured logging with correlation IDs
        /// 
        /// Benefits:
        /// - Testable (can mock dependencies)
        /// - Loosely coupled (no direct service dependencies)
        /// - Follows SOLID principles (Dependency Inversion)
        /// <summary>
        /// Initializes a new instance of the SampleController with its dependencies.
        /// </summary>
        /// <param name="mediator">MediatR mediator used to dispatch queries and commands to handlers.</param>
        /// <param name="logger">Structured logger for recording controller events and diagnostics.</param>
        public SampleController(IMediator mediator, ILogger<SampleController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Handles HTTP GET requests to retrieve all data from the external API and returns the result as an HTTP
        /// response.
        /// </summary>
        /// <remarks>
        /// **Security:** Requires valid JWT bearer token.
        /// 
        /// **Flow:**
        /// 1. User sends GET request with JWT token in Authorization header
        /// 2. JWT middleware validates token (authentication)
        /// 3. [Authorize] attribute checks user is authenticated (authorization)
        /// 4. Controller logs request with user identity
        /// 5. MediatR sends query to handler
        /// 6. Handler calls API integration service
        /// 7. Service calls external API with retry/circuit breaker policies
        /// 8. Result returns through layers
        /// 9. Controller returns HTTP response
        /// 
        /// **Caching:**
        /// If query implements ICacheable, CachingBehavior will cache results
        /// 
        /// The response will contain the data as returned by the external API if the request
        /// succeeds. If the API call fails, the response will include error information in the body. This method does
        /// not perform any additional validation or transformation of the data.
        /// </remarks>
        /// <returns>An <see cref="IActionResult"/> containing the data retrieved from the external API if the operation is
        /// successful; otherwise, a bad request response with error details.</returns>
        /// <response code="200">Returns the requested data</response>
        /// <response code="400">If the external API call fails</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <summary>
        /// Retrieves all sample data from the configured external API.
        /// </summary>
        /// <returns>HTTP 200 with a collection of SampleDtoModel on success; HTTP 400 with `{ error }` when the external call fails; HTTP 401 when the caller is unauthorized; HTTP 500 with an error payload for unexpected server errors.</returns>
        /// <response code="200">Returns a collection of SampleDtoModel.</response>
        /// <response code="400">When the external API or query handler reports a failure; response contains `{ error }`.</response>
        /// <response code="401">When the caller is not authenticated.</response>
        /// <response code="500">When an unexpected server error occurs; response contains a generic error message and diagnostic details.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllData(
            // Optional query parameters for filtering, sorting, pagination can be added here. This should be implemented in the query/handler as needed.
            // Query/handler currently does not support these parameters. GetApiDataQuery query would need to be extended to accept and process them.
            // Validation and application of these parameters should be done in the handler layer since controller should remain thin.
            //[FromQuery] string? search, 
            //[FromQuery] string? sortBy,
            //[FromQuery] string? order = "asc",
            //[FromQuery] int? page = 1,
            //[FromQuery] int? pageSize = 10
            )
        {
            try
            {
                // ===== STEP 1: Log Request with User Context =====
                // Structured logging with user identity for audit trail
                // User.Identity?.Name contains username from JWT token claims
                // Falls back to "Anonymous" if somehow authentication failed
                // 
                // Why log this?
                // - Security auditing (who accessed what)
                // - Usage analytics (which users use the API)
                // - Troubleshooting (correlate errors with users)
                _logger.LogInformation("User {UserId} requested all data", User.Identity?.Name ?? "Anonymous");
                
                // ===== STEP 2: Send Query via MediatR =====
                // CQRS Pattern: Query objects separate reads from writes
                // MediatR routes query to appropriate handler
                // 
                // GetApiDataQuery<SampleDtoModel>:
                // - Generic query that can handle any DTO type
                // - "your-api-relative-path": Relative path for external API
                // - Handler will use ApiIntegrationService to call external API
                // 
                // Result<T> Pattern:
                // - Encapsulates success/failure without exceptions
                // - Contains either Data (on success) or Error (on failure)
                // - Cleaner than try-catch for expected failures
                var result = await _mediator.Send(new GetApiDataQuery<SampleDtoModel>("your-api-relative-path"));
                
                // ===== STEP 3: Process Result =====
                if (result.Success)
                {
                    // ===== SUCCESS PATH =====
                    // Log successful data retrieval
                    // Include user identity for audit trail
                    // User.Identity?.Name: Username from JWT token claims
                    _logger.LogInformation("Successfully retrieved data for user {UserId}", User.Identity?.Name);
                    
                    // Return HTTP 200 OK with data
                    // Data is already in DTO format (SampleDtoModel)
                    // No additional transformation needed
                    return Ok(result.Data);
                }
                
                // ===== FAILURE PATH =====
                // Log warning (not error, as this is an expected failure from external API)
                // Include error message for troubleshooting
                _logger.LogWarning("Failed to retrieve data: {Error}", result.Error);
                
                // Return HTTP 400 Bad Request with error details
                // Client can display error message to user
                // Anonymous object for JSON response: { "error": "message" }
                return BadRequest(new { error = result.Error });
            }
            catch (Exception ex)
            {
                // ===== EXCEPTION HANDLING =====
                // Catches unexpected errors (not handled by Result pattern)
                // Examples: Network issues, serialization errors, null references
                // 
                // Log as Error (unexpected condition requiring attention)
                // Include exception details for debugging
                _logger.LogError(ex, "Unexpected error occurred while retrieving all data");
                
                // Return HTTP 500 Internal Server Error
                // Include generic error message (don't leak sensitive details)
                // In production, exclude ex.Message to prevent information disclosure
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint - does not require authentication. Check URL: /api/v1/sample/status
        /// </summary>
        /// <returns>Status message indicating the controller is running</returns>
        /// <response code="200">Controller is operational</response>
        /// <remarks>
        /// Public health check endpoint for monitoring and diagnostics.
        /// 
        /// **Use Cases:**
        /// - Load balancer health probes
        /// - Kubernetes liveness/readiness probes
        /// - Uptime monitoring services (Pingdom, UptimeRobot)
        /// - Application Insights availability tests
        /// - Manual testing (quick check if API is responding)
        /// 
        /// **Why Anonymous?**
        /// - Health checks shouldn't require authentication
        /// - Monitoring tools may not support authentication
        /// - Quick verification without token generation
        /// 
        /// **Response Format:**
        /// Returns JSON with:
        /// - status: Simple message indicating controller is operational
        /// - timestamp: UTC time of request (useful for checking API time sync)
        /// 
        /// **Security Note:**
        /// This endpoint leaks minimal information (just that API is running)
        /// No sensitive data or system details exposed
        /// <summary>
        /// Provides a public health-check endpoint that reports service status and the current UTC timestamp.
        /// </summary>
        /// <returns>An HTTP 200 OK response with a JSON object containing `status` (human-readable message) and `timestamp` (UTC DateTime).</returns>
        [HttpGet("status")]
        [AllowAnonymous] // Public endpoint for health checks (overrides [Authorize] at class level)
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetStatus() => Ok(new { status = "SampleController is running.", timestamp = DateTime.UtcNow });

        /// <summary>
        /// Handles HTTP GET requests to retrieve data by ID from the external API and returns the result as an HTTP
        /// response.
        /// </summary>
        /// <remarks>
        /// **Security:** Requires valid JWT bearer token.
        /// 
        /// **Parameterized Query:**
        /// Retrieves a single resource by its identifier
        /// Route pattern: /api/v1/sample/{id}
        /// Example: /api/v1/sample/123
        /// 
        /// **Validation:**
        /// - ID is required (cannot be null, empty, or whitespace)
        /// - Returns 400 Bad Request if validation fails
        /// - Prevents unnecessary external API calls with invalid data
        /// 
        /// **Flow:**
        /// 1. Validate ID parameter
        /// 2. Log request with user and ID
        /// 3. Send query via MediatR
        /// 4. Handler calls external API with ID
        /// 5. Return result or error
        /// 
        /// The response will contain the data as returned by the external API if the request
        /// succeeds. If the API call fails, the response will include error information in the body. This method does
        /// not perform any additional validation or transformation of the data.
        /// </remarks>
        /// <param name="id">The ID of the data to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the data retrieved from the external API if the operation is
        /// successful; otherwise, a bad request response with error details.</returns>
        /// <response code="200">Returns the requested data</response>
        /// <response code="400">If the external API call fails or ID is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the resource is not found</response>
        /// <summary>
        /// Retrieves a single SampleDtoModel by its identifier.
        /// </summary>
        /// <param name="id">Identifier of the resource to retrieve; cannot be null, empty, or whitespace.</param>
        /// <returns>HTTP 200 with the requested resource when found; HTTP 400 with an error message for validation failures or API-reported errors; HTTP 401 if the caller is not authenticated; HTTP 404 if the resource does not exist; HTTP 500 with error details for unexpected failures.</returns>
        /// <response code="200">Resource found and returned.</response>
        /// <response code="400">Invalid request or external API returned an error.</response>
        /// <response code="401">Authentication is required or has failed.</response>
        /// <response code="404">Resource with the specified ID was not found.</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDataById(string id)
        {
            try
            {
                // ===== STEP 1: Input Validation =====
                // Validate ID parameter before processing
                // Prevents calling external API with invalid data
                // Saves network bandwidth and API quota
                // 
                // Validation checks:
                // - Not null
                // - Not empty string
                // - Not whitespace only
                if (string.IsNullOrWhiteSpace(id))
                {
                    // Log validation failure with user context
                    // Warning level (not error, expected validation failure)
                    // User.Identity?.Name contains username from JWT token claims
                    _logger.LogWarning("Invalid ID provided by user {UserId}", User.Identity?.Name);
                    
                    // Return HTTP 400 Bad Request
                    // Clear error message for client
                    return BadRequest(new { error = "ID parameter is required and cannot be empty." });
                }

                // ===== STEP 2: Log Request =====
                // Structured logging with user identity and ID parameter
                // Essential for:
                // - Audit trail (who accessed what resource)
                // - Usage analytics (which resources are popular)
                // - Troubleshooting (correlate errors with specific IDs)
                _logger.LogInformation("User {UserId} requested data for ID: {Id}", User.Identity?.Name ?? "Anonymous", id);
                
                // ===== STEP 3: Construct API Path =====
                // Relative path for external API call
                // Handler will combine this with base URL from configuration
                // Example: base URL + "your-api-relative-path" + "/" + id
                // 
                // TODO: Replace "your-api-relative-path" with actual API endpoint
                // Example: "posts", "users", "products", etc.
                var apiPath = "your-api-relative-path";
                
                // ===== STEP 4: Send Query via MediatR =====
                // GetApiDataByIdQuery: Parameterized query with ID
                // Generic type: SampleDtoModel (can be any DTO)
                // MediatR routes to GetApiDataByIdQueryHandler
                var result = await _mediator.Send(new GetApiDataByIdQuery<SampleDtoModel>(apiPath, id));
                
                // ===== STEP 5: Process Result =====
                if (result.Success)
                {
                    // ===== SUCCESS PATH =====
                    // Log successful retrieval with ID for audit trail
                    _logger.LogInformation("Successfully retrieved data for ID: {Id}", id);
                    
                    // Return HTTP 200 OK with data
                    return Ok(result.Data);
                }
                
                // ===== FAILURE PATH =====
                // Log warning with ID and error message
                // Could be 404 (not found) or other API error
                _logger.LogWarning("Failed to retrieve data for ID {Id}: {Error}", id, result.Error);
                
                // Return HTTP 400 Bad Request with error details
                // Note: Could enhance this to return 404 Not Found for specific errors
                // Check if error message contains "not found" to return appropriate status
                return BadRequest(new { error = result.Error });
            }
            catch (Exception ex)
            {
                // ===== EXCEPTION HANDLING =====
                // Catches unexpected errors during request processing
                // Log with both exception details and ID for troubleshooting
                _logger.LogError(ex, "Unexpected error occurred while retrieving data for ID: {Id}", id);
                
                // Return HTTP 500 Internal Server Error
                // Generic error message + exception details
                // Production: Consider removing ex.Message to prevent information disclosure
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Admin-only endpoint for demonstration purposes. Requires the user to have the "Admin" role. check URL: /api/v1/sample/admin
        /// </summary>
        /// <returns>Admin data</returns>
        /// <response code="200">Returns admin data</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not an admin</response>
        /// <remarks>
        /// **Role-Based Authorization Demonstration**
        /// 
        /// This endpoint showcases role-based access control (RBAC):
        /// - [Authorize] at class level: Requires authentication
        /// - [Authorize(Policy = "AdminOnly")] at method level: Requires "Admin" role
        /// 
        /// **Authorization Flow:**
        /// 1. User sends request with JWT token
        /// 2. JWT middleware validates token (authentication)
        /// 3. Authorization middleware checks token claims
        /// 4. Verifies user has "Admin" role claim
        /// 5. If yes: Execute endpoint
        /// 6. If no: Return 403 Forbidden
        /// 
        /// **HTTP Status Codes:**
        /// - 401 Unauthorized: No token or invalid token (authentication failed)
        /// - 403 Forbidden: Valid token but missing Admin role (authorization failed)
        /// - 200 OK: Valid token with Admin role (success)
        /// 
        /// **Testing:**
        /// - User token (GET /api/v1/auth/token?type=user): Returns 403 Forbidden
        /// - Admin token (GET /api/v1/auth/token?type=admin): Returns 200 OK
        /// 
        /// **Policy Configuration:**
        /// "AdminOnly" policy defined in PresentationServiceExtensions.cs:
        /// options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        /// 
        /// **Production Use Cases:**
        /// - Administrative dashboards
        /// - User management endpoints
        /// - System configuration APIs
        /// - Audit log access
        /// - Reporting and analytics
        /// <summary>
        /// Provides an admin-only endpoint that returns a simple confirmation payload.
        /// </summary>
        /// <remarks>
        /// Requires the caller to satisfy the "AdminOnly" authorization policy. Access is logged for audit purposes using the caller's identity.
        /// </remarks>
        /// <returns>An HTTP 200 response containing a JSON object with a confirmation message and the current user's username.</returns>
        [HttpGet("admin")]
        [Authorize(Policy = "AdminOnly")] // Only users with Admin role can access (additional authorization check)
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GetAdminData()
        {
            // ===== Log Admin Access =====
            // Important for security auditing
            // Track which admins access sensitive endpoints
            // Helps detect unauthorized access attempts or compromised accounts
            // 
            // Log Level: Information (normal operation for admins)
            // Include user identity from JWT token claims
            _logger.LogInformation("Admin {UserId} accessed admin endpoint", User.Identity?.Name);
            
            // ===== Return Admin Data =====
            // Simple response demonstrating endpoint works
            // Production would return actual admin data:
            // - System statistics
            // - User management data
            // - Configuration settings
            // - Audit logs
            // 
            // User.Identity?.Name: Username from JWT token claims
            // Confirms correct user is accessing admin endpoint
            return Ok(new { message = "This is admin-only data", user = User.Identity?.Name });
        }
    }
}