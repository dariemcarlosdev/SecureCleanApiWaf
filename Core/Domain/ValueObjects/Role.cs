using SecureCleanApiWaf.Core.Domain.Exceptions;

namespace SecureCleanApiWaf.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents a user role in the system with predefined role types.
    /// </summary>
    /// <remarks>
    /// This value object encapsulates role validation and comparison logic,
    /// ensuring only valid roles exist in the domain model.
    /// 
    /// Why Value Object for Role?
    /// - **Type Safety**: Prevents arbitrary string roles
    /// - **Predefined Set**: Only valid roles (User, Admin, SuperAdmin)
    /// - **Immutability**: Role cannot be changed after creation
    /// - **Comparison**: Value-based equality
    /// - **Domain Language**: Makes code more expressive
    /// 
    /// Role Hierarchy:
    /// ```
    /// SuperAdmin (highest)
    ///     ?
    ///   Admin
    ///     ?
    ///   User (lowest)
    /// ```
    /// 
    /// Usage Examples:
    /// ```csharp
    /// // Using predefined roles
    /// var userRole = Role.User;
    /// var adminRole = Role.Admin;
    /// 
    /// // Creating from string
    /// var role = Role.Create("Admin");
    /// 
    /// // Checking permissions
    /// if (role.IsAdmin())
    /// {
    ///     // Allow admin operations
    /// }
    /// 
    /// // Comparing roles
    /// var role1 = Role.User;
    /// var role2 = Role.User;
    /// Console.WriteLine(role1 == role2); // True
    /// ```
    /// 
    /// Entity Framework Configuration:
    /// ```csharp
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     // Store as string in database
    ///     modelBuilder.Entity<User>()
    ///         .Property(u => u.Roles)
    ///         .HasConversion(
    ///             roles => string.Join(",", roles.Select(r => r.Name)),
    ///             value => value.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ///                          .Select(r => Role.Create(r))
    ///                          .ToList()
    ///         );
    /// }
    /// ```
    /// </remarks>
    public class Role : ValueObject
    {
        /// <summary>
        /// Gets the role name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        /// <summary>
        /// Initializes a Role instance with the specified canonical role name.
        /// </summary>
        /// <param name="name">The canonical role name (for example: "User", "Admin", "SuperAdmin").</param>
        private Role(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Standard User role with basic permissions.
        /// </summary>
        /// <remarks>
        /// Default role for regular users. Has access to:
        /// - Read operations
        /// - Own profile management
        /// - Basic features
        /// 
        /// Cannot access:
        /// - Admin endpoints
        /// - User management
        /// - System configuration
        /// </remarks>
        public static readonly Role User = new("User");

        /// <summary>
        /// Administrator role with elevated permissions.
        /// </summary>
        /// <remarks>
        /// Elevated role for administrators. Has access to:
        /// - All User permissions
        /// - User management
        /// - Configuration management
        /// - Reporting and analytics
        /// 
        /// Cannot access:
        /// - System-wide settings (SuperAdmin only)
        /// - Security configurations (SuperAdmin only)
        /// </remarks>
        public static readonly Role Admin = new("Admin");

        /// <summary>
        /// Super Administrator role with full system access.
        /// </summary>
        /// <remarks>
        /// Highest privilege role. Has access to:
        /// - All Admin permissions
        /// - System-wide configuration
        /// - Security settings
        /// - Role management
        /// - Audit logs
        /// 
        /// Use Cases:
        /// - System initialization
        /// - Emergency access
        /// - Security administration
        /// - Compliance and auditing
        /// 
        /// Security Note:
        /// - Limit number of SuperAdmin accounts
        /// - Monitor all SuperAdmin activities
        /// - Require MFA for SuperAdmin access
        /// </remarks>
        public static readonly Role SuperAdmin = new("SuperAdmin");

        /// <summary>
        /// Creates a Role from a string representation.
        /// </summary>
        /// <param name="roleName">The role name (case-insensitive).</param>
        /// <returns>A Role value object.</returns>
        /// <exception cref="DomainException">Thrown when role name is invalid.</exception>
        /// <remarks>
        /// Performs case-insensitive matching to predefined roles.
        /// 
        /// Supported Values:
        /// - "User" or "user" ? Role.User
        /// - "Admin" or "admin" ? Role.Admin
        /// - "SuperAdmin", "superadmin", "super-admin" ? Role.SuperAdmin
        /// 
        /// Examples:
        /// ```csharp
        /// var role1 = Role.Create("user");      // Valid
        /// var role2 = Role.Create("ADMIN");     // Valid
        /// var role3 = Role.Create("SuperAdmin"); // Valid
        /// var role4 = Role.Create("Guest");     // DomainException
        /// ```
        /// 
        /// Why Static Factory Method?
        /// - Centralized validation
        /// - Consistent error messages
        /// - Easy to extend with new roles
        /// - Thread-safe for predefined roles
        /// <summary>
        /// Creates a Role from a string representation, validating and mapping it to a predefined Role instance.
        /// </summary>
        /// <param name="roleName">The role name to parse; accepts "User", "Admin", "SuperAdmin" (also "Super-Admin"), matched case-insensitively.</param>
        /// <returns>The corresponding predefined <see cref="Role"/> instance.</returns>
        /// <exception cref="DomainException">Thrown when <paramref name="roleName"/> is null, empty, whitespace, or does not match a valid predefined role.</exception>
        public static Role Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new DomainException("Role name cannot be empty");
            }

            // Normalize input for comparison
            var normalizedName = roleName.Trim();

            // Match against predefined roles (case-insensitive)
            return normalizedName.ToLowerInvariant() switch
            {
                "user" => User,
                "admin" => Admin,
                "superadmin" or "super-admin" => SuperAdmin,
                _ => throw new DomainException(
                    $"Unknown role: {roleName}. Valid roles are: User, Admin, SuperAdmin")
            };
        }

        /// <summary>
        /// Checks if this role has administrative privileges.
        /// </summary>
        /// <returns>True if role is Admin or SuperAdmin, false otherwise.</returns>
        /// <remarks>
        /// Useful for authorization checks in application layer.
        /// 
        /// Usage Example:
        /// ```csharp
        /// public async Task<Result<Unit>> Handle(DeleteUserCommand request)
        /// {
        ///     var currentUser = await _userRepository.GetByIdAsync(request.CurrentUserId);
        ///     
        ///     // Check if user has admin privileges
        ///     if (!currentUser.Roles.Any(r => r.IsAdmin()))
        ///     {
        ///         return Result<Unit>.Fail("Insufficient permissions");
        ///     }
        ///     
        ///     // Proceed with deletion
        /// }
        /// ```
        /// <summary>
        /// Determines whether the role has administrator privileges.
        /// </summary>
        /// <returns>`true` if the role is `Admin` or `SuperAdmin`, `false` otherwise.</returns>
        public bool IsAdmin()
        {
            return this == Admin || this == SuperAdmin;
        }

        /// <summary>
        /// Checks if this role is SuperAdmin.
        /// </summary>
        /// <returns>True if role is SuperAdmin, false otherwise.</returns>
        /// <remarks>
        /// Use for operations requiring highest privilege level.
        /// 
        /// Usage Example:
        /// ```csharp
        /// public async Task<Result<Unit>> Handle(ChangeSystemSettingsCommand request)
        /// {
        ///     var currentUser = await _userRepository.GetByIdAsync(request.UserId);
        ///     
        ///     // Only SuperAdmin can change system settings
        ///     if (!currentUser.Roles.Any(r => r.IsSuperAdmin()))
        ///     {
        ///         return Result<Unit>.Fail("Only SuperAdmin can modify system settings");
        ///     }
        ///     
        ///     // Proceed with changes
        /// }
        /// ```
        /// <summary>
        /// Determines whether this role is the SuperAdmin role.
        /// </summary>
        /// <returns>`true` if the role is SuperAdmin, `false` otherwise.</returns>
        public bool IsSuperAdmin()
        {
            return this == SuperAdmin;
        }

        /// <summary>
        /// Checks if this role is a standard User role.
        /// </summary>
        /// <returns>True if role is User, false otherwise.</returns>
        /// <remarks>
        /// Useful for identifying non-privileged accounts.
        /// 
        /// Usage Example:
        /// ```csharp
        /// // Apply rate limiting to regular users
        /// if (currentUser.Roles.All(r => r.IsUser()))
        /// {
        ///     await _rateLimiter.CheckLimitAsync(currentUser.Id);
        /// }
        /// ```
        /// <summary>
        /// Determines whether the role represents the standard User role.
        /// </summary>
        /// <returns>true if this role is User; false otherwise.</returns>
        public bool IsUser()
        {
            return this == User;
        }

        /// <summary>
        /// Checks if this role has permission to perform the specified action.
        /// </summary>
        /// <param name="requiredRole">The minimum role required for the action.</param>
        /// <returns>True if current role meets or exceeds the required role.</returns>
        /// <remarks>
        /// Implements role hierarchy checking.
        /// 
        /// Role Hierarchy:
        /// - SuperAdmin can do everything
        /// - Admin can do Admin and User actions
        /// - User can only do User actions
        /// 
        /// Usage Example:
        /// ```csharp
        /// public bool CanDeleteUser(User currentUser, User targetUser)
        /// {
        ///     // Need Admin role to delete users
        ///     return currentUser.Roles.Any(r => r.HasPermission(Role.Admin));
        /// }
        /// ```
        /// <summary>
        /// Determines whether the current role satisfies a required role according to the role hierarchy.
        /// </summary>
        /// <param name="requiredRole">The minimum role required for the operation; if null, the requirement is not satisfied.</param>
        /// <returns>`true` if the current role meets or exceeds the required role in the hierarchy (SuperAdmin &gt; Admin &gt; User), `false` otherwise.</returns>
        public bool HasPermission(Role requiredRole)
        {
            if (requiredRole == null)
                return false;

            // SuperAdmin has all permissions
            if (this == SuperAdmin)
                return true;

            // Admin has Admin and User permissions
            if (this == Admin)
                return requiredRole == Admin || requiredRole == User;

            // User only has User permissions
            if (this == User)
                return requiredRole == User;

            return false;
        }

        /// <summary>
        /// Gets a display-friendly name for the role.
        /// </summary>
        /// <returns>Formatted role name.</returns>
        /// <remarks>
        /// Useful for UI display purposes.
        /// 
        /// Examples:
        /// - Role.User ? "User"
        /// - Role.Admin ? "Administrator"
        /// - Role.SuperAdmin ? "Super Administrator"
        /// <summary>
        /// Gets a user-friendly display name for the role.
        /// </summary>
        /// <returns>The display name: "User" for User, "Administrator" for Admin, "Super Administrator" for SuperAdmin; otherwise the raw role name.</returns>
        public string GetDisplayName()
        {
            return Name switch
            {
                "User" => "User",
                "Admin" => "Administrator",
                "SuperAdmin" => "Super Administrator",
                _ => Name
            };
        }

        /// <summary>
        /// Returns the role name.
        /// </summary>
        /// <summary>
        /// Gets the role's name for display or logging.
        /// </summary>
        /// <returns>The underlying role name.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets the equality components for value object comparison.
        /// </summary>
        /// <returns>An enumerable of components (role name).</returns>
        /// <remarks>
        /// Roles are compared by their name.
        /// Role.Create("Admin") == Role.Admin returns true.
        /// <summary>
        /// Provides the components used to determine value equality for this Role.
        /// </summary>
        /// <returns>An enumerable of equality-significant components; yields the role's <c>Name</c>.</returns>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }

        /// <summary>
        /// Implicit conversion from Role to string.
        /// </summary>
        /// <param name="role">The role value object.</param>
        public static implicit operator string(Role role)
        {
            return role.Name;
        }

        /// <summary>
        /// Gets all available roles in the system.
        /// </summary>
        /// <returns>List of all predefined roles.</returns>
        /// <remarks>
        /// Useful for dropdowns, configuration, and validation.
        /// 
        /// Usage Example:
        /// ```csharp
        /// // Populate role dropdown
        /// var availableRoles = Role.GetAllRoles();
        /// foreach (var role in availableRoles)
        /// {
        ///     roleDropdown.Items.Add(new SelectListItem 
        ///     { 
        ///         Text = role.GetDisplayName(), 
        ///         Value = role.Name 
        ///     });
        /// }
        /// ```
        /// <summary>
        /// Enumerates all predefined Role instances.
        /// </summary>
        /// <returns>An enumerable containing the predefined Role instances: User, Admin, and SuperAdmin.</returns>
        public static IEnumerable<Role> GetAllRoles()
        {
            yield return User;
            yield return Admin;
            yield return SuperAdmin;
        }
    }
}