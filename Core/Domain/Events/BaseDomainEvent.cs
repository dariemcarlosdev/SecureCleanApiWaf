namespace SecureCleanApiWaf.Core.Domain.Events;

/// <summary>
/// Base class for all domain events providing common event properties.
/// Implements <see cref="IDomainEvent"/> with default behavior for event tracking.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <list type="bullet">
///   <item>Provide consistent event identification and timestamping</item>
///   <item>Reduce boilerplate in concrete event implementations</item>
///   <item>Ensure all events have correlation and audit capabilities</item>
///   <item>Support event sourcing and event-driven architecture patterns</item>
/// </list>
/// 
/// <para><strong>Key Features:</strong></para>
/// <list type="bullet">
///   <item><strong>Auto-Generated EventId:</strong> Unique GUID for each event instance</item>
///   <item><strong>UTC Timestamp:</strong> Precise moment when event occurred</item>
///   <item><strong>Immutability:</strong> Properties are init-only to prevent modification</item>
///   <item><strong>Inheritance Ready:</strong> Designed for easy extension by concrete events</item>
/// </list>
/// 
/// <para><strong>Design Decisions:</strong></para>
/// <list type="bullet">
///   <item><strong>Abstract vs Interface:</strong> Abstract class chosen to provide default implementations</item>
///   <item><strong>UTC Timestamp:</strong> Always use UTC to avoid timezone confusion</item>
///   <item><strong>Guid EventId:</strong> Provides globally unique identification across systems</item>
///   <item><strong>Protected Constructor:</strong> Ensures EventId and OccurredOn are set at creation</item>
/// </list>
/// 
/// <para><strong>Usage Pattern:</strong></para>
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
/// 
/// // Event is automatically assigned EventId and OccurredOn
/// var @event = new UserRegisteredEvent(userId, "john_doe", email);
/// Console.WriteLine($"Event {event.EventId} occurred at {event.OccurredOn}");
/// </code>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Domain events can implement INotification for in-process event handling:</para>
/// <code>
/// // Option 1: Direct implementation
/// public class UserRegisteredEvent : BaseDomainEvent, INotification
/// {
///     // Event properties...
/// }
/// 
/// // Option 2: Generic base (recommended)
/// public abstract class BaseDomainEvent : IDomainEvent, INotification
/// {
///     // Common properties...
/// }
/// </code>
/// 
/// <para><strong>Event Versioning:</strong></para>
/// <para>For long-lived systems, consider adding version information:</para>
/// <code>
/// public abstract class VersionedDomainEvent : BaseDomainEvent
/// {
///     public int Version { get; init; } = 1;
/// }
/// 
/// public class UserRegisteredEvent_V2 : VersionedDomainEvent
/// {
///     // Updated properties...
/// }
/// </code>
/// 
/// <para><strong>Event Correlation:</strong></para>
/// <para>For complex workflows, consider correlation patterns:</para>
/// <code>
/// public abstract class CorrelatedDomainEvent : BaseDomainEvent
/// {
///     public Guid? CorrelationId { get; init; }
///     public Guid? CausationId { get; init; }
/// }
/// </code>
/// 
/// <para><strong>Serialization Considerations:</strong></para>
/// <list type="bullet">
///   <item>Ensure all event properties are serializable for persistence</item>
///   <item>Use JSON-friendly types (avoid complex objects when possible)</item>
///   <item>Consider using DTOs for events that cross bounded contexts</item>
///   <item>Document breaking changes in event schema</item>
/// </list>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// <list type="bullet">
///   <item>Keep events lightweight - avoid large payloads</item>
///   <item>Consider event payload size for message bus limitations</item>
///   <item>Use lazy initialization for computed properties</item>
///   <item>Avoid loading entities just to create events - use IDs instead</item>
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
/// <strong>Example 2: Event with Rich Context</strong>
/// <code>
/// public class UserAccountLockedEvent : BaseDomainEvent
/// {
///     public Guid UserId { get; }
///     public string Username { get; }
///     public string Reason { get; }
///     public DateTime? LockedUntil { get; }
///     public int FailedLoginAttempts { get; }
///     public string? IpAddress { get; }
///     
///     public UserAccountLockedEvent(
///         Guid userId,
///         string username,
///         string reason,
///         DateTime? lockedUntil,
///         int failedLoginAttempts,
///         string? ipAddress)
///     {
///         UserId = userId;
///         Username = username;
///         Reason = reason;
///         LockedUntil = lockedUntil;
///         FailedLoginAttempts = failedLoginAttempts;
///         IpAddress = ipAddress;
///     }
/// }
/// </code>
/// 
/// <strong>Example 3: Event Handler</strong>
/// <code>
/// public class TokenRevokedEventHandler : INotificationHandler&lt;TokenRevokedEvent&gt;
/// {
///     private readonly ILogger&lt;TokenRevokedEventHandler&gt; _logger;
///     private readonly ITokenBlacklistService _blacklistService;
///     
///     public async Task Handle(TokenRevokedEvent notification, CancellationToken ct)
///     {
///         _logger.LogInformation(
///             "Token {TokenId} revoked for user {UserId} at {OccurredOn}. Reason: {Reason}",
///             notification.TokenId,
///             notification.UserId,
///             notification.OccurredOn,
///             notification.Reason);
///             
///         // Additional processing...
///         await _blacklistService.AddToBlacklistAsync(
///             notification.TokenId.ToString(),
///             ct);
///     }
/// }
/// </code>
/// </example>
public abstract class BaseDomainEvent : IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// Auto-generated using <see cref="Guid.NewGuid()"/> at event creation.
    /// </summary>
    /// <remarks>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item>Deduplication: Ensure the same event isn't processed twice</item>
    ///   <item>Correlation: Link related events across systems</item>
    ///   <item>Tracking: Monitor event flow through the system</item>
    ///   <item>Debugging: Identify specific event instances in logs</item>
    /// </list>
    /// </remarks>
    public Guid EventId { get; init; }
    
    /// <summary>
    /// UTC timestamp when the event occurred.
    /// Auto-generated using <see cref="DateTime.UtcNow"/> at event creation.
    /// </summary>
    /// <remarks>
    /// <para><strong>Important Notes:</strong></para>
    /// <list type="bullet">
    ///   <item>Always in UTC to avoid timezone-related issues</item>
    ///   <item>Represents when the event was raised, not when it's processed</item>
    ///   <item>Useful for event ordering, time-based queries, and audit trails</item>
    ///   <item>Can be used to calculate event age and implement time-based policies</item>
    /// </list>
    /// 
    /// <para><strong>Precision:</strong></para>
    /// DateTime precision is typically to milliseconds, which is sufficient for most
    /// domain events. For high-frequency events, consider using DateTimeOffset or
    /// adding a sequence number.
    /// </remarks>
    public DateTime OccurredOn { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDomainEvent"/> class.
    /// Automatically sets <see cref="EventId"/> and <see cref="OccurredOn"/>.
    /// </summary>
    /// <remarks>
    /// <para>Protected constructor ensures all derived events have:</para>
    /// <list type="bullet">
    ///   <item>A unique event identifier</item>
    ///   <item>A precise UTC timestamp of occurrence</item>
    /// </list>
    /// 
    /// <para>These values are immutable and cannot be changed after creation,
    /// ensuring event integrity and consistency.</para>
    /// <summary>
    /// Initializes a new BaseDomainEvent with a unique event identifier and a UTC occurrence timestamp.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="EventId"/> to a newly generated GUID and <see cref="OccurredOn"/> to the current UTC time.
    /// These properties are init-only to preserve event immutability and represent the event's creation moment (not processing time).
    /// </remarks>
    protected BaseDomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}