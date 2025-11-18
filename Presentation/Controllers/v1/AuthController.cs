using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureCleanApiWaf.Infrastructure.Security;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Application.Features.Authentication.Commands;
using MediatR;
using System.IdentityModel.Tokens.Jwt;

namespace SecureCleanApiWaf.Presentation.Controllers.v1
{
    /// <summary>
    /// Authentication controller for token generation.
    /// WARNING: This is for DEVELOPMENT/DEMO purposes only! 
    /// In production, use a proper identity provider (Azure AD, IdentityServer, etc.)
    /// </summary>
    /// <remarks>
    /// This controller demonstrates JWT token generation for testing and development.
    /// It bypasses real authentication and should NEVER be used in production.
    /// 
    /// Production alternatives:
    /// - Azure Active Directory / Microsoft Entra ID
    /// - IdentityServer / Duende IdentityServer
    /// - Auth0, Okta, or other OAuth 2.0 providers
    /// 
    /// This implementation is useful for:
    /// - Local development testing
    /// - API integration testing
    /// - Learning JWT authentication patterns
    /// - Portfolio demonstrations
    /// </remarks>
    [ApiController]
    [Route("api/v1/auth")]
    [AllowAnonymous]  // Allows unauthenticated access (anyone can get a token)
    [ApiExplorerSettings(IgnoreApi = false)] // Ensure it appears in Swagger
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly ILogger<AuthController> _logger;
        private readonly IMediator _mediator;

        /// <summary>
        /// Constructor: Injects dependencies for token generation, logging, and MediatR
        /// </summary>
        /// <param name="tokenGenerator">Service for creating JWT tokens</param>
        /// <param name="logger">Logger for tracking token generation requests</param>
        /// <param name="mediator">MediatR instance for CQRS pattern</param>
        public AuthController(
            JwtTokenGenerator tokenGenerator, 
            ILogger<AuthController> logger,
            IMediator mediator)
        {
            _tokenGenerator = tokenGenerator;
            _logger = logger;
            _mediator = mediator;
        }

