using CleanArchitecture.ApiTemplate.Core.Domain.ValueObjects;

namespace CleanArchitecture.ApiTemplate.Core.Domain.Events;

/// <summary>
/// Domain event raised when a new user successfully registers in the system.
/// This event signifies the creation of a new user account and the start of the user lifecycle.
/// </summary>
/// <remarks>
/// <para><strong>Business Context:</strong></para>
/// <para>
/// User registration is a critical business event that triggers multiple downstream processes:
/// <list type="bullet">
///   <item>Welcome email and onboarding sequence</item>
///   <item>Initial user preferences and settings creation</item>
///   <item>Analytics tracking (user acquisition metrics)</item>
///   <item>Audit log entry for compliance</item>
///   <item>CRM integration (customer record creation)</item>
///   <item>Marketing automation (welcome campaign, segmentation)</item>
///   <item>Notification to admin team (new user alerts)</item>
/// </list>
/// </para>
/// 
/// <para><strong>Event Purpose:</strong></para>
/// <list type="bullet">
///   <item><strong>Decoupling:</strong> Separate registration logic from post-registration actions</item>
///   <item><strong>Auditability:</strong> Permanent record of when and how user was created</item>
///   <item><strong>Integration:</strong> Notify external systems of new user</item>
///   <item><strong>Analytics:</strong> Track user acquisition and conversion funnels</item>
///   <item><strong>Extensibility:</strong> Easy to add new post-registration behaviors</item>
/// </list>
/// 
/// <para><strong>Event Handlers:</strong></para>
/// <para>This event typically triggers multiple handlers:</para>
/// <code>
/// // Handler 1: Send welcome email
/// public class SendWelcomeEmailHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly IEmailService _emailService;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         await _emailService.SendWelcomeEmailAsync(
///             evt.Email.Value,
///             evt.Username,
///             ct);
///     }
/// }
/// 
/// // Handler 2: Create audit log
/// public class AuditUserRegistrationHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly IAuditLogService _auditLog;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         await _auditLog.LogAsync(
///             "UserRegistered",
///             evt.UserId,
///             new { evt.Username, evt.Email, evt.IpAddress },
///             ct);
///     }
/// }
/// 
/// // Handler 3: Initialize user preferences
/// public class InitializeUserPreferencesHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly IUserPreferencesService _preferencesService;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         await _preferencesService.CreateDefaultPreferencesAsync(
///             evt.UserId,
///             ct);
///     }
/// }
/// 
/// // Handler 4: Track analytics
/// public class TrackUserRegistrationHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly IAnalyticsService _analytics;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         await _analytics.TrackEventAsync(
///             "user_registered",
///             new { 
///                 userId = evt.UserId,
///                 registrationMethod = evt.RegistrationMethod,
///                 timestamp = evt.OccurredOn
///             },
///             ct);
///     }
/// }
/// 
/// // Handler 5: Integrate with CRM
/// public class SyncUserToCrmHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly ICrmService _crmService;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         await _crmService.CreateContactAsync(
///             evt.UserId,
///             evt.Username,
///             evt.Email.Value,
///             ct);
///     }
/// }
/// </code>
/// 
/// <para><strong>Integration Patterns:</strong></para>
/// <list type="bullet">
///   <item><strong>In-Process:</strong> MediatR for immediate, transactional handlers</item>
///   <item><strong>Outbox Pattern:</strong> Persist event, then publish to message bus</item>
///   <item><strong>Message Bus:</strong> Publish to external systems (RabbitMQ, Azure Service Bus)</item>
///   <item><strong>Webhooks:</strong> Notify third-party integrations</item>
/// </list>
/// 
/// <para><strong>Transactional Boundaries:</strong></para>
/// <para>
/// Consider which handlers should be part of the registration transaction:
/// <list type="bullet">
///   <item><strong>Transactional:</strong> Critical data consistency (user preferences, initial setup)</item>
///   <item><strong>Eventual:</strong> Non-critical, can retry (emails, analytics, CRM)</item>
/// </list>
/// </para>
/// 
/// <para><strong>Failure Handling:</strong></para>
/// <code>
/// // Resilient email handler with retry policy
/// public class SendWelcomeEmailHandler : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly IEmailService _emailService;
///     private readonly ILogger&lt;SendWelcomeEmailHandler&gt; _logger;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         try
///         {
///             await _emailService.SendWelcomeEmailAsync(
///                 evt.Email.Value,
///                 evt.Username,
///                 ct);
///         }
///         catch (Exception ex)
///         {
///             // Log error but don't fail registration
///             _logger.LogError(ex,
///                 "Failed to send welcome email to user {UserId}. Email: {Email}",
///                 evt.UserId,
///                 evt.Email.ToMaskedString());
///             
///             // Optionally: Queue for retry, send to dead letter queue
///         }
///     }
/// }
/// </code>
/// 
/// <para><strong>Privacy and GDPR Considerations:</strong></para>
/// <list type="bullet">
///   <item>Event contains PII (email, IP address) - handle according to privacy policy</item>
///   <item>Consider data retention policies for event store</item>
///   <item>Implement "right to be forgotten" by anonymizing events</item>
///   <item>Log access to user registration events for compliance</item>
/// </list>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// <list type="bullet">
///   <item>Keep registration handler fast - move slow operations to async handlers</item>
///   <item>Use message queues for non-critical, time-consuming tasks</item>
///   <item>Implement circuit breakers for external service calls</item>
///   <item>Monitor handler execution times and failure rates</item>
/// </list>
/// 
/// <para><strong>Testing Strategy:</strong></para>
/// <code>
/// [Fact]
/// public void UserRegisteredEvent_Should_Include_All_Required_Data()
/// {
///     // Arrange
///     var userId = Guid.NewGuid();
///     var username = "john_doe";
///     var email = Email.Create("john.doe@example.com");
///     var initialRoles = new List&lt;Role&gt; { Role.User };
///     var ipAddress = "192.168.1.1";
///     var userAgent = "Mozilla/5.0";
///     var registrationMethod = "Email";
///     
///     // Act
///     var @event = new UserRegisteredEvent(
///         userId, username, email, initialRoles, ipAddress, userAgent, registrationMethod);
///     
///     // Assert
///     Assert.Equal(userId, @event.UserId);
///     Assert.Equal(username, @event.Username);
///     Assert.Equal(email, @event.Email);
///     Assert.Single(@event.InitialRoles);
///     Assert.Equal(ipAddress, @event.IpAddress);
///     Assert.Equal(userAgent, @event.UserAgent);
///     Assert.Equal(registrationMethod, @event.RegistrationMethod);
///     Assert.NotEqual(Guid.Empty, @event.EventId);
///     Assert.True(@event.OccurredOn &lt;= DateTime.UtcNow);
/// }
/// </code>
/// </remarks>
/// <example>
/// <strong>Example 1: Raising Event in User.Create Method</strong>
/// <code>
/// public class User : BaseEntity
/// {
///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
///     
///     public static User Create(
///         string username,
///         Email email,
///         string passwordHash,
///         string? ipAddress = null,
///         string? userAgent = null)
///     {
///         var user = new User
///         {
///             Id = Guid.NewGuid(),
///             Username = username,
///             Email = email,
///             PasswordHash = passwordHash,
///             UserStatus = UserStatus.Active,
///             CreatedAt = DateTime.UtcNow
///         };
///         
///         // Assign default role
///         user._roles.Add(Role.User);
///         
///         // Raise domain event
///         user._domainEvents.Add(new UserRegisteredEvent(
///             user.Id,
///             username,
///             email,
///             user.Roles.ToList(),
///             ipAddress,
///             userAgent,
///             "Email")); // Registration method
///         
///         return user;
///     }
///     
///     public void ClearDomainEvents() => _domainEvents.Clear();
/// }
/// </code>
/// 
/// <strong>Example 2: Command Handler with Event Publishing</strong>
/// <code>
/// public class RegisterUserCommandHandler : IRequestHandler&lt;RegisterUserCommand, Result&lt;Guid&gt;&gt;
/// {
///     private readonly IUserRepository _userRepository;
///     private readonly IMediator _mediator;
///     private readonly IPasswordHasher _passwordHasher;
///     
///     public async Task&lt;Result&lt;Guid&gt;&gt; Handle(
///         RegisterUserCommand request,
///         CancellationToken ct)
///     {
///         // Validate email uniqueness
///         var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, ct);
///         if (emailExists)
///             return Result&lt;Guid&gt;.Failure("Email already registered");
///         
///         // Hash password
///         var passwordHash = _passwordHasher.HashPassword(request.Password);
///         
///         // Create user (this raises the domain event)
///         var email = Email.Create(request.Email);
///         var user = User.Create(
///             request.Username,
///             email,
///             passwordHash,
///             request.IpAddress,
///             request.UserAgent);
///         
///         // Persist user
///         await _userRepository.AddAsync(user, ct);
///         await _userRepository.SaveChangesAsync(ct);
///         
///         // Publish domain events
///         foreach (var domainEvent in user.DomainEvents)
///         {
///             await _mediator.Publish(domainEvent, ct);
///         }
///         
///         user.ClearDomainEvents();
///         
///         return Result&lt;Guid&gt;.Success(user.Id);
///     }
/// }
/// </code>
/// 
/// <strong>Example 3: Outbox Pattern for Reliable Event Publishing</strong>
/// <code>
/// // Save event to outbox table in same transaction as user
/// public class OutboxEventPublisher : INotificationHandler&lt;UserRegisteredEvent&gt;
/// {
///     private readonly IOutboxRepository _outboxRepository;
///     
///     public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
///     {
///         var outboxMessage = new OutboxMessage
///         {
///             Id = Guid.NewGuid(),
///             EventType = nameof(UserRegisteredEvent),
///             Payload = JsonSerializer.Serialize(evt),
///             CreatedAt = DateTime.UtcNow,
///             ProcessedAt = null
///         };
///         
///         await _outboxRepository.AddAsync(outboxMessage, ct);
///     }
/// }
/// 
/// // Background service processes outbox and publishes to message bus
/// public class OutboxProcessor : BackgroundService
/// {
///     protected override async Task ExecuteAsync(CancellationToken ct)
///     {
///         while (!ct.IsCancellationRequested)
///         {
///             var pendingMessages = await _outboxRepository
///                 .GetUnprocessedMessagesAsync(ct);
///             
///             foreach (var message in pendingMessages)
///             {
///                 // Publish to message bus
///                 await _messageBus.PublishAsync(message.Payload, ct);
///                 
///                 // Mark as processed
///                 message.ProcessedAt = DateTime.UtcNow;
///                 await _outboxRepository.UpdateAsync(message, ct);
///             }
///             
///             await Task.Delay(TimeSpan.FromSeconds(5), ct);
///         }
///     }
/// }
/// </code>
/// </example>
public class UserRegisteredEvent : BaseDomainEvent
{
    /// <summary>
    /// The unique identifier of the newly registered user.
    /// </summary>
    /// <remarks>
    /// This is the primary key for the user entity and should be used
    /// to reference the user in all subsequent operations and integrations.
    /// </remarks>
    public Guid UserId { get; }
    
