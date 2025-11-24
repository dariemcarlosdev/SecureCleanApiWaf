using CleanArchitecture.ApiTemplate.Core.Domain.Enums;

namespace CleanArchitecture.ApiTemplate.Core.Domain.Events;

/// <summary>
/// Domain event raised when a JWT token is revoked.
/// This event signifies that a token has been explicitly invalidated before its natural expiration.
/// </summary>
/// <remarks>
/// <para><strong>Business Context:</strong></para>
/// <para>
/// Token revocation is a critical security operation that occurs when:
/// <list type="bullet">
///   <item>User logs out (invalidating their access token)</item>
///   <item>Security breach detected (force logout all sessions)</item>
///   <item>Administrative action (disable user account, change permissions)</item>
///   <item>Password or credentials changed (invalidate existing sessions)</item>
///   <item>Suspicious activity detected (preventive security measure)</item>
///   <item>Token refresh (old refresh token must be revoked)</item>
/// </list>
/// </para>
/// 
/// <para><strong>Event Purpose:</strong></para>
/// <list type="bullet">
///   <item><strong>Security Audit:</strong> Create immutable log of token revocations</item>
///   <item><strong>Real-time Notification:</strong> Inform other services/instances immediately</item>
///   <item><strong>Blacklist Synchronization:</strong> Update distributed token blacklists</item>
///   <item><strong>User Notification:</strong> Alert user if revocation was unexpected</item>
///   <item><strong>Analytics:</strong> Track patterns in token revocation (security insights)</item>
/// </list>
/// 
/// <para><strong>Event Handlers:</strong></para>
/// <para>This event can trigger multiple handlers:</para>
/// <code>
/// // Handler 1: Update distributed blacklist
/// public class UpdateBlacklistOnTokenRevokedHandler : INotificationHandler&lt;TokenRevokedEvent&gt;
/// {
///     private readonly ITokenBlacklistService _blacklistService;
///     
///     public async Task Handle(TokenRevokedEvent evt, CancellationToken ct)
///     {
///         await _blacklistService.AddToBlacklistAsync(
///             evt.TokenId.ToString(),
///             evt.ExpiresAt,
///             ct);
///     }
/// }
/// 
/// // Handler 2: Create audit log entry
/// public class AuditTokenRevocationHandler : INotificationHandler&lt;TokenRevokedEvent&gt;
/// {
///     private readonly IAuditLogService _auditLog;
///     
///     public async Task Handle(TokenRevokedEvent evt, CancellationToken ct)
///     {
///         await _auditLog.LogSecurityEventAsync(
///             "TokenRevoked",
///             evt.UserId,
///             new { evt.TokenId, evt.Reason, evt.TokenType },
///             ct);
///     }
/// }
/// 
/// // Handler 3: Send security notification
/// public class NotifyUserOnTokenRevokedHandler : INotificationHandler&lt;TokenRevokedEvent&gt;
/// {
///     private readonly INotificationService _notificationService;
///     
///     public async Task Handle(TokenRevokedEvent evt, CancellationToken ct)
///     {
///         // Only notify for suspicious revocations
///         if (evt.Reason.Contains("suspicious", StringComparison.OrdinalIgnoreCase))
///         {
///             await _notificationService.SendSecurityAlertAsync(
///                 evt.UserId,
///                 "Your session was terminated due to suspicious activity",
///                 ct);
///         }
///     }
/// }
/// </code>
/// 
/// <para><strong>Integration Patterns:</strong></para>
/// <list type="bullet">
///   <item><strong>In-Process:</strong> MediatR INotification for same-process handlers</item>
///   <item><strong>Distributed:</strong> Publish to message bus (Redis Pub/Sub, RabbitMQ, Azure Service Bus)</item>
///   <item><strong>Event Store:</strong> Persist for audit trail and event sourcing</item>
///   <item><strong>Real-time:</strong> SignalR notification to connected clients</item>
/// </list>
/// 
/// <para><strong>Distributed Systems Consideration:</strong></para>
/// <para>
/// In multi-server environments, token revocation must be propagated to all instances:
/// <list type="bullet">
///   <item>Publish event to shared message bus (Redis, RabbitMQ)</item>
///   <item>All instances subscribe and update local blacklist caches</item>
///   <item>Ensure idempotent handling (same event processed multiple times is safe)</item>
///   <item>Consider eventual consistency timing (small window where token still valid)</item>
/// </list>
/// </para>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// <list type="bullet">
///   <item>Event is lightweight - only IDs and metadata (no entity loading)</item>
///   <item>Handlers should be async and non-blocking</item>
///   <item>Critical path: blacklist update; non-critical: notifications, analytics</item>
///   <item>Consider priority queues for time-sensitive handlers</item>
/// </list>
/// 
/// <para><strong>Security Implications:</strong></para>
/// <list type="bullet">
///   <item>Event contains sensitive operation data - log carefully</item>
///   <item>Ensure event handlers have appropriate permissions</item>
///   <item>Rate-limit event publishing to prevent DoS via event storm</item>
///   <item>Validate event authenticity in distributed scenarios</item>
/// </list>
/// 
/// <para><strong>Testing Strategy:</strong></para>
/// <code>
/// [Fact]
/// public void TokenRevokedEvent_Should_Include_All_Required_Data()
/// {
///     // Arrange
///     var tokenId = Guid.NewGuid();
///     var userId = Guid.NewGuid();
///     var username = "john_doe";
///     var tokenType = TokenType.AccessToken;
///     var expiresAt = DateTime.UtcNow.AddHours(1);
///     var reason = "User logout";
///     
///     // Act
///     var @event = new TokenRevokedEvent(
///         tokenId, userId, username, tokenType, expiresAt, reason);
///     
///     // Assert
///     Assert.Equal(tokenId, @event.TokenId);
///     Assert.Equal(userId, @event.UserId);
///     Assert.Equal(username, @event.Username);
///     Assert.Equal(tokenType, @event.TokenType);
///     Assert.Equal(expiresAt, @event.ExpiresAt);
///     Assert.Equal(reason, @event.Reason);
///     Assert.NotEqual(Guid.Empty, @event.EventId);
///     Assert.True(@event.OccurredOn &lt;= DateTime.UtcNow);
/// }
/// </code>
/// </remarks>
/// <example>
/// <strong>Example 1: Raising Event on User Logout</strong>
/// <code>
/// public class Token : BaseEntity
/// {
///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
///     
///     public void Revoke(string reason)
///     {
///         if (TokenStatus == TokenStatus.Revoked)
///             throw new InvalidDomainOperationException("Token is already revoked");
///         
///         TokenStatus = TokenStatus.Revoked;
///         RevokedAt = DateTime.UtcNow;
///         RevocationReason = reason;
///         
///         // Raise domain event
///         _domainEvents.Add(new TokenRevokedEvent(
///             TokenId,
///             UserId,
///             Username,
///             TokenType,
///             ExpiresAt,
///             reason));
///     }
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
/// }
/// </code>
/// 
/// <strong>Example 2: Command Handler with Event Publishing</strong>
/// <code>
/// public class RevokeTokenCommandHandler : IRequestHandler&lt;RevokeTokenCommand, Result&gt;
/// {
///     private readonly ITokenRepository _tokenRepository;
///     private readonly IMediator _mediator;
///     
///     public async Task&lt;Result&gt; Handle(RevokeTokenCommand request, CancellationToken ct)
///     {
///         // Retrieve token
///         var token = await _tokenRepository.GetByIdAsync(request.TokenId, ct);
///         if (token is null)
///             return Result.Failure("Token not found");
///         
///         // Revoke token (this raises the domain event)
///         token.Revoke(request.Reason);
///         
///         // Persist changes
///         await _tokenRepository.UpdateAsync(token, ct);
///         
///         // Publish domain events
///         foreach (var domainEvent in token.DomainEvents)
///         {
///             await _mediator.Publish(domainEvent, ct);
///         }
///         
///         token.ClearDomainEvents();
///         
///         return Result.Success();
///     }
/// }
/// </code>
/// 
/// <strong>Example 3: Distributed Event Publishing (Redis)</strong>
/// <code>
/// public class PublishTokenRevokedToRedisHandler : INotificationHandler&lt;TokenRevokedEvent&gt;
/// {
///     private readonly IConnectionMultiplexer _redis;
///     
///     public async Task Handle(TokenRevokedEvent evt, CancellationToken ct)
///     {
///         var subscriber = _redis.GetSubscriber();
///         var message = JsonSerializer.Serialize(evt);
///         
///         // Publish to Redis channel for other app instances
///         await subscriber.PublishAsync("token:revoked", message);
///     }
/// }
/// 
/// // In Program.cs - Subscribe to Redis events
/// var subscriber = redis.GetSubscriber();
/// await subscriber.SubscribeAsync("token:revoked", async (channel, message) =>
/// {
///     var evt = JsonSerializer.Deserialize&lt;TokenRevokedEvent&gt;(message);
///     // Update local blacklist cache
///     await localBlacklistCache.AddAsync(evt.TokenId.ToString());
/// });
/// </code>
/// </example>
public class TokenRevokedEvent : BaseDomainEvent
{
    /// <summary>
    /// The unique identifier of the revoked token (JTI claim).
    /// This is the primary key used in the token blacklist.
    /// </summary>
    /// <remarks>
    /// This corresponds to the "jti" (JWT ID) claim in the JWT token.
    /// Used to uniquely identify and blacklist specific tokens.
    /// </remarks>
    public Guid TokenId { get; }
    
