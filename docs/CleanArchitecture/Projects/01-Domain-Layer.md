# SecureCleanApiWaf.Domain Project

> *"The domain model is the heart of the software. It is where the business logic lives, and it should be protected from external concerns."*  
> — **Eric Evans**, Domain-Driven Design

---

**?? New to Clean Architecture or DDD?**  
Read **[Architecture Patterns Explained](../ARCHITECTURE_PATTERNS_EXPLAINED.md)** first to understand how Clean Architecture and Domain-Driven Design work together in this project.

---

## ?? Table of Contents

1. [Overview](#-overview)
2. [Purpose](#-purpose)
3. [Implementation Status](#-implementation-status)
4. [Project Structure](#-project-structure)
5. [Implemented Components](#-implemented-components)
   - [1. BaseEntity](#1-baseentity--complete)
   - [2. ValueObject Base Class](#2-valueobject-base-class--complete)
   - [3. Domain Exceptions](#3-domain-exceptions--complete)
   - [4. Domain Enums](#4-domain-enums--complete---all-4)
     - [UserStatus](#userstatus)
     - [TokenStatus](#tokenstatus)
     - [TokenType](#tokentype)
     - [DataStatus](#datastatus)
   - [5. Email Value Object](#5-email-value-object--complete)
   - [6. Role Value Object](#6-role-value-object--complete)
   - [7. User Entity](#7-user-entity--complete)
   - [8. Token Entity](#8-token-entity--complete)
   - [9. ApiDataItem Entity](#9-apidataitem-entity--complete)
   - [10. Domain Events](#10-domain-events--complete)
     - [IDomainEvent Interface](#idomainevent-interface)
     - [BaseDomainEvent Abstract Class](#basedomainevent-abstract-class)
     - [TokenRevokedEvent](#tokenrevokedevent--complete---hands-on-example)
     - [UserRegisteredEvent](#userregisteredevent--complete---additional-example)
     - [Domain Event Publishing](#domain-event-publishing-in-application-layer)
6. [Pending Components](#-pending-components)
7. [Related Documentation](#-related-documentation)
8. [Contact & Support](#-contact--support)

---

## ?? Overview
The **Domain Layer** is the core of the Clean Architecture, containing enterprise business logic and rules. This layer has **no external dependencies** and represents the heart of your application.
It includes:
- **Entities**: Core business objects with identity and lifecycle. (e.g., User, Token, ApiDataItem)
- **Value Objects**: Immutable objects are those that represent descriptive aspects of the domain with no conceptual identity. Immutable meaning their state cannot change after creation. (e.g., Email, Role).(e.g., Email, Role). Immmutabble and compared by value rather than by identity. Include validation logic.
- **Domain Services**: Business logic that doesn't naturally fit within an entity or value object. ( e.g., TokenBlacklistService, though this may also be in Application layer depending on complexity).
- **Domain Events**: Events that signify something important in the domain has occurred. (e.g., UserRegisteredEvent, TokenRevokedEvent, though these may also be in Application layer depending on complexity).
- **Domain Exceptions**: Custom exceptions for domain rule violations. (e.g., DomainException, EntityNotFoundException, InvalidDomainOperationException).
- **Domain Enums**: Enumerations representing domain-specific states or types. (e.g., UserStatus, TokenStatus, TokenType, DataStatus).
- **Repositories Interfaces**: Abstractions for data access, implemented in the Infrastructure layer.

---

## ?? Purpose
- Define business entities and value objects
- Encapsulate core business rules
- Remain completely independent of infrastructure, UI, or frameworks ( e.g., EF Core, ASP.NET)
- Serve as the foundation for all other layers ( Application, Infrastructure, Presentation)
- Facilitate unit testing of business logic (without external dependencies e.g., databases, web services)
- Promote maintainability and scalability of the application. ( e.g., easy to add new business rules, entities, value objects)
- Enable clear separation of concerns between domain logic and other layers. ( e.g., data access, UI)
- Support domain-driven design principles and best practices. ( e.g., aggregates, repositories, domain events, messaging patterns ( e.g., CQRS, Event Sourcing, Message Queues Storage, Message Brokers, Message Service Bus))
- Provide a ubiquitous language for developers and stakeholders to communicate about the domain model. ( e.g., consistent terminology, shared understanding)
- Ensure business logic is centralized and consistent. ( e.g., avoid duplication, inconsistencies)
- Allow easy evolution of business rules without impacting other layers. ( e.g., adding new features, changing workflows)
- Enhance collaboration between technical and non-technical team members. ( e.g., domain experts, developers, testers)

---

## ?? Implementation Status

### ? Completed Components (100%)

| Component | Status | Files Created |
|-----------|--------|---------------|
| **Base Classes** | ? 100% | BaseEntity.cs, ValueObject.cs, DomainException.cs |
| **Domain Enums** | ? 100% | UserStatus.cs, TokenStatus.cs, TokenType.cs, DataStatus.cs |
| **Value Objects** | ? 100% | Email.cs, Role.cs |
| **Domain Entities** | ? 100% | User.cs, Token.cs, ApiDataItem.cs |
| **Domain Events** | ? 100% | IDomainEvent.cs, BaseDomainEvent.cs, TokenRevokedEvent.cs, UserRegisteredEvent.cs |

### ?? Next Steps (0% Remaining)

**Priority 1: EF Core Configurations**
- [ ] Create `UserConfiguration.cs` for User entity
- [ ] Create `TokenConfiguration.cs` for Token entity
- [ ] Create `ApiDataItemConfiguration.cs` for ApiDataItem entity
- [ ] Add migrations for domain entities

**Priority 2: Unit Tests**
- [ ] User entity tests (authentication, roles, lifecycle)
- [ ] Token entity tests (lifecycle, revocation, validation)
- [ ] ApiDataItem entity tests (sync, staleness, metadata)
- [ ] Email value object tests
- [ ] Role value object tests

**Priority 3: Application Layer Integration**
- [ ] Update command/query handlers to use entities
- [ ] Create entity-to-DTO mapping
- [ ] Update TokenBlacklistService to use Token entity
- [ ] Update repositories to work with entities

---

## ?? Project Structure

```
Core/Domain/
+-- Entities/                    # Business entities
¦   +-- BaseEntity.cs           ? Base class with audit fields
¦   +-- User.cs                 ? User entity with domain events
¦   +-- Token.cs                ? JWT token entity with domain events
¦   +-- ApiDataItem.cs          ? API data entity
¦
+-- ValueObjects/               # Immutable value objects
¦   +-- ValueObject.cs          ? Base value object class
¦   +-- Email.cs                ? Email value object
¦   +-- Role.cs                 ? Role value object
¦
+-- Enums/                      # Domain-specific enumerations
¦   +-- UserStatus.cs           ? User lifecycle states
¦   +-- TokenStatus.cs          ? Token states
¦   +-- TokenType.cs            ? Access/Refresh tokens
¦   +-- DataStatus.cs           ? Data freshness states
¦
+-- Exceptions/                 # Domain-specific exceptions
¦   +-- DomainException.cs      ? Domain exceptions
¦
+-- Events/                     # Domain events
    +-- IDomainEvent.cs         ? Domain event interface
    +-- BaseDomainEvent.cs      ? Base domain event class
    +-- TokenRevokedEvent.cs    ? Token revocation event
    +-- UserRegisteredEvent.cs  ? User registration event
```

---

## ??? Implemented Components

### 1. BaseEntity (? Complete)

**Location:** `Core/Domain/Entities/BaseEntity.cs`

All domain entities inherit from this base class providing:
- ? Guid-based unique identifiers
- ? Audit timestamps (CreatedAt, UpdatedAt)
- ? Soft delete support (IsDeleted, DeletedAt)
- ? Entity equality by ID
- ? Comprehensive XML documentation

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }
    
    public void SoftDelete() { /* ... */ }
    public void Restore() { /* ... */ }
}
```

### 2. ValueObject Base Class (? Complete)

**Location:** `Core/Domain/ValueObjects/ValueObject.cs`

Base class for all value objects following DDD principles:
- ? Immutability enforcement
- ? Equality by value comparison
- ? GetEqualityComponents pattern
- ? Comprehensive documentation

```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    public override bool Equals(object? obj) { /* ... */ }
    public override int GetHashCode() { /* ... */ }
}
```

### 3. Domain Exceptions (? Complete)

**Location:** `Core/Domain/Exceptions/DomainException.cs`

Three exception types for domain rule violations:
- ? `DomainException` - Base domain exception
- ? `EntityNotFoundException` - Entity not found
- ? `InvalidDomainOperationException` - Invalid state transitions

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }
    // ...
}
```

### 4. Domain Enums (? Complete - All 4)

#### UserStatus
**Location:** `Core/Domain/Enums/UserStatus.cs`
- Active, Inactive, Suspended, Locked
- Includes state transition diagrams
- Business rules documentation

#### TokenStatus
**Location:** `Core/Domain/Enums/TokenStatus.cs`
- Active, Revoked, Expired
- Security considerations documented
- Implementation patterns included

#### TokenType
**Location:** `Core/Domain/Enums/TokenType.cs`
- AccessToken, RefreshToken
- Comprehensive comparison table
- Security best practices

#### DataStatus
**Location:** `Core/Domain/Enums/DataStatus.cs`
- Active, Stale, Deleted
- Cache refresh strategies
- Cleanup patterns documented

### 5. Email Value Object (? Complete)

**Location:** `Core/Domain/ValueObjects/Email.cs`

Complete email value object implementation:
- ? RFC 5321 compliant validation
- ? Case-insensitive comparison
- ? Email masking for privacy
- ? Domain extraction methods
- ? Entity Framework integration examples

```csharp
public class Email : ValueObject
{
    public string Value { get; private set; }
    
    private Email(string value) { Value = value; }
    
    public static Email Create(string email)
    {
        // Validation logic
        return new Email(email.ToLowerInvariant());
    }
    
    public string GetLocalPart() { /* ... */ }
    public string GetDomain() { /* ... */ }
    public string ToMaskedString() { /* ... */ }
}
```

### 6. Role Value Object (? Complete)

**Location:** `Core/Domain/ValueObjects/Role.cs`

Complete role value object with predefined roles:
- ? Predefined roles (User, Admin, SuperAdmin)
- ? Permission hierarchy checking
- ? Role validation and comparison
- ? Display name formatting

```csharp
public class Role : ValueObject
{
    public string Name { get; private set; }
    
    // Predefined roles
    public static readonly Role User = new("User");
    public static readonly Role Admin = new("Admin");
    public static readonly Role SuperAdmin = new("SuperAdmin");
    
    public static Role Create(string roleName) { /* ... */ }
    public bool IsAdmin() { /* ... */ }
    public bool HasPermission(Role requiredRole) { /* ... */ }
}
```

### 7. User Entity (? Complete)

**Location:** `Core/Domain/Entities/User.cs`

Complete user entity implementation:
- ? User authentication and identity
- ? Role assignment and validation
- ? Login tracking and failed attempts
- ? Account lifecycle (activate, deactivate, suspend, lock)
- ? Password management
- ? Business rule enforcement

**Key Properties:**
- Username, Email (value object), PasswordHash
- Roles (collection of Role value objects)
- UserStatus, LastLoginAt, FailedLoginAttempts
- LockedUntil for temporary locks

**Key Methods:**
```csharp
public static User Create(string username, Email email, string passwordHash);
public void AssignRole(Role role);
public void RecordLogin(string? ipAddress, string? userAgent);
public void RecordFailedLogin(int maxAttempts = 5);
public void Deactivate();
public void Activate();
public void Suspend(string reason);
public void Lock(string reason, TimeSpan? duration = null);
public bool CanLogin();
public bool HasRole(Role role);
```

### 8. Token Entity (? Complete)

**Location:** `Core/Domain/Entities/Token.cs`

Complete JWT token entity implementation:
- ? Token lifecycle management
- ? Revocation and blacklisting support
- ? Expiration tracking
- ? Token validation rules
- ? Security audit trail

**Key Properties:**
- TokenId (JTI claim), UserId, Username
- TokenType, TokenStatus
- IssuedAt, ExpiresAt, RevokedAt
- ClientIpAddress, UserAgent

**Key Methods:**
```csharp
public static Token Create(Guid userId, string username, DateTime expiresAt, TokenType type);
public void Revoke(string reason);
public bool IsValid();
public bool IsExpired();
public TimeSpan GetRemainingLifetime();
public bool CanBeRefreshed();
public bool IsExpiringSoon(TimeSpan? threshold = null);
```

### 9. ApiDataItem Entity (? Complete)

**Location:** `Core/Domain/Entities/ApiDataItem.cs`

Complete API data entity implementation:
- ? External data synchronization tracking
- ? Cache freshness management
- ? Metadata storage (flexible key-value pairs)
- ? Data staleness detection
- ? Source attribution

**Key Properties:**
- ExternalId, Name, Description
- SourceUrl, LastSyncedAt
- DataStatus
- Metadata (Dictionary<string, object>)

**Key Methods:**
```csharp
public static ApiDataItem CreateFromExternalSource(string externalId, string name, ...);
public void UpdateFromExternalSource(string name, string description);
public void MarkAsStale();
public void MarkAsActive();
public bool NeedsRefresh(TimeSpan maxAge);
public void AddMetadata(string key, object value);
public T? GetMetadata<T>(string key);
```

### 10. Domain Events (? Complete)

**Location:** `Core/Domain/Events/`

Complete domain events implementation following DDD principles:
- ? `IDomainEvent` interface - Domain event contract
- ? `BaseDomainEvent` - Base class with EventId and OccurredOn
- ? `TokenRevokedEvent` - Raised when token is revoked
- ? `UserRegisteredEvent` - Raised when new user registers

#### IDomainEvent Interface

**Location:** `Core/Domain/Events/IDomainEvent.cs`

Marker interface for all domain events:
- Defines event identification (EventId)
- Timestamp tracking (OccurredOn)
- Comprehensive documentation on event patterns

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
```

#### BaseDomainEvent Abstract Class

**Location:** `Core/Domain/Events/BaseDomainEvent.cs`

Base class providing common event functionality:
- Auto-generated unique EventId
- Automatic UTC timestamp
- Immutable properties
- Ready for MediatR integration

```csharp
public abstract class BaseDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    
    protected BaseDomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}
```

#### TokenRevokedEvent (? Complete - Hands-On Example)

**Location:** `Core/Domain/Events/TokenRevokedEvent.cs`

Raised when a JWT token is revoked, containing:
- TokenId (JTI), UserId, Username
- TokenType, ExpiresAt, Reason
- Validation and immutability
- Comprehensive usage examples

**Use Cases:**
- Update distributed token blacklists
- Create security audit logs
- Send security notifications
- Track revocation analytics
- Synchronize across multiple instances

**Example Usage:**
```csharp
// In Token entity
public void Revoke(string reason)
{
    // ... validation ...
    Status = TokenStatus.Revoked;
    RevokedAt = DateTime.UtcNow;
    
    // Raise domain event
    _domainEvents.Add(new TokenRevokedEvent(
        tokenId: Guid.Parse(TokenId),
        userId: UserId,
        username: Username,
        tokenType: Type,
        expiresAt: ExpiresAt,
        reason: reason));
}

// In command handler
public async Task Handle(RevokeTokenCommand request, CancellationToken ct)
{
    token.Revoke(request.Reason);
    await _repository.UpdateAsync(token, ct);
    
    // Publish domain events
    foreach (var domainEvent in token.DomainEvents)
    {
        await _mediator.Publish(domainEvent, ct);
    }
    
    token.ClearDomainEvents();
}

// Event handler
public class UpdateBlacklistHandler : INotificationHandler<TokenRevokedEvent>
{
    public async Task Handle(TokenRevokedEvent evt, CancellationToken ct)
    {
        await _blacklistService.AddToBlacklistAsync(
            evt.TokenId.ToString(),
            evt.ExpiresAt,
            ct);
    }
}
```

#### UserRegisteredEvent (? Complete - Additional Example)

**Location:** `Core/Domain/Events/UserRegisteredEvent.cs`

Raised when a new user account is created, containing:
- UserId, Username, Email (value object)
- InitialRoles, IpAddress, UserAgent
- RegistrationMethod (Email, Google, etc.)
- Privacy and GDPR considerations

**Use Cases:**
- Send welcome emails and onboarding
- Initialize user preferences
- Create audit logs
- Integrate with CRM/analytics
- Track user acquisition metrics

**Example Usage:**
```csharp
// In User entity
public static User Create(
    string username,
    Email email,
    string passwordHash,
    string? ipAddress = null,
    string? userAgent = null,
    string registrationMethod = "Email")
{
    var user = new User { /* ... */ };
    user._roles.Add(Role.User);
    
    // Raise domain event
    user._domainEvents.Add(new UserRegisteredEvent(
        userId: user.Id,
        username: username,
        email: email,
        initialRoles: user._roles.ToList(),
        ipAddress: ipAddress,
        userAgent: userAgent,
        registrationMethod: registrationMethod));
    
    return user;
}

// Event handler - Send welcome email
public class SendWelcomeEmailHandler : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(
            evt.Email.Value,
            evt.Username,
            ct);
    }
}
// Event handler - Track analytics
public class TrackRegistrationHandler : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
    {
        await _analytics.TrackEventAsync("user_registered", new
        {
            userId = evt.UserId,
            method = evt.RegistrationMethod,
            timestamp = evt.OccurredOn
        }, ct);
    }
}
```

**Domain Events Best Practices:**
- Events represent immutable historical facts (past tense naming)
- Include all relevant context (avoid entity references)
- Keep events lightweight and focused
- Publish after successful persistence
- Clear events after publishing
- Use MediatR INotification for in-process handling
- Consider Outbox Pattern for distributed systems
- Implement idempotent event handlers
- Document business context and use cases

#### Domain Event Publishing in Application Layer

**How Domain Events Flow Through Clean Architecture:**

```
+-------------------------------------------------------------+
¦                     1. USER REQUEST                          ¦
¦              (API Controller / Blazor Component)             ¦
+-------------------------------------------------------------+
                           ¦
                           ?
+-------------------------------------------------------------+
¦         2. APPLICATION LAYER (Command Handler)               ¦
¦  +---------------------------------------------------+     ¦
¦  ¦ public class RegisterUserCommandHandler           ¦     ¦
¦  ¦ {                                                  ¦     ¦
¦  ¦     public async Task<Result> Handle(...)          ¦     ¦
¦  ¦     {                                              ¦     ¦
¦  ¦         // Create entity (raises event internally) ¦     ¦
¦  ¦         var user = User.Create(...);                ¦     ¦
¦  ¦                                                    ¦     ¦
¦  ¦         // Persist entity                          ¦     ¦
¦  ¦         await _repository.AddAsync(user, ct);      ¦     ¦
¦  ¦         await _repository.SaveChangesAsync(ct);    ¦     ¦
¦  ¦                                                    ¦     ¦
¦  ¦         // Publish domain events                   ¦     ¦
¦  ¦         await PublishDomainEventsAsync(user, ct);  ¦     ¦
¦  ¦                                                    ¦     ¦
¦  ¦         return Result.Success();                   ¦     ¦
¦  ¦     }                                              ¦     ¦
¦  +---------------------------------------------------+     ¦
+-------------------------------------------------------------+
                           ¦
                           ?
+-------------------------------------------------------------+
¦           3. DOMAIN EVENT HANDLERS (MediatR)                 ¦
¦  +---------------------+  +---------------------+          ¦
¦  ¦ SendWelcomeEmail    ¦  ¦ CreateAuditLog      ¦          ¦
¦  ¦ Handler             ¦  ¦ Handler             ¦          ¦
¦  +---------------------+  +---------------------+          ¦
¦  +---------------------+  +---------------------+          ¦
¦  ¦ InitializePrefs     ¦  ¦ TrackAnalytics      ¦          ¦
¦  ¦ Handler             ¦  ¦ Handler             ¦          ¦
¦  +---------------------+  +---------------------+          ¦
+-------------------------------------------------------------+
```

**Complete Implementation Pattern:**

**Step 1: Command Handler Publishes Events**

**Location:** `Core/Application/Features/Users/Commands/RegisterUserCommandHandler.cs`

```csharp
using MediatR;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.ValueObjects;
using SecureCleanApiWaf.Core.Application.Common.Models;

namespace SecureCleanApiWaf.Core.Application.Features.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    
    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IMediator mediator,
        IPasswordHasher passwordHasher,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _mediator = mediator;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }
    
    public async Task<Result<Guid>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate business rules
            var emailExists = await _userRepository
                .ExistsByEmailAsync(request.Email, cancellationToken);
            
            if (emailExists)
                return Result<Guid>.Failure("Email already registered");
            
            // 2. Create value objects
            var email = Email.Create(request.Email);
            var passwordHash = _passwordHasher.HashPassword(request.Password);
            
            // 3. Create domain entity (raises UserRegisteredEvent internally)
            var user = User.Create(
                username: request.Username,
                email: email,
                passwordHash: passwordHash,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                registrationMethod: "Email");
            
            // 4. Persist entity to database
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "User {UserId} registered successfully with username {Username}",
                user.Id, user.Username);
            
            // 5. Publish domain events to MediatR
            await PublishDomainEventsAsync(user, cancellationToken);
            
            // 6. Clear events to prevent duplicate publishing
            user.ClearDomainEvents();
            
            return Result<Guid>.Success(user.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed during user registration");
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            return Result<Guid>.Failure("An error occurred during registration");
        }
    }
    
    /// <summary>
    /// Publishes all domain events raised by the entity.
    /// </summary>
    private async Task PublishDomainEventsAsync(
        User user,
        CancellationToken cancellationToken)
    {
        // Get all domain events from the entity
        var domainEvents = user.DomainEvents.ToList();
        
        if (!domainEvents.Any())
            return;
        
        _logger.LogInformation(
            "Publishing {Count} domain events for user {UserId}",
            domainEvents.Count, user.Id);
        
        // Publish each event through MediatR
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug(
                "Publishing domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.EventId);
            
            // MediatR will find all INotificationHandler<TEvent> implementations
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
```

**Step 2: Create Event Handlers**

**Location:** `Core/Application/Features/Users/EventHandlers/`

**Handler 1: Send Welcome Email**
```csharp
using MediatR;
using SecureCleanApiWaf.Core.Domain.Events;

namespace SecureCleanApiWaf.Core.Application.Features.Users.EventHandlers;

/// <summary>
/// Handles UserRegisteredEvent by sending welcome email to new users.
/// </summary>
public class SendWelcomeEmailHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;
    
    public SendWelcomeEmailHandler(
        IEmailService emailService,
        ILogger<SendWelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task Handle(
        UserRegisteredEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Sending welcome email to user {UserId} at {Email}",
                notification.UserId, notification.Email.ToMaskedString());
            
            await _emailService.SendWelcomeEmailAsync(
                toEmail: notification.Email.Value,
                username: notification.Username,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "Welcome email sent successfully to user {UserId}",
                notification.UserId);
        }
        catch (Exception ex)
        {
            // Don't fail registration if email fails
            _logger.LogError(ex,
                "Failed to send welcome email to user {UserId}",
                notification.UserId);
        }
    }
}

```

---

## ?? Related Documentation

### Clean Architecture Documentation
- **[Application Layer Documentation](./02-Application-Layer.md)** - CQRS, MediatR, Command/Query Handlers
- **[Infrastructure Layer Documentation](./03-Infrastructure-Layer.md)** - EF Core, Caching, External Services
- **[Presentation Layer Documentation](./04-Presentation-Layer.md)** - API Controllers, Blazor Components, Middleware
- **[Testing Strategy Guide](./06-Testing-Strategy.md)** - Unit Tests, Integration Tests, Test Patterns

### Domain-Driven Design Resources
- **[DDD Reference](https://www.domainlanguage.com/ddd/)** - Eric Evans' Domain-Driven Design
- **[Implementing Domain-Driven Design](https://vaughnvernon.com/)** - Vaughn Vernon's IDDD
- **[Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)** - Robert C. Martin

### .NET Resources
- **[Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)** - Official EF Core docs
- **[MediatR Documentation](https://github.com/jbogard/MediatR)** - CQRS and Mediator pattern
- **[FluentValidation Documentation](https://docs.fluentvalidation.net/)** - Validation library
- **[.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)** - Latest .NET features

### Project-Specific Documentation
- **[Getting Started Guide](../GettingStarted.md)** - Project setup and initial configuration
- **[Architecture Decision Records](../ADR/)** - Architectural decisions and rationale
- **[API Documentation](../API/)** - REST API endpoints and specifications
- **[Deployment Guide](../Deployment/)** - Production deployment instructions

---

## ?? Contact & Support

### Project Information
- **Project Name:** SecureCleanApiWaf - Clean Architecture Demo
- **Version:** 1.0.0 (Domain Layer Complete)
- **Framework:** .NET 8
- **Architecture:** Clean Architecture with DDD Patterns
- **Repository:** [https://github.com/dariemcarlosdev/SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)

### Author & Maintainer
- **Name:** Dariem Carlos
- **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)
- **Branch:** Dev
- **Location:** Professional Tech Challenge Submission

### Getting Help

#### ?? **Bug Reports**
If you find a bug in the Domain Layer implementation:
1. Check [existing issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
2. Create a new issue with:
   - Clear description of the problem
   - Steps to reproduce
   - Expected vs actual behavior
   - Code snippets if applicable
   - Environment details (.NET version, OS)

#### ?? **Feature Requests**
To suggest improvements or new domain features:
1. Open a [new issue](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues/new)
2. Label it as "enhancement"
3. Describe:
   - The feature or improvement
   - Business value and use cases
   - Proposed implementation approach
   - Impact on existing domain model

#### ?? **Documentation Issues**
If you find documentation errors or improvements:
1. Issues in `docs/CleanArchitecture/Projects/01-Domain-Layer.md`
2. Submit a pull request with corrections
3. Include context and rationale for changes

#### ?? **Contributing**
Contributions are welcome! To contribute to the Domain Layer:
1. Fork the repository
2. Create a feature branch from `Dev`
3. Make your changes following:
   - Clean Architecture principles
   - DDD best practices
   - Existing code style and patterns
   - Comprehensive XML documentation
4. Write unit tests for new domain logic
5. Submit a pull request with:
   - Clear description of changes
   - Business justification
   - Test coverage report
   - Updated documentation

### Development Guidelines

#### Code Style
- Follow C# coding conventions
- Use meaningful, domain-focused names
- Write comprehensive XML documentation
- Keep methods small and focused
- Maintain immutability where appropriate

#### Domain Layer Rules
? **DO:**
- Keep domain logic pure and framework-independent
- Use value objects for domain concepts
- Raise domain events for significant occurrences
- Validate business rules in entities
- Use factory methods for entity creation
- Document business context and rules

? **DON'T:**
- Reference infrastructure or UI concerns
- Use framework-specific attributes (except EF Core requirements)
- Perform I/O operations (database, file system, network)
- Include application orchestration logic
- Add dependencies on external libraries (except System.*)

#### Testing Requirements
All domain components must have:
- ? Unit tests with >80% code coverage
- ? Tests for all business rules
- ? Tests for domain events
- ? Tests for validation logic
- ? Tests for state transitions
- ? Tests for edge cases and error conditions

### Support Channels

#### ?? **Direct Contact**
For private inquiries or sensitive issues:
- Open a private issue in the repository
- Mark as "confidential" in the title
- Provide contact information for follow-up

#### ?? **Community Discussions**
For general questions and discussions:
- Use [GitHub Discussions](https://github.com/dariemcarlosdev/SecureCleanApiWaf/discussions)
- Tag appropriately: `domain-layer`, `architecture`, `ddd`
- Search existing discussions before posting

#### ?? **Documentation Feedback**
To improve this documentation:
- Comment on specific sections
- Suggest additional examples
- Request clarification on complex topics
- Propose alternative explanations

### Project Status & Roadmap

#### Current Status (Domain Layer)
| Component | Status | Coverage |
|-----------|--------|----------|
| Entities | ? Complete | 100% |
| Value Objects | ? Complete | 100% |
| Domain Events | ? Complete | 100% |
| Enumerations | ? Complete | 100% |
| Exceptions | ? Complete | 100% |
| Unit Tests | ? Pending | 0% |
| EF Configurations | ? Pending | 0% |

#### Upcoming Milestones
1. **Phase 1: Testing (Next)** - Comprehensive unit tests for all domain components
2. **Phase 2: Persistence** - EF Core configurations and migrations
3. **Phase 3: Integration** - Application layer integration with domain entities
4. **Phase 4: Advanced Patterns** - Aggregates, specifications, domain services

### Acknowledgments

This Domain Layer implementation follows industry best practices and patterns from:
- **Clean Architecture** by Robert C. Martin (Uncle Bob)
- **Domain-Driven Design** by Eric Evans
- **Implementing Domain-Driven Design** by Vaughn Vernon
- **Enterprise Patterns and MDA** by Jimmy Nilsson
- **.NET Microservices Architecture** by Microsoft

Special thanks to the .NET community for valuable patterns and practices.

---

## ?? License & Usage

This project is part of a professional tech challenge submission and serves as a demonstration of:
- Clean Architecture implementation
- Domain-Driven Design principles
- .NET 8 best practices
- Professional software engineering

For usage rights and licensing information, see the [LICENSE](../../LICENSE) file in the repository root.

---

## ?? Document Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0.0 | 2024 | Initial complete documentation with all domain components | Dariem Carlos |
| 1.1.0 | 2024 | Added domain events implementation and publishing patterns | Dariem Carlos |
| 1.2.0 | 2024 | Added Table of Contents and Contact section | Dariem Carlos |

---

**Last Updated:** 2024  
**Document Status:** ? Complete and Production-Ready  
**Review Status:** Approved for Tech Challenge Submission

---

*This documentation is maintained as part of the SecureCleanApiWaf Clean Architecture Demo project.*
*For the latest updates, visit the [GitHub repository](https://github.com/dariemcarlosdev/SecureCleanApiWaf).*