        /// <summary>
        /// Generates a JWT token for testing purposes.
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token</returns>
        /// <remarks>
        /// **CQRS IMPLEMENTATION** - Uses LoginUserCommand for authentication
        /// 
        /// This endpoint now uses the CQRS pattern with MediatR for login operations:
        /// 1. Creates LoginUserCommand with credentials and audit info
        /// 2. MediatR sends command to LoginUserCommandHandler
        /// 3. Handler generates JWT token with appropriate claims
        /// 4. Returns structured response with token metadata
        /// 
        /// Sample request:
        /// 
        ///     POST /api/v1/auth/login
        ///     {
        ///         "username": "testuser",
        ///         "password": "any_password_works",
        ///         "role": "User"
        ///     }
        ///     
        /// Use role "Admin" to get admin access.
        /// 
        /// **CQRS Benefits:**
        /// - Clean separation of concerns (controller vs. business logic)
        /// - Testable business logic in command handlers
        /// - Consistent error handling through Result<T> pattern
        /// - Comprehensive audit logging
        /// - Easy to extend with additional behaviors (validation, caching, etc.)
        /// </remarks>
        /// <response code="200">Returns the JWT token with metadata</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // ===== STEP 1: Input Validation =====
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest(new { error = "Username is required" });
                }

                // ===== STEP 2: Create CQRS Command for Login =====
                var command = new LoginUserCommand(
                    username: request.Username,
                    password: request.Password ?? string.Empty,
                    role: request.Role ?? "User",
                    clientIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers.UserAgent.FirstOrDefault()
                );

                // ===== STEP 3: Execute Command via MediatR =====
                var result = await _mediator.Send(command, HttpContext.RequestAborted);

                // ===== STEP 4: Handle Command Result =====
                if (!result.Success)
                {
                    _logger.LogWarning("Login failed via CQRS: {Error}", result.Error);
                    return BadRequest(new { error = result.Error });
                }

                // ===== STEP 5: Return Success Response with CQRS Data =====
                var response = result.Data;

                return Ok(new
                {
                    // Token information
                    token = response.Token,
                    tokenType = response.TokenType,
                    expiresIn = response.ExpiresIn,
                    
                    // User information
                    username = response.Username,
                    roles = response.Roles,
                    
                    // Token metadata (useful for debugging and tracking)
                    tokenId = response.TokenId,
                    issuedAt = response.IssuedAt,
                    expiresAt = response.ExpiresAt,
                    
                    // Processing metadata
                    processingMethod = "CQRS_Command_Pattern",
                    
                    // Helper message
                    message = response.Message
                });
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, "Error during CQRS login process for user: {Username}", request.Username);

                // Don't expose internal error details to clients
                return StatusCode(500, new
                {
                    error = "login_error",
                    error_description = "An error occurred during login processing",
                    message = "Please try again or contact support if the problem persists",
                    timestamp = DateTime.UtcNow,
                    request_id = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Securely logs out a user by blacklisting their JWT token using CQRS pattern.
        /// </summary>
        /// <returns>Logout confirmation with security information</returns>
        /// <remarks>
        /// **SECURE LOGOUT IMPLEMENTATION WITH CQRS**
        /// 
        /// This endpoint implements proper JWT logout using the CQRS pattern with MediatR:
        /// 1. Extracts the JWT token from the Authorization header
        /// 2. Uses BlacklistTokenCommand to handle the logout business logic
        /// 3. Returns comprehensive logout information and security recommendations
        /// 
        /// CQRS Benefits:
        /// - Clean separation of concerns (controller vs. business logic)
        /// - Testable business logic in command handlers
        /// - Consistent error handling through Result<T> pattern
        /// - Automatic caching and pipeline behaviors
        /// - Easy to extend with additional behaviors (validation, logging, etc.)
        /// 
        /// Security Features:
        /// - Token blacklisting prevents reuse of logged-out tokens
        /// - Comprehensive security logging and auditing
        /// - Graceful error handling without information leakage
        /// - Client-side security recommendations
        /// 
        /// Client Requirements:
        /// - Must send Authorization header: "Bearer {token}"
        /// - Should remove token from client-side storage after logout
        /// - Should redirect to login page or update UI state
        /// 
        /// Example Usage:
        /// ```
        /// POST /api/v1/auth/logout
        /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        /// ```
        /// </remarks>
        /// <response code="200">Logout successful, token blacklisted</response>
        /// <response code="400">No token provided or invalid token format</response>
        /// <response code="401">Token is already invalid or expired</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // ===== STEP 1: Extract JWT Token from Authorization Header =====
                var token = ExtractTokenFromRequest();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Logout attempted without token. RemoteIP: {RemoteIP}, UserAgent: {UserAgent}",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        Request.Headers.UserAgent.FirstOrDefault() ?? "unknown");
                    
                    return BadRequest(new
                    {
                        error = "missing_token",
                        error_description = "No JWT token provided in Authorization header",
                        message = "To logout, you must provide a valid JWT token in the Authorization header",
                        example = "Authorization: Bearer your-jwt-token-here"
                    });
                }

                // ===== STEP 2: Create CQRS Command for Token Blacklisting. Command encapsulates all logout logic =====
                var command = new BlacklistTokenCommand(
                    jwtToken: token,
                    reason: "user_logout",
                    clientIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers.UserAgent.FirstOrDefault()
                );

                // ===== STEP 3: Execute Command via MediatR =====
                var result = await _mediator.Send(command, HttpContext.RequestAborted);

                // ===== STEP 4: Handle Command Result =====
                if (!result.Success)
                {
                    _logger.LogWarning("Token blacklisting failed: {Error}", result.Error);
                    
                    return BadRequest(new
                    {
                        error = "blacklist_failed",
                        error_description = result.Error,
                        message = "Failed to blacklist token during logout"
                    });
                }

                // ===== STEP 5: Return Success Response with CQRS Data =====
                var response = result.Data;
                
                return Ok(new
                {
                    message = "Logout successful via CQRS pattern",
                    status = response.Status,
                    details = new
                    {
                        // Safe information from CQRS response
                        token_id = response.TokenId,
                        username = response.Username,
                        blacklisted_at = response.BlacklistedAt,
                        expires_at = response.TokenExpiresAt,
                        processing_method = "CQRS_Command_Pattern",
                        
                        // Security recommendations from command handler
                        client_actions = response.ClientRecommendations
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex, "Error during CQRS logout process");
                
                // Don't expose internal error details to clients
                return StatusCode(500, new
                {
                    error = "logout_error",
                    error_description = "An error occurred during logout processing",
                    message = "Please try again or contact support if the problem persists",
                    timestamp = DateTime.UtcNow,
                    request_id = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) for testing purposes, returning a minimal token response based on the
        /// requested type.
        /// </summary>
        /// <remarks>
        /// This endpoint is intended for development and testing scenarios where a quick JWT is
        /// needed. No authentication or request body is required; supply the desired token type as a query parameter.
        /// The response includes a ready-to-use Authorization header value for integration with HTTP clients.
        /// 
        /// Simplified endpoint for quick token generation without JSON body.
        /// Perfect for:
        /// - Quick Swagger testing
        /// - cURL commands
        /// - Browser testing
        /// - Automated testing scripts
        /// 
        /// Usage Examples:
        /// - GET /api/v1/auth/token?type=user  (generates user token)
        /// - GET /api/v1/auth/token?type=admin (generates admin token)
        /// - GET /api/v1/auth/token            (defaults to user token)
        /// </remarks>
        /// <param name="type">The type of token to generate. Specify "admin" to receive a token with both "User" and "Admin" roles;
        /// otherwise, a standard user token is returned. The comparison is case-insensitive. Defaults to "user" if not
        /// specified.</param>
        /// <returns>An <see cref="IActionResult"/> containing a JSON object with the generated token, token type, requested
        /// type, assigned roles, and usage instructions for the Authorization header.</returns
        /// <response code="200">Returns the JWT token</response>
        [HttpGet("token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetToken([FromQuery] string type = "user")
        {
            // ===== Quick Token Generation =====
            // This endpoint uses convenience methods from JwtTokenGenerator
            // No request body needed - just query parameter
            
            string token;
            string[] roles;

            // ===== Determine Token Type =====
            // Check if admin token is requested (case-insensitive)
            if (string.Equals(type, "admin", StringComparison.OrdinalIgnoreCase))
            {
                // ===== Generate Admin Token =====
                // Uses convenience method that creates token with:
                // - Random GUID user ID
                // - Default username "admin"
                // - Both "User" and "Admin" roles
                token = _tokenGenerator.GenerateAdminToken();
                roles = new[] { "User", "Admin" };
            }
            else
            {
                // ===== Generate User Token =====
                // Uses convenience method that creates token with:
                // - Random GUID user ID
                // - Default username "testuser"
                // - Single "User" role
                token = _tokenGenerator.GenerateUserToken();
                roles = new[] { "User" };
            }

            // ===== Return Simplified Token Response =====
            // Returns minimal but sufficient information for testing
            return Ok(new
            {
                // The JWT token string
                token,
                
                // Token type for Authorization header
                tokenType = "Bearer",
                
                // Echo back the requested type for confirmation
                type,
                
                // Roles assigned to this token
                roles,
                
                // Usage instruction showing exact header format
                // Copy-paste ready for cURL, Postman, or HTTP clients
                usage = $"Add to headers: Authorization: Bearer {token}"
            });
        }

        /// <summary>
        /// Extracts JWT token from the Authorization header.
        /// </summary>
        /// <returns>JWT token string or null if not found</returns>
        private string? ExtractTokenFromRequest()
        {
            try
            {
                var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();
                
                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return null;
                }

                const string bearerPrefix = "Bearer ";
                if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var token = authorizationHeader[bearerPrefix.Length..].Trim();
                
                // Basic JWT format validation (3 parts separated by dots)
                if (!string.IsNullOrEmpty(token) && token.Count(c => c == '.') == 2)
                {
                    return token;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Login request model for POST /api/v1/auth/login endpoint
    /// </summary>
    /// <remarks>
    /// In a real application, this model would include:
    /// - Password validation attributes
    /// - Email validation
    /// - Additional security fields (2FA code, captcha, etc.)
    /// 
    /// For this demo:
    /// - Password is ignored (no validation)
    /// - Any username is accepted
    /// - Role determines access level
    /// </remarks>
    public class LoginRequest
    {
        /// <summary>
        /// Username (any value accepted for demo)
        /// </summary>
        /// <example>john.doe</example>
        /// <remarks>
        /// In production, this would be validated against a user database.
        /// Here, any non-empty string is accepted.
        /// </remarks>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Password (ignored in this demo implementation)
        /// </summary>
        /// <example>password123</example>
        /// <remarks>
        /// ?? SECURITY WARNING: This field is completely ignored in this demo!
        /// 
        /// In production, you would:
        /// 1. Hash the password using bcrypt, Argon2, or PBKDF2
        /// 2. Compare against stored hash in database
        /// 3. Implement account lockout after failed attempts
        /// 4. Log authentication attempts for security monitoring
        /// 5. Enforce strong password policies
        /// 6. Use HTTPS to protect credentials in transit
        /// 
        /// Never store passwords in plain text!
        /// Never log passwords!
        /// </remarks>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Role to assign: "User" or "Admin"
        /// </summary>
        /// <example>User</example>
        /// <remarks>
        /// Determines the access level of the generated token:
        /// 
        /// - "User": Standard access to regular endpoints
        ///   - Can access GET /api/v1/sample (protected endpoint)
        ///   - Cannot access GET /api/v1/sample/admin (admin-only endpoint)
        /// 
        /// - "Admin": Full access to all endpoints
        ///   - Can access all user endpoints
        ///   - Can access admin-only endpoints
        ///   - Gets both "User" and "Admin" roles in token
        /// 
        /// In production, roles would be:
        /// - Retrieved from user database
        /// - Based on user's actual permissions
        /// - Not client-specified (security risk!)
        /// </remarks>
        public string Role { get; set; } = "User";
    }
}
