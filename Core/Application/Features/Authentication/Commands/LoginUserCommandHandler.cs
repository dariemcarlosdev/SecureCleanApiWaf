using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.ValueObjects;
using SecureCleanApiWaf.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace SecureCleanApiWaf.Core.Application.Features.Authentication.Commands
{
    /// <summary>
    /// Handler for LoginUserCommand that processes user authentication and JWT token generation.
    /// </summary>
    /// <remarks>
    /// This handler implements the business logic for user login within the CQRS pattern,
    /// integrating with the Domain layer through User entity and IUserRepository.
    /// 
    /// Responsibilities:
    /// - Authenticate user through repository lookup
    /// - Validate user credentials and account status
    /// - Generate JWT token with appropriate claims from User roles
    /// - Record login attempt and update user entity
    /// - Create and persist Token entity for tracking
    /// - Provide detailed response with token metadata
    /// - Log security events for audit purposes
    /// - Handle errors gracefully without exposing sensitive information
    /// 
    /// Integration Points:
    /// - Uses IUserRepository for user lookup and persistence
    /// - Uses ITokenRepository to track issued tokens
    /// - Uses JwtTokenGenerator for token creation
    /// - Follows existing Result<T> pattern for consistent error handling
    /// - Integrates with application logging infrastructure
    /// - Works with User and Token domain entities
    /// 
    /// Domain Integration Benefits:
    /// - Enforces business rules through User entity (CanLogin, RecordLogin)
    /// - Uses Role value object for type-safe role management
    /// - Tracks tokens as domain entities for security auditing
    /// - Publishes domain events for user activities
    /// - Maintains domain invariants and consistency
    /// 
    /// ?? Development Notice:
    /// This is a simplified implementation for development/demonstration.
    /// In production, implement proper authentication with:
    /// - Password hashing and verification (bcrypt, Argon2, PBKDF2)
    /// - Account lockout after failed attempts
    /// - Two-factor authentication
    /// - Azure AD / Identity Server integration
    /// - Rate limiting per IP/user
    /// </remarks>
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<LoginResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of LoginUserCommandHandler.
        /// </summary>
        /// <param name="userRepository">Repository for user data access</param>
        /// <param name="tokenRepository">Repository for token data access</param>
        /// <param name="tokenGenerator">JWT token generator service</param>
        /// <summary>
        /// Initializes a new instance of <see cref="LoginUserCommandHandler"/> with the required repositories, token generator, and logger and validates that none are null.
        /// </summary>
        /// <param name="userRepository">Repository for user persistence and retrieval.</param>
        /// <param name="tokenRepository">Repository for persisting issued tokens.</param>
        /// <param name="tokenGenerator">Component that generates JWTs for authenticated users.</param>
        /// <param name="logger">Logger for audit and diagnostic messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="userRepository"/>, <paramref name="tokenRepository"/>, <paramref name="tokenGenerator"/>, or <paramref name="logger"/> is null.</exception>
        public LoginUserCommandHandler(
            IUserRepository userRepository,
            ITokenRepository tokenRepository,
            JwtTokenGenerator tokenGenerator,
            ILogger<LoginUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
            _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the user login command.
        /// </summary>
        /// <param name="request">The login command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <summary>
        /// Handles a login request: authenticates the user, issues a JWT, records the token and login events, and returns login details and token metadata.
        /// </summary>
        /// <param name="request">LoginUserCommand containing username, password, optional requested role, client IP address, and user agent.</param>
        /// <returns>A Result&lt;LoginResponseDto&gt; containing token, expiry and user information on success; a failed Result with an error message on failure.</returns>
        public async Task<Result<LoginResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // ===== STEP 1: Validate Input =====
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    _logger.LogWarning("Login attempt with empty username. ClientIP: {ClientIP}", 
                        request.ClientIpAddress ?? "unknown");
                    return Result<LoginResponseDto>.Fail("Username is required");
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login attempt with empty password. Username: {Username}, ClientIP: {ClientIP}",
                        request.Username, request.ClientIpAddress ?? "unknown");
                    return Result<LoginResponseDto>.Fail("Password is required");
                }

                // ===== STEP 2: Lookup User from Repository =====
                _logger.LogInformation(
                    "Login attempt for user: {Username}, ClientIP: {ClientIP}",
                    request.Username, request.ClientIpAddress ?? "unknown");

                var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning(
                        "Login failed - user not found. Username: {Username}, ClientIP: {ClientIP}",
                        request.Username, request.ClientIpAddress ?? "unknown");
                    
                    // Generic error message to prevent username enumeration
                    return Result<LoginResponseDto>.Fail("Invalid username or password");
                }

                // ===== STEP 3: Validate User Can Login =====
                if (!user.CanLogin())
                {
                    _logger.LogWarning(
                        "Login denied - account status: {Status}. Username: {Username}, ClientIP: {ClientIP}",
                        user.Status, request.Username, request.ClientIpAddress ?? "unknown");
                    
                    return Result<LoginResponseDto>.Fail($"Account is {user.Status}. Please contact support.");
                }

                // ===== STEP 4: Verify Password (Simplified for Demo) =====
                // ?? In production, verify password hash:
                // var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
                // if (!passwordValid)
                // {
                //     user.RecordFailedLogin();
                //     await _userRepository.UpdateAsync(user, cancellationToken);
                //     await _userRepository.SaveChangesAsync(cancellationToken);
                //     return Result<LoginResponseDto>.Fail("Invalid username or password");
                // }

                // For demo: Accept any non-empty password
                // In production, this is where password verification happens

                // ===== STEP 5: Get User Roles (from Domain Entity) =====
                // Use roles from User entity (domain-driven approach)
                var roleStrings = user.Roles.Select(r => r.Name).ToArray();
                
                // If user wants admin role but doesn't have it, deny
                if (string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase) 
                    && !user.IsAdmin())
                {
                    _logger.LogWarning(
                        "Login denied - insufficient privileges. Username: {Username}, RequestedRole: {Role}, UserRoles: {UserRoles}, ClientIP: {ClientIP}",
                        request.Username, request.Role, string.Join(", ", roleStrings), request.ClientIpAddress ?? "unknown");
                    
                    return Result<LoginResponseDto>.Fail("Insufficient privileges for requested role");
                }

                // ===== STEP 6: Generate JWT Token =====
                var jwtToken = _tokenGenerator.GenerateToken(
                    user.Id.ToString(), 
                    user.Username, 
                    roleStrings);

                // ===== STEP 7: Extract Token Information =====
                var tokenInfo = ExtractTokenInfo(jwtToken);

                // ===== STEP 8: Create Token Domain Entity =====
                var tokenEntity = Token.Create(
                    tokenId: tokenInfo.Jti ?? Guid.NewGuid().ToString(),
                    userId: user.Id,
                    username: user.Username,
                    expiresAt: tokenInfo.ExpiresAt,
                    type: Core.Domain.Enums.TokenType.AccessToken,
                    clientIp: request.ClientIpAddress,
                    userAgent: request.UserAgent);

                // Store token in repository for tracking and potential blacklisting
                await _tokenRepository.AddAsync(tokenEntity, cancellationToken);

                // ===== STEP 9: Record Successful Login in User Entity =====
                user.RecordLogin(request.ClientIpAddress, request.UserAgent);
                await _userRepository.UpdateAsync(user, cancellationToken);

                // ===== STEP 10: Persist Changes =====
                await _userRepository.SaveChangesAsync(cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);

                // ===== STEP 11: Log Successful Login =====
                _logger.LogInformation(
                    "Login successful. Username: {Username}, Roles: {Roles}, TokenId: {TokenId}, ClientIP: {ClientIP}",
                    user.Username, string.Join(", ", roleStrings), tokenEntity.TokenId, request.ClientIpAddress ?? "unknown");

                // ===== STEP 12: Build Success Response =====
                var response = new LoginResponseDto
                {
                    Token = jwtToken,
                    TokenType = "Bearer",
                    ExpiresIn = (int)(tokenInfo.ExpiresAt - tokenInfo.IssuedAt).TotalSeconds,
                    Username = user.Username,
                    Roles = roleStrings,
                    TokenId = tokenEntity.TokenId,
                    IssuedAt = tokenInfo.IssuedAt,
                    ExpiresAt = tokenInfo.ExpiresAt,
                    Message = "Login successful. Use token in Authorization header: 'Bearer {token}'"
                };

                return Result<LoginResponseDto>.Ok(response);
            }
            catch (Exception ex)
            {
                // ===== Error Handling =====
                _logger.LogError(ex,
                    "Error processing LoginUserCommand for user: {Username}, ClientIP: {ClientIP}",
                    request.Username, request.ClientIpAddress ?? "unknown");

                return Result<LoginResponseDto>.Fail(
                    "An error occurred during login. Please try again.");
            }
        }

        /// <summary>
        /// Extracts information from a generated JWT token.
        /// </summary>
        /// <param name="jwtToken">The JWT token to parse</param>
        /// <summary>
        /// Parses a JWT string and returns its identifier and timing metadata.
        /// </summary>
        /// <param name="jwtToken">The compact JWT to parse.</param>
        /// <returns>
        /// A TokenInfo containing the token's JTI (if present), IssuedAt, and ExpiresAt. If parsing or claim conversion fails,
        /// Jti will be a new GUID string and IssuedAt/ExpiresAt will use current UTC and current UTC + 30 minutes respectively.
        /// </returns>
        private TokenInfo ExtractTokenInfo(string jwtToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwtToken);

                // Extract JTI (JWT ID) claim
                var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                // Extract issued at claim (iat)
                var iatClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat)?.Value;
                var issuedAt = DateTime.UtcNow;
                if (long.TryParse(iatClaim, out var iat))
                {
                    issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime;
                }

                // Extract expiration claim (exp)
                var expClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                var expiresAt = DateTime.UtcNow.AddMinutes(30); // Default fallback
                if (long.TryParse(expClaim, out var exp))
                {
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                }

                return new TokenInfo
                {
                    Jti = jti,
                    IssuedAt = issuedAt,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract token information from JWT");
                return new TokenInfo
                {
                    Jti = Guid.NewGuid().ToString(),
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                };
            }
        }

        /// <summary>
        /// Internal class to hold parsed token information.
        /// </summary>
        private class TokenInfo
        {
            public string? Jti { get; set; }
            public DateTime IssuedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}