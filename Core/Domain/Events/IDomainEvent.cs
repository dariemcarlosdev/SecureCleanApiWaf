namespace SecureCleanApiWaf.Core.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something important that happened in the domain.
/// They are used to decouple domain logic and enable event-driven architectures.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <list type="bullet">
///   <item>Capture domain-significant occurrences (e.g., user registered, token revoked)</item>
///   <item>Enable loose coupling between domain entities and business logic</item>
///   <item>Support event sourcing and audit trail patterns</item>
///   <item>Facilitate asynchronous processing and integration with external systems</item>
/// </list>
/// 
/// <para><strong>Key Characteristics:</strong></para>
/// <list type="bullet">
///   <item><strong>Immutable:</strong> Events represent past facts that cannot change</item>
///   <item><strong>Named in Past Tense:</strong> e.g., "UserRegistered", "TokenRevoked"</item>
///   <item><strong>Rich in Context:</strong> Include all relevant data needed by handlers</item>
///   <item><strong>Domain-Focused:</strong> Reflect business domain language, not technical details</item>
/// </list>
/// 
/// <para><strong>Implementation Pattern:</strong></para>
/// <code>
/// public class UserRegisteredEvent : BaseDomainEvent
/// {
///     public Guid UserId { get; }
///     public string Username { get; }
///     public Email Email { get; }
///     
///     public UserRegisteredEvent(Guid userId, string username, Email email)
///     {
///         UserId = userId;
///         Username = username;
///         Email = email;
///     }
/// }
/// </code>
/// 
/// <para><strong>Event Raising Pattern:</strong></para>
/// <code>
/// public class User : BaseEntity
/// {
///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
///     
///     public static User Create(string username, Email email, string passwordHash)
///     {
///         var user = new User(username, email, passwordHash);
///         user._domainEvents.Add(new UserRegisteredEvent(user.Id, username, email));
///         return user;
///     }
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
/// }
/// </code>
/// 
/// <para><strong>Event Handling Pattern (MediatR):</strong></para>
/// <code>
/// public class UserRegisteredEventHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     public async Task Handle(UserRegisteredEvent notification, CancellationToken ct)
///     {
///         // Send welcome email
///         // Create audit log entry
///         // Notify external systems
///     }
/// }
/// </code>
/// 
/// <para><strong>Best Practices:</strong></para>
/// <list type="bullet">
///   <item>Keep events small and focused on a single occurrence</item>
///   <item>Include only data relevant to the event (avoid entity references)</item>
///   <item>Make all properties immutable (init-only or readonly)</item>
///   <item>Use value objects where appropriate (e.g., Email, not string)</item>
///   <item>Consider versioning strategy for long-lived events</item>
///   <item>Document the business context and why the event matters</item>
/// </list>
/// 
/// <para><strong>Integration with Infrastructure:</strong></para>
/// <list type="bullet">
///   <item><strong>MediatR:</strong> Domain events can implement INotification for in-process handling</item>
///   <item><strong>Outbox Pattern:</strong> Persist events before publishing to ensure reliability</item>
///   <item><strong>Message Bus:</strong> Publish events to external systems (e.g., RabbitMQ, Azure Service Bus)</item>
///   <item><strong>Event Store:</strong> Persist all events for audit trail and event sourcing</item>
/// </list>
/// 
/// <para><strong>Common Event Types:</strong></para>
/// <list type="bullet">
///   <item><strong>Lifecycle Events:</strong> UserRegistered, UserDeleted, AccountActivated</item>
///   <item><strong>Security Events:</strong> TokenRevoked, PasswordChanged, LoginFailed</item>
///   <item><strong>Data Events:</strong> ApiDataSynced, CacheRefreshed, DataStale</item>
///   <item><strong>Business Events:</strong> OrderPlaced, PaymentProcessed, ItemShipped</item>
/// </list>
/// </remarks>
/// <example>
/// <strong>Example 1: Simple Event</strong>
/// <code>
/// public class TokenRevokedEvent : BaseDomainEvent
/// {
///     public Guid TokenId { get; }
///     public Guid UserId { get; }
///     public string Reason { get; }
///     
///     public TokenRevokedEvent(Guid tokenId, Guid userId, string reason)
///     {
///         TokenId = tokenId;
///         UserId = userId;
///         Reason = reason;
///     }
/// }
/// </code>
/// 
/// <strong>Example 2: Event with Value Objects</strong>
/// <code>
/// public class UserEmailChangedEvent : BaseDomainEvent
/// {
///     public Guid UserId { get; }
///     public Email OldEmail { get; }
///     public Email NewEmail { get; }
///     
///     public UserEmailChangedEvent(Guid userId, Email oldEmail, Email newEmail)
///     {
///         UserId = userId;
///         OldEmail = oldEmail;
///         NewEmail = newEmail;
///     }
/// }
/// </code>
/// </example>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// Useful for deduplication, tracking, and correlation.
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// UTC timestamp when the event occurred.
    /// Represents the moment the domain event was raised.
    /// </summary>
    DateTime OccurredOn { get; }
}
