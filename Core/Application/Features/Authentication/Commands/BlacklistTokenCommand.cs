using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Models;

namespace SecureCleanApiWaf.Core.Application.Features.Authentication.Commands
{
    /// <summary>
    /// Command to blacklist a JWT token during logout operation.
    /// </summary>
    /// <remarks>
    /// This command implements the CQRS pattern for token blacklisting operations.
    /// It provides a clean separation between the request (command) and the business logic (handler).
    /// 
    /// Usage:
    /// - Called during logout operations
    /// - Ensures token cannot be reused after logout
    /// - Follows existing CQRS patterns in the application
    /// 
    /// Security Features:
    /// - Token validation before blacklisting
    /// - Comprehensive error handling
    /// - Audit logging through handler
    /// </remarks>
    public class BlacklistTokenCommand : IRequest<Result<BlacklistTokenResponse>>
    {
        /// <summary>
        /// The JWT token to be blacklisted
        /// </summary>
        public string JwtToken { get; }

        /// <summary>
        /// Optional reason for blacklisting (for audit purposes)
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// IP address of the client requesting logout (for security logging)
        /// </summary>
        public string? ClientIpAddress { get; }

        /// <summary>
        /// User agent of the client (for security logging)
        /// </summary>
        public string? UserAgent { get; }

        /// <summary>
        /// Initializes a new instance of the BlacklistTokenCommand.
        /// </summary>
        /// <param name="jwtToken">The JWT token to blacklist</param>
        /// <param name="reason">Optional reason for blacklisting</param>
        /// <param name="clientIpAddress">Client IP address for audit logging</param>
        /// <summary>
        /// Creates a command that requests blacklisting of the specified JWT token for logout/audit purposes.
        /// </summary>
        /// <param name="jwtToken">The JWT to blacklist.</param>
        /// <param name="reason">Optional audit reason for the blacklist operation.</param>
        /// <param name="clientIpAddress">Optional client IP address for security logging.</param>
        /// <param name="userAgent">Optional client user agent for security logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="jwtToken"/> is null.</exception>
        public BlacklistTokenCommand(
            string jwtToken, 
            string? reason = null, 
            string? clientIpAddress = null, 
            string? userAgent = null)
        {
            JwtToken = jwtToken ?? throw new ArgumentNullException(nameof(jwtToken));
            Reason = reason;
            ClientIpAddress = clientIpAddress;
            UserAgent = userAgent;
        }
    }

    /// <summary>
    /// Response model for token blacklisting operation.
    /// </summary>
    /// <remarks>
    /// Contains information about the blacklisting operation result,
    /// useful for client-side handling and audit purposes.
    /// </remarks>
    public class BlacklistTokenResponse
    {
        /// <summary>
        /// JWT ID (JTI) of the blacklisted token
        /// </summary>
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// Username associated with the token
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// When the token was blacklisted
        /// </summary>
        public DateTime BlacklistedAt { get; set; }

        /// <summary>
        /// When the token naturally expires
        /// </summary>
        public DateTime TokenExpiresAt { get; set; }

        /// <summary>
        /// Status of the blacklisting operation
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Additional details about the operation
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Security recommendations for the client
        /// </summary>
        public string[] ClientRecommendations { get; set; } = Array.Empty<string>();
    }
}