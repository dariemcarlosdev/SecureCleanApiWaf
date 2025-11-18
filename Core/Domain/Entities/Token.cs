using SecureCleanApiWaf.Core.Domain.Enums;
using SecureCleanApiWaf.Core.Domain.Events;
using SecureCleanApiWaf.Core.Domain.Exceptions;

namespace SecureCleanApiWaf.Core.Domain.Entities
{
    /// <summary>
    /// Represents a JWT authentication token in the system.
    /// </summary>
    /// <remarks>
    /// This entity manages the lifecycle of authentication tokens including
    /// creation, validation, expiration, and revocation.
    /// 
    /// Key Responsibilities:
    /// - Token identity and metadata management
    /// - Token lifecycle tracking
    /// - Revocation and blacklisting
    /// - Security audit trail
    /// - Domain event publishing for token lifecycle changes
    /// 
    /// Domain Events:
    /// - TokenRevokedEvent: Raised when a token is explicitly revoked
    /// 
    /// Usage Example:
    /// ```csharp
    /// // Create access token
    /// var token = Token.Create(
    ///     userId: user.Id,
    ///     username: user.Username,
    ///     expiresAt: DateTime.UtcNow.AddMinutes(60),
    ///     type: TokenType.AccessToken,
    ///     clientIp: "192.168.1.1"
    /// );
    /// 
    /// // Check validity
    /// if (token.IsValid())
    /// {
    ///     // Allow API access
    /// }
    /// 
    /// // Revoke on logout (raises TokenRevokedEvent)
    /// token.Revoke("User logout");
    /// 
    /// // Publish domain events (typically in command handler)
    /// foreach (var domainEvent in token.DomainEvents)
    /// {
    ///     await mediator.Publish(domainEvent, cancellationToken);
    /// }
    /// token.ClearDomainEvents();
    /// ```
    /// </remarks>
    // Clear indicator is an internal aggregate root
    public class Token : BaseEntity, IAggregateRoot // Inherits from BaseEntity for audit and soft delete  and implements IAggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Token aggregate root.
        /// Represents a JWT authentication token.
        /// Gets the collection of domain events raised by this entity.
        /// 
        /// This is a aggregate root in DDD terminology that:
        /// - Manages token identity and metadata
        /// - Tracks token lifecycle (issued, expired, revoked)
        /// - Handles revocation and blacklisting
        /// - Enforce business rules around token usage ( e.g. expiration limits)
        /// - Raises domain events for significant lifecycle changes.
        /// 
        /// Invariants:
        /// - TokenId (JTI) is unique per token
        /// - UserId and Username must be valid
        /// - ExpiresAt must be in the future at creation
        /// - Status must be one of Active, Revoked, Expired
        /// - RevokedReason is required if Status is Revoked
        /// - ClientIpAddress and UserAgent are optional metadata
        /// - CreatedAt and UpdatedAt track entity timestamps
        /// - Other invariants as per business rules e.g. max token lifetimes
        /// 
        /// </summary>
        /// <remarks>
        /// Domain events should be published by the application layer after
        /// successful persistence of the entity. Events should then be cleared
        /// using <see cref="ClearDomainEvents"/>.
        /// </remarks>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Gets the token ID (JTI claim in JWT).
        /// </summary>
        /// <remarks>
        /// Unique identifier for the token, used for blacklisting.
        /// Maps to the 'jti' claim in the JWT.
        /// </remarks>
        public string TokenId { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the user ID this token belongs to.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Gets the username (for convenience and audit).
        /// </summary>
        public string Username { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the token type (Access or Refresh).
        /// </summary>
        public TokenType Type { get; private set; }

        /// <summary>
        /// Gets when the token was issued.
        /// </summary>
        public DateTime IssuedAt { get; private set; }

        /// <summary>
        /// Gets when the token expires.
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        /// <summary>
        /// Gets the current token status.
        /// </summary>
        public TokenStatus Status { get; private set; }

        /// <summary>
        /// Gets the reason for revocation (if revoked).
        /// </summary>
        public string? RevokedReason { get; private set; }

        /// <summary>
        /// Gets when the token was revoked (if applicable).
        /// </summary>
        public DateTime? RevokedAt { get; private set; }

        /// <summary>
        /// Gets the client IP address that requested the token.
        /// </summary>
        public string? ClientIpAddress { get; private set; }

        /// <summary>
        /// Gets the user agent string of the client.
        /// </summary>
        public string? UserAgent { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private Token() { }

        /// <summary>
        /// Creates a new authentication token.
        /// </summary>
        /// <param name="tokenId">The JWT ID (JTI) from the generated token.</param>`n        /// <param name="userId">The user ID.</param>
        /// <param name="username">The username.</param>
        /// <param name="expiresAt">When the token expires.</param>
        /// <param name="type">Token type (Access or Refresh).</param>
        /// <param name="clientIp">Optional client IP address.</param>
        /// <param name="userAgent">Optional user agent string.</param>
        /// <returns>A new Token entity.</returns>
        /// <exception cref="DomainException">Thrown when validation fails.</exception>
        public static Token Create(string tokenId, Guid userId,
            string username,
            DateTime expiresAt,
            TokenType type,
            string? clientIp = null,
            string? userAgent = null)
        {
            // Validation: Token ID
            if (string.IsNullOrWhiteSpace(tokenId))
                throw new DomainException("Token ID cannot be empty");

            // Validation: User ID
            if (userId == Guid.Empty)
                throw new DomainException("User ID cannot be empty");

            // Validation: Username
            if (string.IsNullOrWhiteSpace(username))
                throw new DomainException("Username cannot be empty");

            // Validation: Expiration
            if (expiresAt <= DateTime.UtcNow)
                throw new DomainException("Token expiration must be in the future");

            // Business Rule: Access tokens should be short-lived
            if (type == TokenType.AccessToken)
            {
                var maxAccessTokenLifetime = TimeSpan.FromHours(2);
                if (expiresAt - DateTime.UtcNow > maxAccessTokenLifetime)
                {
                    throw new DomainException(
                        $"Access tokens cannot have lifetime longer than {maxAccessTokenLifetime.TotalHours} hours");
                }
            }

            // Business Rule: Refresh tokens should have reasonable lifetime
            if (type == TokenType.RefreshToken)
            {
                var maxRefreshTokenLifetime = TimeSpan.FromDays(90);
                if (expiresAt - DateTime.UtcNow > maxRefreshTokenLifetime)
                {
                    throw new DomainException(
                        $"Refresh tokens cannot have lifetime longer than {maxRefreshTokenLifetime.TotalDays} days");
                }
            }

            return new Token
            {
                Id = Guid.NewGuid(),
                TokenId = tokenId, // Use the provided JTI from JWT
                UserId = userId,
                Username = username,
                Type = type,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Status = TokenStatus.Active,
                ClientIpAddress = clientIp,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Revokes the token (makes it invalid).
        /// </summary>
        /// <param name="reason">Reason for revocation (required for audit).</param>
        /// <exception cref="DomainException">Thrown when revocation violates business rules.</exception>
        /// <remarks>
        /// Raises <see cref="TokenRevokedEvent"/> when the token is successfully revoked.
        /// This event should be published by the application layer for:
        /// - Updating distributed token blacklists
        /// - Creating security audit logs
        /// - Notifying users of unexpected revocations
        /// - Analytics and security monitoring
        /// </remarks>
        public void Revoke(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Revocation reason is required");

            if (Status == TokenStatus.Revoked)
                throw new InvalidDomainOperationException(
                    "Revoke token",
                    "Token is already revoked");

            if (IsExpired())
            {
                throw new InvalidDomainOperationException(
                    "Revoke token",
                    "Cannot revoke expired token (already invalid)");
            }

            Status = TokenStatus.Revoked;
            RevokedReason = reason;
            RevokedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            // Raise domain event
            _domainEvents.Add(new TokenRevokedEvent(
                tokenId: Guid.Parse(TokenId),
                userId: UserId,
                username: Username,
                tokenType: Type,
                expiresAt: ExpiresAt,
                reason: reason));
        }

        /// <summary>
        /// Marks the token as expired (automatic process).
        /// </summary>
        /// <remarks>
        /// Called by background jobs or when checking token validity.
        /// Tokens automatically expire based on ExpiresAt timestamp.
        /// </remarks>
        public void MarkAsExpired()
        {
            if (Status == TokenStatus.Expired)
                return; // Already expired

            if (Status == TokenStatus.Revoked)
                return; // Keep revoked status

            if (ExpiresAt > DateTime.UtcNow)
            {
                throw new InvalidDomainOperationException(
                    "Mark token as expired",
                    "Token has not reached expiration time yet");
            }

            Status = TokenStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the token is currently valid for authentication.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        /// <remarks>
        /// A token is valid if:
        /// - Status is Active
        /// - Not expired
        /// - Not revoked
        /// - Not soft-deleted
        /// </remarks>
        public bool IsValid()
        {
            return Status == TokenStatus.Active
                && !IsExpired()
                && !IsDeleted;
        }

        /// <summary>
        /// Checks if the token has expired.
        /// </summary>
        /// <returns>True if expired, false otherwise.</returns>
        public bool IsExpired()
        {
            return ExpiresAt <= DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the token is revoked.
        /// </summary>
        /// <returns>True if revoked, false otherwise.</returns>
        public bool IsRevoked()
        {
            return Status == TokenStatus.Revoked;
        }

        /// <summary>
        /// Gets the remaining lifetime of the token.
        /// </summary>
        /// <returns>TimeSpan of remaining lifetime, or zero if expired.</returns>
        /// <remarks>
        /// Useful for determining if token refresh is needed.
        /// 
        /// Example:
        /// ```csharp
        /// var remaining = token.GetRemainingLifetime();
        /// if (remaining < TimeSpan.FromMinutes(5))
        /// {
        ///     // Refresh token soon
        /// }
        /// ```
        /// </remarks>
        public TimeSpan GetRemainingLifetime()
        {
            if (IsExpired())
                return TimeSpan.Zero;

            return ExpiresAt - DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the token can be refreshed.
        /// </summary>
        /// <returns>True if refresh is allowed, false otherwise.</returns>
        /// <remarks>
        /// Refresh is allowed if:
        /// - Token type is RefreshToken
        /// - Status is Active
        /// - Not expired
        /// - Not revoked
        /// 
        /// Access tokens cannot be refreshed directly.
        /// </remarks>
        public bool CanBeRefreshed()
        {
            return Type == TokenType.RefreshToken
                && Status == TokenStatus.Active
                && !IsExpired()
                && !IsRevoked();
        }

        /// <summary>
        /// Checks if the token is close to expiration.
        /// </summary>
        /// <param name="threshold">Time threshold (default: 5 minutes).</param>
        /// <returns>True if token expires within threshold, false otherwise.</returns>
        /// <remarks>
        /// Useful for proactive token refresh strategies.
        /// 
        /// Example:
        /// ```csharp
        /// if (token.IsExpiringSoon(TimeSpan.FromMinutes(10)))
        /// {
        ///     var newToken = await RefreshTokenAsync(token);
        /// }
        /// ```
        /// </remarks>
        public bool IsExpiringSoon(TimeSpan? threshold = null)
        {
            var checkThreshold = threshold ?? TimeSpan.FromMinutes(5);
            var remaining = GetRemainingLifetime();
            
            return remaining > TimeSpan.Zero && remaining <= checkThreshold;
        }

        /// <summary>
        /// Gets the token age (time since issued).
        /// </summary>
        /// <returns>TimeSpan representing token age.</returns>
        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - IssuedAt;
        }

        /// <summary>
        /// Checks if the token belongs to a specific user.
        /// </summary>
        /// <param name="userId">The user ID to check.</param>
        /// <returns>True if token belongs to user, false otherwise.</returns>
        public bool BelongsToUser(Guid userId)
        {
            return UserId == userId;
        }

        /// <summary>
        /// Checks if the token was issued from a specific IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if matches, false otherwise.</returns>
        /// <remarks>
        /// Useful for detecting token theft or suspicious activity.
        /// 
        /// Example:
        /// ```csharp
        /// var currentIp = httpContext.Connection.RemoteIpAddress?.ToString();
        /// if (!token.IsFromIpAddress(currentIp))
        /// {
        ///     // Suspicious activity - token might be stolen
        ///     _logger.LogWarning("Token used from different IP");
        /// }
        /// ```
        /// </remarks>
        public bool IsFromIpAddress(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ClientIpAddress))
                return true; // No IP restriction

            return ClientIpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a summary of the token for logging/debugging.
        /// </summary>
        /// <returns>Token summary string.</returns>
        /// <remarks>
        /// Never includes the actual JWT token value.
        /// Safe for logging and debugging purposes.
        /// </remarks>
        public string GetSummary()
        {
            return $"TokenID: {TokenId}, " +
                   $"User: {Username}, " +
                   $"Type: {Type}, " +
                   $"Status: {Status}, " +
                   $"Issued: {IssuedAt:yyyy-MM-dd HH:mm:ss}, " +
                   $"Expires: {ExpiresAt:yyyy-MM-dd HH:mm:ss}, " +
                   $"Valid: {IsValid()}";
        }

        /// <summary>
        /// Clears all domain events from this entity.
        /// </summary>
        /// <remarks>
        /// Should be called after domain events have been successfully published
        /// to prevent duplicate event publishing.
        /// 
        /// Typical usage in command handlers:
        /// <code>
        /// await _repository.UpdateAsync(token, cancellationToken);
        /// 
        /// foreach (var domainEvent in token.DomainEvents)
        /// {
        ///     await _mediator.Publish(domainEvent, cancellationToken);
        /// }
        /// 
        /// token.ClearDomainEvents();
        /// </code>
        /// </remarks>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