    /// <summary>
    /// The username chosen by the user during registration.
    /// </summary>
    /// <remarks>
    /// Useful for:
    /// <list type="bullet">
    ///   <item>Personalized welcome messages</item>
    ///   <item>Display in admin notifications</item>
    ///   <item>Audit logs and analytics</item>
    ///   <item>Username-based integrations</item>
    /// </list>
    /// </remarks>
    public string Username { get; }
    
    /// <summary>
    /// The email address of the newly registered user.
    /// Represented as an <see cref="Email"/> value object for validation and consistency.
    /// </summary>
    /// <remarks>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item>Send welcome email and onboarding sequence</item>
    ///   <item>Email verification workflow</item>
    ///   <item>CRM and marketing automation integration</item>
    ///   <item>Communication channel for notifications</item>
    /// </list>
    /// 
    /// <para><strong>Privacy:</strong></para>
    /// Email is PII - handle according to privacy regulations (GDPR, CCPA).
    /// When logging, use masked version: <c>evt.Email.ToMaskedString()</c>
    /// </remarks>
    public Email Email { get; }
    
    /// <summary>
    /// The initial roles assigned to the user upon registration.
    /// </summary>
    /// <remarks>
    /// <para><strong>Typical Scenarios:</strong></para>
    /// <list type="bullet">
    ///   <item>Normal registration: [Role.User]</item>
    ///   <item>Invited admin: [Role.User, Role.Admin]</item>
    ///   <item>Service account: [Role.ServiceAccount]</item>
    /// </list>
    /// 
    /// <para>Useful for:</para>
    /// <list type="bullet">
    ///   <item>Role-based onboarding flows</item>
    ///   <item>Access control setup in external systems</item>
    ///   <item>Analytics segmentation by user type</item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<Role> InitialRoles { get; }
    
