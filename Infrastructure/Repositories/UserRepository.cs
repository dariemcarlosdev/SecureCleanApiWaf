using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.ValueObjects;
using SecureCleanApiWaf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SecureCleanApiWaf.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework Core implementation of IUserRepository.
    /// Provides data access for User entities using EF Core.
    /// </summary>
    /// <remarks>
    /// This repository implementation follows best practices:
    /// - Async/await for all database operations (scalability)
    /// - IQueryable composition for flexible querying
    /// - Proper use of AsNoTracking for read-only queries (performance)
    /// - Comprehensive error handling
    /// - Optimized queries with proper indexing
    /// 
    /// Clean Architecture Benefits:
    /// - Infrastructure layer implementation (EF Core details)
    /// - Application layer interface (business operations)
    /// - Domain layer entities (business rules)
    /// - Easy to swap implementations
    /// - Testable via interface mocking
    /// </remarks>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the UserRepository.
        /// </summary>
        /// <param name="context">EF Core database context.</param>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                return null;

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<User>> GetUsersByRoleAsync(Role role, CancellationToken cancellationToken = default)
        {
            if (role == null)
                return Array.Empty<User>();

            // Note: This assumes EF Core can query the Roles collection
            // Adjust based on your UserConfiguration setup
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.Roles.Contains(role))
                .OrderBy(x => x.Username)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await _context.Users.AddAsync(user, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Soft delete - user should already be marked as deleted
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Username.ToLower() == username.ToLower(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                return false;

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Email == email, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<User>> GetUsersWithFailedLoginAttemptsAsync(int threshold, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.FailedLoginAttempts >= threshold)
                .OrderByDescending(x => x.FailedLoginAttempts)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
