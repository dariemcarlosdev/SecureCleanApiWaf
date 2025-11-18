namespace SecureCleanApiWaf.Core.Domain.Enums
{
    /// <summary>
    /// Represents the type of authentication token.
    /// </summary>
    /// <remarks>
    /// Different token types serve different purposes in the authentication flow
    /// and have different security requirements and lifetimes.
    /// 
    /// Token Type Comparison:
    /// 
    /// ```
    /// ???????????????????????????????????????????????????????
    /// ?   Property     ?  Access Token   ?  Refresh Token   ?
    /// ???????????????????????????????????????????????????????
    /// ?   Purpose      ?  API Access     ?  Get New Tokens  ?
    /// ?   Lifetime     ?  Short (15-60m) ?  Long (7-90d)    ?
    /// ?   Storage      ?  Memory/Storage ?  Secure Storage  ?
    /// ?   Exposure     ?  High (each req)?  Low (refresh)   ?
    /// ?   Risk         ?  Lower          ?  Higher          ?
    /// ?   Revocation   ?  Blacklist      ?  Database        ?
    /// ???????????????????????????????????????????????????????
    /// ```
    /// 
    /// Authentication Flow:
    /// ```
    /// [Login Request]
    ///       ?
    ///   Generate Both Token Types
    ///       ?
    /// ???????????????????  ????????????????????
    /// ?  Access Token   ?  ?  Refresh Token   ?
    /// ?  (Short-lived)  ?  ?  (Long-lived)    ?
    /// ???????????????????  ????????????????????
    ///       ?                      ?
    ///       ?                      ?
    /// [API Requests]               ?
    ///   15 min later...            ?
    ///       ?                      ?
    /// [Token Expired]              ?
    ///       ?                      ?
    /// [Refresh Request] ????????????
    ///       ?
    /// [New Access Token]
    /// ```
    /// 
    /// Security Architecture:
    /// - Access tokens: Stateless, cached blacklist for revocation
    /// - Refresh tokens: Stateful, stored in database with usage tracking
    /// - Both: Should be JWT format with proper claims
    /// - Both: Should have unique JTI (JWT ID) for tracking
    /// </remarks>
    public enum TokenType
    {
        /// <summary>
        /// Access token used for authenticating API requests.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Short-lived (15 minutes to 1 hour)
        /// - Used in Authorization header for each API request
        /// - Contains user identity and permissions (claims)
        /// - Lightweight and stateless for performance
        /// - Cached blacklist for logout/revocation
        /// 
        /// Structure:
        /// ```json
        /// {
        ///   "typ": "JWT",
        ///   "alg": "HS256"
        /// }
        /// .
        /// {
        ///   "sub": "user-id-123",
        ///   "username": "john.doe",
        ///   "email": "john@example.com",
        ///   "roles": ["User", "Admin"],
        ///   "jti": "unique-token-id",
        ///   "iat": 1609459200,
        ///   "exp": 1609462800,
        ///   "iss": "https://api.myapp.com",
        ///   "aud": "https://api.myapp.com"
        /// }
        /// .
        /// [signature]
        /// ```
        /// 
        /// Usage Pattern:
        /// ```csharp
        /// // Client sends with every API request
        /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        /// 
        /// // Server validates on each request
        /// var principal = await HttpContext.AuthenticateAsync();
        /// if (!principal.Succeeded)
        ///     return Unauthorized();
        /// ```
        /// 
        /// Claims Typically Included:
        /// - **sub (Subject)**: User ID
        /// - **username**: Username for display
        /// - **email**: User email
        /// - **roles**: User roles/permissions
        /// - **jti (JWT ID)**: Unique token identifier
        /// - **iat (Issued At)**: Creation timestamp
        /// - **exp (Expiration)**: Expiration timestamp
        /// - **iss (Issuer)**: Token issuer
        /// - **aud (Audience)**: Intended recipient
        /// 
        /// Security Best Practices:
        /// 
        /// **Lifetime:**
        /// - Use short expiration (15-60 minutes)
        /// - Longer lifetime = higher security risk
        /// - Balance security vs. user experience
        /// - Consider sensitivity of data/operations
        /// 
        /// **Storage:**
        /// - Client: Memory, SessionStorage, or localStorage
        /// - Avoid storing in cookies (CSRF risk)
        /// - Clear on logout immediately
        /// - Never log full token content
        /// 
        /// **Transmission:**
        /// - Always use HTTPS/TLS
        /// - Authorization header preferred
        /// - Never in URL parameters (logged)
        /// - Never in non-secure cookies
        /// 
        /// **Validation:**
        /// - Verify signature
        /// - Check expiration
        /// - Validate issuer and audience
        /// - Check against blacklist (logout)
        /// - Validate required claims
        /// 
        /// **Revocation:**
        /// - Store JTI in Redis cache on logout
        /// - TTL matches token expiration
        /// - Check blacklist on each request
        /// - Fast O(1) lookup performance
        /// 
        /// Error Handling:
        /// ```csharp
        /// // Expired token
        /// if (tokenExpired)
        ///     return Problem(
        ///         statusCode: 401,
        ///         title: "Token expired",
        ///         detail: "Please refresh your token");
        /// 
        /// // Revoked token
        /// if (tokenRevoked)
        ///     return Problem(
        ///         statusCode: 401,
        ///         title: "Token revoked",
        ///         detail: "Please log in again");
        /// 
        /// // Invalid signature
        /// if (invalidSignature)
        ///     return Problem(
        ///         statusCode: 401,
        ///         title: "Invalid token",
        ///         detail: "Token signature verification failed");
        /// ```
        /// 
        /// Performance Considerations:
        /// - Token validation is CPU-intensive (signature verification)
        /// - Cache validated tokens in memory briefly (5-10 seconds)
        /// - Use efficient blacklist lookup (Redis with key prefix)
        /// - Consider token size (affects request overhead)
        /// </remarks>
        AccessToken = 1,

        /// <summary>
        /// Refresh token used to obtain new access tokens.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Long-lived (7 to 90 days)
        /// - Used only at refresh endpoint
        /// - Stored securely (HTTP-only cookie or secure storage)
        /// - Typically one-time use (rotation)
        /// - Stored in database for revocation
        /// 
        /// Structure:
        /// ```json
        /// {
        ///   "typ": "JWT",
        ///   "alg": "HS256"
        /// }
        /// .
        /// {
        ///   "sub": "user-id-123",
        ///   "jti": "unique-refresh-token-id",
        ///   "token_type": "refresh",
        ///   "iat": 1609459200,
        ///   "exp": 1617235200,
        ///   "iss": "https://api.myapp.com"
        /// }
        /// .
        /// [signature]
        /// ```
        /// 
        /// Usage Pattern:
        /// ```csharp
        /// // Client requests new access token
        /// POST /api/auth/refresh
        /// {
        ///     "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI..."
        /// }
        /// 
        /// // Server validates and returns new tokens
        /// {
        ///     "accessToken": "new-access-token",
        ///     "refreshToken": "new-refresh-token", // Token rotation
        ///     "expiresIn": 3600
        /// }
        /// ```
        /// 
        /// Security Best Practices:
        /// 
        /// **Token Rotation:**
        /// - Issue new refresh token with each use
        /// - Invalidate old refresh token immediately
        /// - Prevents replay attacks
        /// - Limits impact of token theft
        /// 
        /// ```csharp
        /// public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        /// {
        ///     // Validate current refresh token
        ///     var tokenRecord = await _db.RefreshTokens
        ///         .FirstOrDefaultAsync(t => t.Token == refreshToken);
        ///     
        ///     if (tokenRecord == null || tokenRecord.IsRevoked)
        ///         throw new SecurityException("Invalid refresh token");
        ///     
        ///     // Revoke old refresh token
        ///     tokenRecord.IsRevoked = true;
        ///     tokenRecord.RevokedAt = DateTime.UtcNow;
        ///     
        ///     // Generate new tokens
        ///     var newAccessToken = GenerateAccessToken(tokenRecord.UserId);
        ///     var newRefreshToken = GenerateRefreshToken(tokenRecord.UserId);
        ///     
        ///     // Store new refresh token
        ///     await _db.RefreshTokens.AddAsync(new RefreshToken
        ///     {
        ///         Token = newRefreshToken,
        ///         UserId = tokenRecord.UserId,
        ///         ExpiresAt = DateTime.UtcNow.AddDays(30),
        ///         CreatedAt = DateTime.UtcNow
        ///     });
        ///     
        ///     await _db.SaveChangesAsync();
        ///     
        ///     return new TokenResponse
        ///     {
        ///         AccessToken = newAccessToken,
        ///         RefreshToken = newRefreshToken
        ///     };
        /// }
        /// ```
        /// 
        /// **Storage:**
        /// - Server: Database with usage tracking
        /// - Client: Secure HTTP-only cookie (web)
        /// - Client: Secure keychain/keystore (mobile)
        /// - Never in localStorage (XSS vulnerable)
        /// 
        /// **Validation:**
        /// - Check token exists in database
        /// - Verify not revoked
        /// - Check not expired
        /// - Validate user still exists/active
        /// - Log all refresh attempts
        /// 
        /// **Revocation:**
        /// - Delete from database on logout
        /// - Revoke all user tokens on password change
        /// - Revoke on suspicious activity
        /// - Implement family tree tracking
        /// 
        /// **Database Schema:**
        /// ```sql
        /// CREATE TABLE RefreshTokens (
        ///     Id UNIQUEIDENTIFIER PRIMARY KEY,
        ///     UserId UNIQUEIDENTIFIER NOT NULL,
        ///     Token NVARCHAR(500) NOT NULL UNIQUE,
        ///     JwtId NVARCHAR(100) NOT NULL,
        ///     IsRevoked BIT NOT NULL DEFAULT 0,
        ///     ReplacedByToken NVARCHAR(500) NULL,
        ///     RevokedAt DATETIME2 NULL,
        ///     CreatedAt DATETIME2 NOT NULL,
        ///     ExpiresAt DATETIME2 NOT NULL,
        ///     CreatedByIp NVARCHAR(50) NULL,
        ///     RevokedByIp NVARCHAR(50) NULL,
        ///     
        ///     INDEX IX_RefreshTokens_UserId (UserId),
        ///     INDEX IX_RefreshTokens_Token (Token),
        ///     INDEX IX_RefreshTokens_ExpiresAt (ExpiresAt)
        /// );
        /// ```
        /// 
        /// **Monitoring:**
        /// - Track refresh frequency per user
        /// - Alert on unusual patterns
        /// - Monitor token reuse attempts
        /// - Log IP addresses and user agents
        /// 
        /// **Cleanup:**
        /// - Background job to delete expired tokens
        /// - Run daily or weekly
        /// - Keep for audit period first
        /// ```csharp
        /// public async Task CleanupExpiredTokensAsync()
        /// {
        ///     var cutoffDate = DateTime.UtcNow.AddDays(-90);
        ///     await _db.RefreshTokens
        ///         .Where(t => t.ExpiresAt < cutoffDate)
        ///         .ExecuteDeleteAsync();
        /// }
        /// ```
        /// </remarks>
        RefreshToken = 2
    }
}