    /// <summary>
    /// The unique identifier of the user who owned the revoked token.
    /// </summary>
    /// <remarks>
    /// Useful for:
    /// <list type="bullet">
    ///   <item>Audit logs (which user's token was revoked)</item>
    ///   <item>User notifications</item>
    ///   <item>Security analytics (track revocation patterns per user)</item>
    ///   <item>Revoking all user tokens (e.g., on password change)</item>
    /// </list>
    /// </remarks>
    public Guid UserId { get; }
    
    /// <summary>
    /// The username of the user who owned the revoked token.
    /// </summary>
    /// <remarks>
    /// Included for convenience in logging and notifications.
    /// Avoids additional database lookups in event handlers.
    /// Represents the state at time of revocation (usernames can change).
    /// </remarks>
    public string Username { get; }
    
    /// <summary>
    /// The type of token that was revoked (AccessToken or RefreshToken).
    /// </summary>
    /// <remarks>
    /// Different token types may require different handling:
    /// <list type="bullet">
    ///   <item><strong>AccessToken:</strong> Short-lived, blacklist until expiration</item>
    ///   <item><strong>RefreshToken:</strong> Long-lived, critical to blacklist immediately</item>
    /// </list>
    /// </remarks>
    public TokenType TokenType { get; }
    
    /// <summary>
    /// The original expiration time of the token.
    /// </summary>
    /// <remarks>
    /// <para><strong>Blacklist Cleanup Strategy:</strong></para>
    /// Tokens can be safely removed from blacklist after this time (already expired).
    /// This prevents blacklist from growing indefinitely.
    /// 
    /// <para><strong>TTL Calculation:</strong></para>
    /// <code>
    /// var timeToLive = evt.ExpiresAt - DateTime.UtcNow;
    /// if (timeToLive > TimeSpan.Zero)
    /// {
    ///     await cache.SetAsync(evt.TokenId.ToString(), "revoked", timeToLive);
    /// }
    /// </code>
    /// </remarks>
    public DateTime ExpiresAt { get; }
    
