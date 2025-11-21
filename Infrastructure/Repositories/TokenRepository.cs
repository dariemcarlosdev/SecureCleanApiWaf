using SecureCleanApiWaf.Core.Application.Common.DTOs;
using SecureCleanApiWaf.Core.Application.Common.Interfaces;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.Enums;
using SecureCleanApiWaf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SecureCleanApiWaf.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework Core implementation of ITokenRepository.
    /// Provides data access for Token entities using EF Core.
    /// </summary>
    /// <remarks>
    /// This repository implementation follows best practices:
    /// - Async/await for all database operations
    /// - Optimized queries with proper indexing
    /// - AsNoTracking for read-only queries
    /// - Batch operations for efficiency
    /// 
    /// Performance Considerations:
    /// - TokenId should be indexed for fast blacklist lookups
    /// - ExpiresAt should be indexed for cleanup queries
    /// - UserId should be indexed for user session management
    /// </remarks>
    public class TokenRepository : ITokenRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the TokenRepository.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of <see cref="TokenRepository"/> backed by the supplied EF Core context.
        /// </summary>
        /// <param name="context">The EF Core <see cref="ApplicationDbContext"/> used for token data access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
        public TokenRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieve the token with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the token to retrieve.</param>
        /// <returns>The token with the specified Id, or `null` if no matching token is found.</returns>
        public async Task<Token?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Tokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <summary>
        /// Retrieve the token that matches the specified token identifier.
        /// </summary>
        /// <param name="tokenId">The token identifier to search for. If null, empty, or whitespace, no lookup is performed.</param>
        /// <returns>The matching <see cref="Token"/> if found; otherwise <c>null</c>.</returns>
        public async Task<Token?> GetByTokenIdAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return null;

            // Uses index on TokenId for fast lookup
            return await _context.Tokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TokenId == tokenId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Token>> GetTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Tokens
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Token>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _context.Tokens
                .AsNoTracking()
                .Where(x => x.UserId == userId 
                    && x.Status == TokenStatus.Active 
                    && x.ExpiresAt > now)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Token>> GetTokensByUserAndTypeAsync(Guid userId, TokenType tokenType, CancellationToken cancellationToken = default)
        {
            return await _context.Tokens
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Type == tokenType)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all tokens marked as revoked, ordered by most recent revocation first.
        /// </summary>
        /// <returns>A read-only list of revoked tokens ordered by `RevokedAt` descending.</returns>
        public async Task<IReadOnlyList<Token>> GetRevokedTokensAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Tokens
                .AsNoTracking()
                .Where(x => x.Status == TokenStatus.Revoked)
                .OrderByDescending(x => x.RevokedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Token>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _context.Tokens
                .AsNoTracking()
                .Where(x => x.ExpiresAt < now)
                .OrderBy(x => x.ExpiresAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> IsTokenValidAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var now = DateTime.UtcNow;

            return await _context.Tokens
                .AsNoTracking()
                .AnyAsync(x => x.TokenId == tokenId 
                    && x.Status == TokenStatus.Active 
                    && x.ExpiresAt > now, 
                    cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> IsTokenBlacklistedAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var now = DateTime.UtcNow;

            // Token is blacklisted if it's revoked and not yet expired
            return await _context.Tokens
                .AsNoTracking()
                .AnyAsync(x => x.TokenId == tokenId 
                    && x.Status == TokenStatus.Revoked 
                    && x.ExpiresAt > now, 
                    cancellationToken);
        }

        /// <summary>
        /// Adds the specified Token entity to the EF Core context so it will be persisted on the next save.
        /// </summary>
        /// <param name="token">The Token entity to add.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the add operation to complete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null.</exception>
        public async Task AddAsync(Token token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            await _context.Tokens.AddAsync(token, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Token token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            _context.Tokens.Update(token);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Marks the specified token for deletion from the database context.
        /// </summary>
        /// <param name="token">The token entity to remove; cannot be null.</param>
        /// <remarks>
        /// The token is removed from the DbContext. Changes are not persisted until <see cref="SaveChangesAsync(CancellationToken)"/> is called.
        /// </remarks>
        public async Task DeleteAsync(Token token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            _context.Tokens.Remove(token);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> DeleteExpiredTokensAsync(IEnumerable<Token> tokens, CancellationToken cancellationToken = default)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));

            var tokenList = tokens.ToList();
            if (tokenList.Count == 0)
                return 0;

            _context.Tokens.RemoveRange(tokenList);
            return await SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var activeTokens = await _context.Tokens
                .Where(x => x.UserId == userId 
                    && x.Status == TokenStatus.Active 
                    && x.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            if (activeTokens.Count == 0)
                return 0;

            foreach (var token in activeTokens)
            {
                token.Revoke(reason);
            }

            return await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Computes aggregate counts for tokens (active, revoked, expired) and by type, using the current UTC time.
        /// </summary>
        /// <returns>A <see cref="TokenStatistics"/> object containing counts for active, revoked, expired, access, and refresh tokens, and the UTC timestamp when the values were calculated.</returns>
        public async Task<TokenStatistics> GetTokenStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var activeCount = await _context.Tokens
                .AsNoTracking()
                .CountAsync(x => x.Status == TokenStatus.Active && x.ExpiresAt > now, cancellationToken);

            var revokedCount = await _context.Tokens
                .AsNoTracking()
                .CountAsync(x => x.Status == TokenStatus.Revoked && x.ExpiresAt > now, cancellationToken);

            var expiredCount = await _context.Tokens
                .AsNoTracking()
                .CountAsync(x => x.ExpiresAt <= now, cancellationToken);

            var accessTokenCount = await _context.Tokens
                .AsNoTracking()
                .CountAsync(x => x.Type == TokenType.AccessToken, cancellationToken);

            var refreshTokenCount = await _context.Tokens
                .AsNoTracking()
                .CountAsync(x => x.Type == TokenType.RefreshToken, cancellationToken);

            return new TokenStatistics
            {
                ActiveTokens = activeCount,
                RevokedTokens = revokedCount,
                ExpiredTokens = expiredCount,
                AccessTokens = accessTokenCount,
                RefreshTokens = refreshTokenCount,
                CalculatedAt = now
            };
        }

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}