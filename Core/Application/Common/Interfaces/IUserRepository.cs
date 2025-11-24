using CleanArchitecture.ApiTemplate.Core.Domain.Entities;
using CleanArchitecture.ApiTemplate.Core.Domain.ValueObjects;

namespace CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Repository interface for User aggregate root.
    /// Provides data access methods for User entity following Repository pattern.
    /// </summary>
    /// <remarks>
    /// This interface abstracts data access for User entities, following the
    /// Repository pattern and Dependency Inversion Principle of Clean Architecture.
    /// 
    /// Key Responsibilities:
    /// - User CRUD operations
    /// - User lookup by various criteria
    /// - User authentication and validation
    /// - User role management
    /// 
    /// Benefits:
    /// - Decouples domain from infrastructure
    /// - Enables unit testing with mock repositories
    /// - Provides clear contract for data operations
    /// - Allows switching data stores without changing business logic
    /// 
    /// Implementation Note:
    /// The concrete implementation should be in the Infrastructure layer,
    /// typically using Entity Framework Core or other ORM.
    /// 
    /// Usage Example:
    /// ```csharp
    /// public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<LoginResponse>>
    /// {
    ///     private readonly IUserRepository _userRepository;
    ///     
    ///     public async Task<Result<LoginResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    ///     {
    ///         var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
    ///         
    ///         if (user == null || !user.CanLogin())
    ///         {
    ///             return Result<LoginResponse>.Fail("Invalid credentials");
    ///         }
    ///         
    ///         // Verify password, generate token, etc.
    ///     }
    /// }
    /// ```
    /// </remarks>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by their unique ID.
        /// </summary>
        /// <param name="id">The user's unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <summary>
/// Retrieve a user by their unique identifier.
/// </summary>
/// <param name="id">The user's unique identifier.</param>
/// <returns>The user with the specified ID, or null if no matching user is found.</returns>
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by their username.
        /// </summary>
        /// <param name="username">The username (case-insensitive).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, null otherwise.</returns>
        /// <remarks>
        /// Used primarily for authentication/login operations.
        /// Should perform case-insensitive comparison.
        /// <summary>
/// Retrieves a user by username using a case-insensitive comparison.
/// </summary>
/// <param name="username">The username to look up (comparison should be case-insensitive).</param>
/// <returns>The matching <see cref="User"/>, or <c>null</c> if no user exists with the given username.</returns>
/// <remarks>
/// Primarily used for authentication and login flows; implementations should consider only non-deleted users when performing the lookup.
/// </remarks>
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, null otherwise.</returns>
        /// <remarks>
        /// Used for password reset, email verification, etc.
        /// Should perform case-insensitive comparison.
        /// <summary>
/// Retrieve a user by email address using a case-insensitive comparison.
/// </summary>
/// <param name="email">The email address to find.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>The matching <see cref="User"/> if found, or <c>null</c> otherwise.</returns>
/// <remarks>
/// Should consider only non-deleted users and be suitable for scenarios such as password reset and account verification.
/// </remarks>
        Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all users with a specific role.
        /// </summary>
        /// <param name="role">The role to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of users with the specified role.</returns>
        /// <remarks>
        /// Useful for administrative operations and reporting.
        /// <summary>
/// Retrieve all users assigned the specified role.
/// </summary>
/// <param name="role">The role used to filter users.</param>
/// <returns>A read-only list of users that have the specified role.</returns>
        Task<IReadOnlyList<User>> GetUsersByRoleAsync(Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new user to the repository.
        /// </summary>
        /// <param name="user">The user entity to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Should validate that username and email are unique before adding.
        /// Raises domain events (UserRegisteredEvent) that should be published after persistence.
        /// <summary>
/// Adds a new User to the repository.
/// </summary>
/// <param name="user">The User aggregate to persist; username and email must be unique among non-deleted users.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <remarks>
/// Implementations should validate uniqueness of username and email (considering non-deleted users) and may publish domain events (for example, a UserRegisteredEvent) after the user has been persisted.
/// </remarks>
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user in the repository.
        /// </summary>
        /// <param name="user">The user entity to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Updates all modified properties including roles.
        /// Should handle domain events if any were raised during entity modifications.
        /// <summary>
/// Updates an existing user's state in the repository.
/// </summary>
/// <param name="user">The User aggregate with updated values to persist.</param>
/// <remarks>
/// Implementations should persist all modified properties (including roles) and handle any domain events raised by the aggregate.
/// </remarks>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user (soft delete).
        /// </summary>
        /// <param name="user">The user entity to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Performs soft delete by setting IsDeleted flag.
        /// User data is preserved for audit purposes.
        /// <summary>
/// Marks the specified user as deleted in the repository using a soft-delete strategy.
/// </summary>
/// <param name="user">The user entity to delete; its state will be updated to indicate deletion.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
/// <remarks>
/// Implementations should persist the soft-delete (for example by setting an `IsDeleted` flag), preserve the record for auditing, and handle any domain events raised during deletion.
/// </remarks>
        Task DeleteAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a username already exists in the system.
        /// </summary>
        /// <param name="username">The username to check (case-insensitive).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if username exists, false otherwise.</returns>
        /// <remarks>
        /// Used during user registration to ensure uniqueness.
        /// Should check against non-deleted users only.
        /// <summary>
/// Checks whether a username is already registered in the repository.
/// </summary>
/// <param name="username">The username to check (comparison should be case-insensitive).</param>
/// <returns>`true` if a non-deleted user with the specified username exists, `false` otherwise.</returns>
/// <remarks>
/// Comparison should ignore case and consider only users that are not soft-deleted.
/// This is typically used during user registration to enforce username uniqueness.
/// </remarks>
        Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an email already exists in the system.
        /// </summary>
        /// <param name="email">The email to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if email exists, false otherwise.</returns>
        /// <remarks>
        /// Used during user registration to ensure uniqueness.
        /// Should check against non-deleted users only.
        /// <summary>
/// Determines whether an active user with the specified email address already exists.
/// </summary>
/// <param name="email">The email address to check (value object representing an email).</param>
/// <returns>`true` if a non-deleted user exists with the specified email (comparison is case-insensitive), `false` otherwise.</returns>
        Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets users with failed login attempts exceeding threshold.
        /// </summary>
        /// <param name="threshold">Minimum number of failed attempts.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of users with excessive failed login attempts.</returns>
        /// <remarks>
        /// Used for security monitoring and account lockout management.
        /// <summary>
/// Retrieves users whose recorded failed login attempts are greater than or equal to the specified threshold.
/// </summary>
/// <param name="threshold">Minimum number of failed login attempts a user must have to be included.</param>
/// <returns>A read-only list of users whose failed login attempts meet or exceed the threshold.</returns>
/// <remarks>
/// Intended for security monitoring and lockout management; implementations should consider only active (non-deleted) users.
/// </remarks>
        Task<IReadOnlyList<User>> GetUsersWithFailedLoginAttemptsAsync(int threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of entities affected.</returns>
        /// <remarks>
        /// Commits the unit of work transaction.
        /// Should be called after Add/Update/Delete operations.
        /// <summary>
/// Persists all pending changes in the repository as a unit-of-work commit.
/// </summary>
/// <returns>The number of affected entities persisted to the data store.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}