    /// <summary>
    /// Human-readable reason for token revocation.
    /// </summary>
    /// <remarks>
    /// <para><strong>Common Reasons:</strong></para>
    /// <list type="bullet">
    ///   <item>"User logout" - Normal user-initiated logout</item>
    ///   <item>"Password changed" - Security measure after credential change</item>
    ///   <item>"Account suspended" - Administrative action</item>
    ///   <item>"Suspicious activity detected" - Security response</item>
    ///   <item>"Token refresh" - Old refresh token invalidated</item>
    ///   <item>"Admin revocation" - Manual admin intervention</item>
    /// </list>
    /// 
    /// <para>This reason should:</para>
    /// <list type="bullet">
    ///   <item>Be logged for audit purposes</item>
    ///   <item>Help security teams identify patterns</item>
    ///   <item>Drive conditional event handling (e.g., notify on suspicious activity)</item>
    ///   <item>Provide context for compliance and auditing</item>
    /// </list>
    /// </remarks>
    public string Reason { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevokedEvent"/> class.
    /// </summary>
    /// <param name="tokenId">The unique identifier of the revoked token (JTI).</param>
    /// <param name="userId">The unique identifier of the user who owned the token.</param>
    /// <param name="username">The username of the user (at time of revocation).</param>
    /// <param name="tokenType">The type of token (AccessToken or RefreshToken).</param>
    /// <param name="expiresAt">The original expiration time of the token.</param>
    /// <param name="reason">Human-readable reason for revocation.</param>
    /// <remarks>
    /// <para><strong>Immutability:</strong></para>
    /// All properties are set via constructor and are immutable (readonly).
    /// This ensures event integrity - events represent immutable historical facts.
    /// 
    /// <para><strong>Automatic Properties:</strong></para>
    /// <list type="bullet">
    ///   <item><see cref="BaseDomainEvent.EventId"/> - Auto-generated unique identifier</item>
    ///   <item><see cref="BaseDomainEvent.OccurredOn"/> - Auto-set to current UTC time</item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="username"/> or <paramref name="reason"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="tokenId"/> or <paramref name="userId"/> is empty GUID.
    /// <summary>
    /// Creates a TokenRevokedEvent capturing details about a JWT token that was explicitly revoked before its expiration.
    /// </summary>
    /// <param name="tokenId">The token's unique identifier (JTI) used as the blacklist key.</param>
    /// <param name="userId">The unique identifier of the user who owned the token.</param>
    /// <param name="username">The username of the user at the time of revocation.</param>
    /// <param name="tokenType">The type of token that was revoked (e.g., AccessToken or RefreshToken).</param>
    /// <param name="expiresAt">The original expiration time of the token.</param>
    /// <param name="reason">A human-readable reason for the revocation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tokenId"/> or <paramref name="userId"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="username"/> or <paramref name="reason"/> is null, empty, or whitespace.</exception>
    public TokenRevokedEvent(
        Guid tokenId,
        Guid userId,
        string username,
        TokenType tokenType,
        DateTime expiresAt,
        string reason)
    {
        if (tokenId == Guid.Empty)
            throw new ArgumentException("Token ID cannot be empty", nameof(tokenId));
        
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username), "Username cannot be null or empty");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentNullException(nameof(reason), "Revocation reason cannot be null or empty");
        
        TokenId = tokenId;
        UserId = userId;
        Username = username;
        TokenType = tokenType;
        ExpiresAt = expiresAt;
        Reason = reason;
    }
}