    /// <summary>
    /// The IP address from which the user registered (optional).
    /// </summary>
    /// <remarks>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item>Fraud detection and geolocation analysis</item>
    ///   <item>Security audit trail</item>
    ///   <item>Compliance and regulatory requirements</item>
    ///   <item>Geographic user distribution analytics</item>
    /// </list>
    /// 
    /// <para><strong>Privacy:</strong></para>
    /// IP addresses are considered PII in some jurisdictions (e.g., GDPR).
    /// Implement appropriate retention and anonymization policies.
    /// </remarks>
    public string? IpAddress { get; }
    
    /// <summary>
    /// The User-Agent string from the registration request (optional).
    /// </summary>
    /// <remarks>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item>Device and browser analytics</item>
    ///   <item>Platform-specific onboarding (mobile vs web)</item>
    ///   <item>Bot detection and fraud prevention</item>
    ///   <item>Technical support and troubleshooting</item>
    /// </list>
    /// </remarks>
    public string? UserAgent { get; }
    
    /// <summary>
    /// The method or source through which the user registered.
    /// </summary>
    /// <remarks>
    /// <para><strong>Common Values:</strong></para>
    /// <list type="bullet">
    ///   <item>"Email" - Traditional email/password registration</item>
    ///   <item>"Google" - OAuth via Google</item>
    ///   <item>"Microsoft" - OAuth via Microsoft/Azure AD</item>
    ///   <item>"GitHub" - OAuth via GitHub</item>
    ///   <item>"Invitation" - User invited by admin</item>
    ///   <item>"Import" - Bulk user import</item>
    /// </list>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item>Track most effective registration channels</item>
    ///   <item>Customize onboarding based on registration method</item>
    ///   <item>Attribution for marketing campaigns</item>
    ///   <item>Analytics on user acquisition sources</item>
    /// </list>
    /// </remarks>
    public string RegistrationMethod { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRegisteredEvent"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the newly registered user.</param>
    /// <param name="username">The username chosen during registration.</param>
    /// <param name="email">The email address as an <see cref="Email"/> value object.</param>
    /// <param name="initialRoles">The roles assigned to the user upon registration.</param>
    /// <param name="ipAddress">The IP address from which the user registered (optional).</param>
    /// <param name="userAgent">The User-Agent string from the registration request (optional).</param>
    /// <param name="registrationMethod">The registration method/source (e.g., "Email", "Google").</param>
    /// <remarks>
    /// <para><strong>Immutability:</strong></para>
    /// All properties are set via constructor and are immutable.
    /// This ensures the event represents an unchangeable historical fact.
    /// 
    /// <para><strong>Automatic Properties:</strong></para>
    /// <list type="bullet">
    ///   <item><see cref="BaseDomainEvent.EventId"/> - Auto-generated unique identifier</item>
    ///   <item><see cref="BaseDomainEvent.OccurredOn"/> - Auto-set to current UTC time</item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userId"/> is empty GUID.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="username"/>, <paramref name="email"/>,
    /// <paramref name="initialRoles"/>, or <paramref name="registrationMethod"/> is null or empty.
    /// <summary>
    /// Initializes a domain event representing a newly registered user and the details of their registration.
    /// </summary>
    /// <param name="userId">Unique identifier of the newly registered user.</param>
    /// <param name="username">Chosen username for the user.</param>
    /// <param name="email">Email address value object for the user.</param>
    /// <param name="initialRoles">Read-only list of roles assigned at registration; must contain at least one role.</param>
    /// <param name="ipAddress">Optional IP address from which the registration originated.</param>
    /// <param name="userAgent">Optional User-Agent string from the registration request.</param>
    /// <param name="registrationMethod">Method or source of registration (for example, "Email" or "Google").</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="username"/>, <paramref name="email"/>, <paramref name="initialRoles"/>, or <paramref name="registrationMethod"/> is null or invalid (username/registrationMethod empty or whitespace, or <paramref name="initialRoles"/> empty).</exception>
    public UserRegisteredEvent(
        Guid userId,
        string username,
        Email email,
        IReadOnlyList<Role> initialRoles,
        string? ipAddress,
        string? userAgent,
        string registrationMethod)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username), "Username cannot be null or empty");
        
        ArgumentNullException.ThrowIfNull(email);
        
        if (initialRoles is null || initialRoles.Count == 0)
            throw new ArgumentNullException(nameof(initialRoles), "User must have at least one role");
        
        if (string.IsNullOrWhiteSpace(registrationMethod))
            throw new ArgumentNullException(nameof(registrationMethod), "Registration method cannot be null or empty");
        
        UserId = userId;
        Username = username;
        Email = email;
        InitialRoles = initialRoles;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        RegistrationMethod = registrationMethod;
    }
}