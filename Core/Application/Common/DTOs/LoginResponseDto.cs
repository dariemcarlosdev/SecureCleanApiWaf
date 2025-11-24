namespace CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs
{
    /// <summary>
    /// DTO for login response containing JWT token and user information.
    /// Returned upon successful authentication via the login endpoint.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// JWT token string for authentication.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token type (always "Bearer" for JWT).
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration time in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Username that was authenticated.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Roles assigned to the user in the token.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// JWT ID (JTI) for token tracking and blacklisting.
        /// </summary>
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// When the token was issued (UTC).
        /// </summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>
        /// When the token expires (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Helper message for API consumers.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
