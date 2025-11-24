using CleanArchitecture.ApiTemplate.Core.Domain.Enums;
using CleanArchitecture.ApiTemplate.Core.Domain.Events;
using CleanArchitecture.ApiTemplate.Core.Domain.Exceptions;
using CleanArchitecture.ApiTemplate.Core.Domain.ValueObjects;

namespace CleanArchitecture.ApiTemplate.Core.Domain.Entities
{
    /// <summary>
    /// User aggregate root.
    /// Represents a user account with authentication, roles, and lifecycle management.
    /// 
    /// This is an aggregate root that:
    /// - Manages user roles (consistency boundary)
    /// - Raises domain events (UserRegistered, UserDeleted)
    /// - Enforces business rules (CanLogin, role assignment)
    /// 
    /// Invariants:
    /// - Must have at least one role
    /// - Email must be valid (Email value object)
    /// - Status must be valid UserStatus enum
    /// </summary>
    /// <remarks>
    /// This entity encapsulates user account business logic and rules.
    /// Inherits audit fields and soft delete from BaseEntity.
    /// 
    /// Key Responsibilities:
    /// - User identity management
    /// - Role assignment and validation
    /// - Login tracking and security
    /// - Account lifecycle management
    /// - Business rule enforcement
    /// - Domain event publishing for user lifecycle changes
    /// 
    /// Domain Events:
    /// - UserRegisteredEvent: Raised when a new user account is created
    /// 
    /// Usage Example:
    /// ```csharp
    /// // Create new user (raises UserRegisteredEvent)
    /// var email = Email.Create("john.doe@example.com");
    /// var user = User.Create(
    ///     username: "johndoe",
    ///     email: email,
    ///     passwordHash: hashedPassword,
    ///     ipAddress: "192.168.1.1",
    ///     userAgent: "Mozilla/5.0...",
    ///     registrationMethod: "Email");
    /// 
    /// // Assign roles
    /// user.AssignRole(Role.User);
    /// user.AssignRole(Role.Admin);
    /// 
    /// // Track login
    /// user.RecordLogin("192.168.1.1", "Mozilla/5.0...");
    /// 
    /// // Manage account status
    /// user.Suspend("Policy violation");
    /// user.Activate();
    /// 
    /// // Publish domain events (typically in command handler)
    /// foreach (var domainEvent in user.DomainEvents)
    /// {
    ///     await mediator.Publish(domainEvent, cancellationToken);
    /// }
    /// user.ClearDomainEvents();
    /// ```
    /// </remarks>
    // ? Clear indicators this is an Aggregate Root
    public class User : BaseEntity, IAggregateRoot // 1. Inherits from BaseEntity for audit and soft delete  and implements IAggregateRoot
    {
        //2 . Encapsulated collections for roles and domain events ( private lists with public read-only accessors). User manages its own roles and domain events.
        private readonly List<Role> _roles = new();
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Gets the collection of domain events raised by this entity.
        /// </summary>
        /// <remarks>
        /// Domain events should be published by the application layer after
        /// successful persistence of the entity. Events should then be cleared
        /// using <see cref="ClearDomainEvents"/>.
        /// </remarks>
        // 6. Expose domain events as read-only collection
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Gets the username (unique identifier for login).
        /// </summary>
        public string Username { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the user's email address.
        /// </summary>
        public Email Email { get; private set; } = null!;

        /// <summary>
        /// Gets the hashed password (never store plain text!).
        /// </summary>
        /// <remarks>
        /// Should be hashed using bcrypt, Argon2, or PBKDF2.
        /// Never log or expose this property.
        /// </remarks>
        public string PasswordHash { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the user's assigned roles.
        /// </summary>
        // 5. Expose roles as read-only collection
        public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

        /// <summary>
        /// Gets the current account status.
        /// </summary>
        public UserStatus Status { get; private set; }

        /// <summary>
        /// Gets the timestamp of the last successful login.
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }

        /// <summary>
        /// Gets the IP address of the last login.
        /// </summary>
        public string? LastLoginIpAddress { get; private set; }

        /// <summary>
        /// Gets the user agent string of the last login.
        /// </summary>
        public string? LastLoginUserAgent { get; private set; }

        /// <summary>
        /// Gets the number of failed login attempts since last successful login.
        /// </summary>
        public int FailedLoginAttempts { get; private set; }

        /// <summary>
        /// Gets the timestamp when the account was locked (if applicable).
        /// </summary>
        public DateTime? LockedUntil { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// <summary>
/// Initializes a new instance of the <see cref="User"/> class for use by Entity Framework Core and other ORMs.
/// </summary>
        private User() { }

        /// <summary>
        /// Creates a new user with validated data.
        /// </summary>
        /// <param name="username">Unique username (3-50 characters).</param>
        /// <param name="email">Valid email address.</param>
        /// <param name="passwordHash">Hashed password (not plain text!).</param>
        /// <param name="ipAddress">IP address of registration request (optional).</param>
        /// <param name="userAgent">User agent string of registration request (optional).</param>
        /// <param name="registrationMethod">Registration method/source (e.g., "Email", "Google").</param>
        /// <returns>A new User entity.</returns>
        /// <exception cref="DomainException">Thrown when validation fails.</exception>
        /// <remarks>
        /// Raises <see cref="UserRegisteredEvent"/> when a new user is successfully created.
        /// This event should be published by the application layer for:
        /// - Sending welcome emails
        /// - Creating audit logs
        /// - Initializing user preferences
        /// - Integrating with external systems (CRM, analytics)
        /// </remarks>
        /// <summary>
        /// Create a new active User with the provided credentials, assign the default User role, and enqueue a UserRegisteredEvent.
        /// </summary>
        /// <param name="username">Desired username (3–50 characters; letters, digits, '.', '_', and '-' only).</param>
        /// <param name="email">Verified email value object for the user.</param>
        /// <param name="passwordHash">Non-empty hashed password for the user.</param>
        /// <param name="ipAddress">Optional IP address associated with the registration.</param>
        /// <param name="userAgent">Optional user agent string associated with the registration.</param>
        /// <param name="registrationMethod">Non-empty identifier of how the user registered (e.g., "Email").</param>
        /// <returns>The newly created User instance.</returns>
        /// <exception cref="DomainException">Thrown when any validation fails:
        /// username is empty, too short (<3) or too long (>50), contains invalid characters;
        /// email is null; passwordHash is empty; or registrationMethod is empty.</exception>
        public static User Create(
            string username,
            Email email,
            string passwordHash,
            string? ipAddress = null,
            string? userAgent = null,
            string registrationMethod = "Email")
        {
            // Validation: Username
            if (string.IsNullOrWhiteSpace(username))
                throw new DomainException("Username cannot be empty");

            if (username.Length < 3)
                throw new DomainException("Username must be at least 3 characters");

            if (username.Length > 50)
                throw new DomainException("Username cannot exceed 50 characters");

            if (!IsValidUsername(username))
                throw new DomainException(
                    "Username can only contain letters, numbers, dots, underscores, and hyphens");

            // Validation: Email
            if (email == null)
                throw new DomainException("Email is required");

            // Validation: Password Hash
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("Password hash cannot be empty");

            // Validation: Registration Method
            if (string.IsNullOrWhiteSpace(registrationMethod))
                throw new DomainException("Registration method is required");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Status = UserStatus.Active,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            // Assign default User role
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

        /// <summary>
        /// Assigns a role to the user.
        /// </summary>
        /// <param name="role">The role to assign.</param>
        /// <exception cref="DomainException">Thrown when role assignment violates business rules.</exception>
        /// <summary>
        /// Assigns the specified role to the user if the user's status allows role changes.
        /// </summary>
        /// <param name="role">The role to assign; must not be null. If the user already has this role, the call is ignored.</param>
        /// <exception cref="DomainException">Thrown when <paramref name="role"/> is null.</exception>
        /// <exception cref="InvalidDomainOperationException">Thrown when the user's current status does not permit assigning roles (for example, when suspended or locked).</exception>
        public void AssignRole(Role role)
        {
            if (role == null)
                throw new DomainException("Role cannot be null");

            if (_roles.Contains(role))
                return; // Already has role

            // Business Rule: Cannot assign roles to inactive accounts
            if (Status != UserStatus.Active && Status != UserStatus.Inactive)
            {
                throw new InvalidDomainOperationException(
                    "Assign role",
                    $"Cannot assign roles to {Status} accounts");
            }

            _roles.Add(role);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a role from the user.
        /// </summary>
        /// <param name="role">The role to remove.</param>
        /// <summary>
        /// Removes the specified role from the user while enforcing that the user retains at least one role.
        /// </summary>
        /// <param name="role">The role to remove.</param>
        /// <exception cref="DomainException">Thrown when <paramref name="role"/> is null.</exception>
        /// <exception cref="InvalidDomainOperationException">Thrown when removing the role would leave the user with no roles.</exception>
        public void RemoveRole(Role role)
        {
            if (role == null)
                throw new DomainException("Role cannot be null");

            // Business Rule: Must have at least one role
            if (_roles.Count == 1 && _roles.Contains(role))
            {
                throw new InvalidDomainOperationException(
                    "Remove role",
                    "User must have at least one role");
            }

            if (_roles.Remove(role))
            {
                UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Records a successful login attempt.
        /// </summary>
        /// <param name="ipAddress">The IP address of the login.</param>
        /// <summary>
        /// Record a successful login and update the user's last-login tracking and authentication state.
        /// </summary>
        /// <param name="ipAddress">The client's IP address, or null if not available.</param>
        /// <param name="userAgent">The client's user-agent string, or null if not available.</param>
        /// <exception cref="InvalidDomainOperationException">Thrown if the user is not allowed to log in (e.g., not active or otherwise blocked).</exception>
        public void RecordLogin(string? ipAddress, string? userAgent)
        {
            // Business Rule: Can only login if active
            if (!CanLogin())
            {
                throw new InvalidDomainOperationException(
                    "Record login",
                    $"Account status is {Status}");
            }

            LastLoginAt = DateTime.UtcNow;
            LastLoginIpAddress = ipAddress;
            LastLoginUserAgent = userAgent;
            FailedLoginAttempts = 0; // Reset on successful login
            LockedUntil = null; // Clear any temporary locks
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records a failed login attempt and locks account if threshold exceeded.
        /// </summary>
        /// <param name="maxAttempts">Maximum allowed failed attempts before locking (default: 5).</param>
        /// <summary>
        /// Records a failed login attempt for the user and locks the account when the configured threshold is reached.
        /// </summary>
        /// <param name="maxAttempts">Number of failed attempts required to trigger an account lock (default: 5).</param>
        /// <param name="lockoutDuration">Duration to lock the account when the threshold is reached (default: 15 minutes; null means a permanent lock).</param>
        public void RecordFailedLogin(int maxAttempts = 5, TimeSpan? lockoutDuration = null)
        {
            FailedLoginAttempts++;
            UpdatedAt = DateTime.UtcNow;

            // Lock account if threshold exceeded
            if (FailedLoginAttempts >= maxAttempts)
            {
                var lockDuration = lockoutDuration ?? TimeSpan.FromMinutes(15);
                Lock($"Exceeded {maxAttempts} failed login attempts", lockDuration);
            }
        }

        /// <summary>
        /// Deactivates the user account (voluntary user action).
        /// <summary>
        /// Mark the user account as inactive.
        /// </summary>
        /// <remarks>
        /// This operation is idempotent: if the account is already inactive it does nothing.
        /// On success it sets the user's status to <see cref="UserStatus.Inactive"/> and updates the <see cref="BaseEntity.UpdatedAt"/> timestamp.
        /// </remarks>
        /// <exception cref="InvalidDomainOperationException">
        /// Thrown when the current user status is not <see cref="UserStatus.Active"/> (and not already inactive).
        /// </exception>
        public void Deactivate()
        {
            if (Status == UserStatus.Inactive)
                return; // Already inactive

            if (Status != UserStatus.Active)
            {
                throw new InvalidDomainOperationException(
                    "Deactivate account",
                    $"Cannot deactivate {Status} account");
            }

            Status = UserStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Activates the user account.
        /// <summary>
        /// Activates the user account.
        /// </summary>
        /// <remarks>
        /// No-op if the account is already active. Throws <see cref="InvalidDomainOperationException"/> when the account is suspended and requires admin approval to reactivate. On success, sets the status to Active, resets failed login attempts, clears any temporary lock, and updates the UpdatedAt timestamp.
        /// </remarks>
        /// <exception cref="InvalidDomainOperationException">Thrown when attempting to activate a suspended account.</exception>
        public void Activate()
        {
            if (Status == UserStatus.Active)
                return; // Already active

            // Can activate from Inactive or unlocked Locked status
            if (Status == UserStatus.Suspended)
            {
                throw new InvalidDomainOperationException(
                    "Activate account",
                    "Suspended accounts require admin approval");
            }

            Status = UserStatus.Active;
            FailedLoginAttempts = 0;
            LockedUntil = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Suspends the user account (admin action).
        /// </summary>
        /// <summary>
        /// Suspends the user account and records an audit reason.
        /// </summary>
        /// <remarks>
        /// No-op if the user is already suspended.
        /// </remarks>
        /// <param name="reason">Non-empty reason for suspension used for auditing.</param>
        /// <exception cref="DomainException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
        public void Suspend(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Suspension reason is required");

            if (Status == UserStatus.Suspended)
                return; // Already suspended

            Status = UserStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;

            // Note: In a real system, you'd log this to an audit table
            // _domainEvents.Add(new UserSuspendedEvent(Id, reason, DateTime.UtcNow));
        }

        /// <summary>
        /// Locks the user account due to security concerns.
        /// </summary>
        /// <param name="reason">Reason for locking.</param>
        /// <summary>
        /// Locks the user account with a specified reason and optional duration.
        /// </summary>
        /// <param name="reason">A non-empty explanation for the lock; required.</param>
        /// <param name="duration">Optional lock duration; when null the lock is permanent.</param>
        /// <exception cref="DomainException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
        public void Lock(string reason, TimeSpan? duration = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Lock reason is required");

            Status = UserStatus.Locked;
            LockedUntil = duration.HasValue
                ? DateTime.UtcNow.Add(duration.Value)
                : null; // Permanent lock

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Unlocks a locked user account (admin action).
        /// <summary>
        /// Unlocks a locked user account, sets its status to Active, resets failed login attempts, clears any temporary lock expiration, and updates the modification timestamp.
        /// </summary>
        /// <exception cref="InvalidDomainOperationException">Thrown if the account is not currently in the Locked status.</exception>
        public void Unlock()
        {
            if (Status != UserStatus.Locked)
            {
                throw new InvalidDomainOperationException(
                    "Unlock account",
                    "Only locked accounts can be unlocked");
            }

            Status = UserStatus.Active;
            FailedLoginAttempts = 0;
            LockedUntil = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the user's email address.
        /// </summary>
        /// <summary>
        /// Replaces the user's email with the provided value and updates the entity's timestamp.
        /// </summary>
        /// <param name="newEmail">The new email value object to set.</param>
        /// <exception cref="DomainException">Thrown if <paramref name="newEmail"/> is null.</exception>
        public void UpdateEmail(Email newEmail)
        {
            if (newEmail == null)
                throw new DomainException("Email cannot be null");

            if (Email == newEmail)
                return; // No change

            Email = newEmail;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the user's password hash.
        /// </summary>
        /// <summary>
        /// Update the user's stored password hash and reset related authentication state.
        /// </summary>
        /// <param name="newPasswordHash">The new password hash (must be a non-empty hashed value).</param>
        /// <exception cref="DomainException">Thrown when <paramref name="newPasswordHash"/> is null, empty, or whitespace.</exception>
        public void UpdatePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new DomainException("Password hash cannot be empty");

            PasswordHash = newPasswordHash;
            FailedLoginAttempts = 0; // Reset on password change
            UpdatedAt = DateTime.UtcNow;

            // Note: Password change should invalidate all existing tokens
            // _domainEvents.Add(new PasswordChangedEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Checks if the user account is currently active.
        /// </summary>
        /// <summary>
/// Determines whether the user is currently active.
/// </summary>
/// <returns>`true` if the user's status is Active, `false` otherwise.</returns>
        public bool IsActive() => Status == UserStatus.Active;

        /// <summary>
        /// Checks if the user can log in.
        /// </summary>
        /// <summary>
        /// Determines whether the user is permitted to log in given their current status and lock state.
        /// </summary>
        /// <returns>`true` if the user is not deleted and either has status `Active` or had a temporary `Locked` status that has expired; `false` otherwise.</returns>
        public bool CanLogin()
        {
            // Cannot login if account is deleted
            if (IsDeleted)
                return false;

            // Cannot login if not active
            if (Status != UserStatus.Active)
                return false;

            // Check if temporary lock has expired
            if (Status == UserStatus.Locked && LockedUntil.HasValue)
            {
                if (DateTime.UtcNow >= LockedUntil.Value)
                {
                    // Temporary lock expired, allow login
                    return true;
                }
            }

            return Status == UserStatus.Active;
        }

        /// <summary>
        /// Checks if the user has a specific role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <summary>
        /// Determines whether the user has the specified role.
        /// </summary>
        /// <param name="role">The role to check; if null, it is treated as not present.</param>
        /// <returns>`true` if the user has the specified role, `false` otherwise.</returns>
        public bool HasRole(Role role)
        {
            return role != null && _roles.Contains(role);
        }

        /// <summary>
        /// Checks if the user has any admin privileges.
        /// </summary>
        /// <summary>
        /// Determines whether the user has an administrative role.
        /// </summary>
        /// <returns>`true` if the user has an administrative role (Admin or SuperAdmin), `false` otherwise.</returns>
        public bool IsAdmin()
        {
            return _roles.Any(r => r.IsAdmin());
        }

        /// <summary>
        /// Checks if the user has SuperAdmin privileges.
        /// </summary>
        /// <summary>
        /// Determines whether the user has the SuperAdmin role.
        /// </summary>
        /// <returns>`true` if the user has the SuperAdmin role, `false` otherwise.</returns>
        public bool IsSuperAdmin()
        {
            return _roles.Any(r => r.IsSuperAdmin());
        }

        /// <summary>
        /// Validates username format.
        /// </summary>
        /// <param name="username">Username to validate.</param>
        /// <summary>
        /// Determines whether a username contains only allowed characters.
        /// </summary>
        /// <returns>`true` if the username consists exclusively of letters, digits, '.', '_', or '-', `false` otherwise.</returns>
        private static bool IsValidUsername(string username)
        {
            // Allow: letters, numbers, dots, underscores, hyphens
            // No spaces, no special characters
            return username.All(c =>
                char.IsLetterOrDigit(c) ||
                c == '.' ||
                c == '_' ||
                c == '-');
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
        /// await _repository.AddAsync(user, cancellationToken);
        /// 
        /// foreach (var domainEvent in user.DomainEvents)
        /// {
        ///     await _mediator.Publish(domainEvent, cancellationToken);
        /// }
        /// 
        /// user.ClearDomainEvents();
        /// </code>
        /// <summary>
        /// Removes all domain events accumulated by the aggregate.
        /// </summary>
        /// <remarks>
        /// Call after domain events have been published to prevent re-publishing.
        /// </remarks>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}