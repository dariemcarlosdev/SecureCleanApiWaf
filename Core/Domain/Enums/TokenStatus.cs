namespace CleanArchitecture.ApiTemplate.Core.Domain.Enums
{
    /// <summary>
    /// Represents the current status of a JWT authentication token.
    /// </summary>
    /// <remarks>
    /// Token status is critical for security and access control in JWT-based authentication.
    /// 
    /// Status Lifecycle:
    /// ```
    ///     [Token Generation]
    ///            ?
    ///      ????????????
    ///      ?  Active  ? ??????? Normal Usage ??????
    ///      ????????????                           ?
    ///            ?                                ?
    ///            ? User Logout                    ?
    ///            ? Security Breach                ?
    ///            ? Admin Revoke                   ?
    ///            ?                                ?
    ///      ????????????                           ?
    ///      ? Revoked  ?                           ?
    ///      ????????????                           ?
    ///                                             ?
    ///      ????????????                           ?
    ///      ? Expired  ? ?????? Time Passes ????????
    ///      ????????????
    /// ```
    /// 
    /// Security Considerations:
    /// - Active tokens are the only ones accepted for authentication
    /// - Revoked tokens must be checked against blacklist
    /// - Expired tokens are automatically rejected
    /// - Token status changes should be logged for audit trails
    /// 
    /// Implementation Notes:
    /// - Use distributed cache (Redis) for blacklist storage
    /// - Token blacklist entries expire with the token
    /// - Check token status before processing every request
    /// - Log all status transitions for security monitoring
    /// </remarks>
    public enum TokenStatus
    {
        /// <summary>
        /// Token is active and can be used for authentication.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Token is within its validity period
        /// - Not revoked or blacklisted
        /// - Can be used for API requests
        /// - Subject to all security validations
        /// 
        /// Validation Checks:
        /// ```csharp
        /// public bool IsValid(Token token)
        /// {
        ///     return token.Status == TokenStatus.Active 
        ///         && token.ExpiresAt > DateTime.UtcNow
        ///         && !IsBlacklisted(token.TokenId);
        /// }
        /// ```
        /// 
        /// Common Operations:
        /// - Authenticate API requests ?
        /// - Access protected resources ?
        /// - Refresh token exchange ?
        /// - User session maintenance ?
        /// 
        /// Security Validations Required:
        /// - Signature verification
        /// - Expiration check
        /// - Issuer validation
        /// - Audience validation
        /// - Not in blacklist
        /// - Claims validation
        /// </remarks>
        Active = 1,

        /// <summary>
        /// Token has been explicitly revoked and should not be accepted.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Token was valid but is now invalidated
        /// - Cannot be used even if not expired
        /// - Stored in blacklist cache
        /// - Requires active checking during authentication
        /// 
        /// Common Operations:
        /// - Authenticate API requests ?
        /// - Blacklist storage ?
        /// - Security audit logging ?
        /// - Client notification ?
        /// 
        /// Revocation Triggers:
        /// 
        /// **User-Initiated:**
        /// - Explicit logout from application
        /// - Logout from all devices
        /// - Account deactivation
        /// - Password change (optional policy)
        /// 
        /// **Security-Initiated:**
        /// - Suspicious activity detection
        /// - Compromised account notification
        /// - Token theft detection
        /// - Failed security checks
        /// 
        /// **Admin-Initiated:**
        /// - Manual token revocation
        /// - Account suspension
        /// - Policy violation enforcement
        /// - Emergency security response
        /// 
        /// Implementation Pattern:
        /// ```csharp
        /// public async Task RevokeTokenAsync(string tokenId, string reason)
        /// {
        ///     var blacklistEntry = new TokenBlacklistEntry
        ///     {
        ///         TokenId = tokenId,
        ///         RevokedAt = DateTime.UtcNow,
        ///         Reason = reason,
        ///         ExpiresAt = GetTokenExpiration(tokenId)
        ///     };
        ///     
        ///     await _cache.SetAsync(
        ///         $"blacklist:{tokenId}",
        ///         blacklistEntry,
        ///         blacklistEntry.ExpiresAt - DateTime.UtcNow);
        ///         
        ///     _logger.LogWarning(
        ///         "Token {TokenId} revoked. Reason: {Reason}",
        ///         tokenId, reason);
        /// }
        /// ```
        /// 
        /// Client-Side Handling:
        /// - Remove token from storage immediately
        /// - Clear session state
        /// - Redirect to login page
        /// - Show appropriate message
        /// - Don't retry with same token
        /// 
        /// Server-Side Handling:
        /// - Return 401 Unauthorized
        /// - Log the attempted use
        /// - Include reason in response (optional)
        /// - Monitor for abuse patterns
        /// </remarks>
        Revoked = 2,

        /// <summary>
        /// Token has passed its expiration time and is no longer valid.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Token's exp claim time has passed
        /// - Natural end of token lifetime
        /// - No blacklist storage needed
        /// - Automatically rejected by JWT validation
        /// 
        /// Common Operations:
        /// - Authenticate API requests ?
        /// - Token refresh flow ?
        /// - Re-authentication required ?
        /// - Session renewal ?
        /// 
        /// Expiration Handling:
        /// 
        /// **Server-Side:**
        /// ```csharp
        /// services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        ///     .AddJwtBearer(options =>
        ///     {
        ///         options.TokenValidationParameters = new TokenValidationParameters
        ///         {
        ///             ValidateLifetime = true, // Reject expired tokens
        ///             ClockSkew = TimeSpan.FromMinutes(5) // Allow 5min buffer
        ///         };
        ///     });
        /// ```
        /// 
        /// **Client-Side:**
        /// ```javascript
        /// // Check if token will expire soon
        /// function isTokenExpiringSoon(token) {
        ///     const payload = JSON.parse(atob(token.split('.')[1]));
        ///     const expirationTime = payload.exp * 1000; // Convert to ms
        ///     const now = Date.now();
        ///     const fiveMinutes = 5 * 60 * 1000;
        ///     
        ///     return (expirationTime - now) < fiveMinutes;
        /// }
        /// 
        /// // Proactive token refresh
        /// if (isTokenExpiringSoon(currentToken)) {
        ///     currentToken = await refreshToken();
        /// }
        /// ```
        /// 
        /// Token Lifetime Best Practices:
        /// 
        /// **Access Tokens (Short-lived):**
        /// - Duration: 15 minutes to 1 hour
        /// - Purpose: API authentication
        /// - Refresh: Using refresh tokens
        /// - Risk: Lower (short exposure window)
        /// 
        /// **Refresh Tokens (Long-lived):**
        /// - Duration: 7 to 90 days
        /// - Purpose: Obtain new access tokens
        /// - Storage: Secure, HTTP-only cookies
        /// - Risk: Higher (longer exposure window)
        /// - Additional security: Rotation, binding
        /// 
        /// Error Response Example:
        /// ```json
        /// {
        ///     "error": "token_expired",
        ///     "error_description": "The access token has expired",
        ///     "expired_at": "2024-01-15T10:30:00Z",
        ///     "actions": {
        ///         "refresh": "/api/auth/refresh",
        ///         "login": "/api/auth/login"
        ///     }
        /// }
        /// ```
        /// 
        /// Monitoring Recommendations:
        /// - Track token expiration rates
        /// - Monitor refresh patterns
        /// - Alert on unusual expiration spikes
        /// - Measure user experience impact
        /// </remarks>
        Expired = 3
    }
}
