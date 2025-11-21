using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Features.Authentication.Commands
{
    /// <summary>
    /// Command to authenticate a user and generate a JWT token using CQRS pattern.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for user authentication and token generation.
    /// It provides a clean separation between the request (command) and the business logic (handler).
    /// 
    /// Usage:
    /// - Called during login operations
    /// - Generates JWT token with user claims
    /// - Follows existing CQRS patterns in the application
    /// 
    /// Security Features:
    /// - Username validation
    /// - Role-based token generation
    /// - Comprehensive error handling
    /// - Audit logging through handler
    /// - Client context tracking
    /// </remarks>
    public class LoginUserCommand : IRequest<Result<LoginResponseDto>>
    {
        /// <summary>
        /// Username for authentication
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Password for authentication (validated but not stored in this demo)
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Requested role (User or Admin)
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// IP address of the client requesting login (for security logging)
        /// </summary>
        public string? ClientIpAddress { get; }

        /// <summary>
        /// User agent of the client (for security logging)
        /// </summary>
        public string? UserAgent { get; }

        /// <summary>
        /// Initializes a new instance of the LoginUserCommand.
        /// </summary>
        /// <param name="username">Username for authentication</param>
        /// <param name="password">Password for authentication</param>
        /// <param name="role">Requested role (User or Admin)</param>
        /// <param name="clientIpAddress">Client IP address for audit logging</param>
        /// <summary>
        /// Creates a command to authenticate a user and initiate generation of a login response (token and claims).
        /// </summary>
        /// <param name="username">Username used for authentication (required).</param>
        /// <param name="password">Password used for authentication (required).</param>
        /// <param name="role">Requested user role; defaults to "User" when not provided.</param>
        /// <param name="clientIpAddress">Optional client IP address for audit and logging.</param>
        /// <param name="userAgent">Optional client user agent string for audit and logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="username"/> or <paramref name="password"/> is null.</exception>
        public LoginUserCommand(
            string username,
            string password,
            string role = "User",
            string? clientIpAddress = null,
            string? userAgent = null)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            Role = role ?? "User";
            ClientIpAddress = clientIpAddress;
            UserAgent = userAgent;
        }
    }
}