using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Domain.Enums;

namespace SecureCleanApiWaf.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Repository interface for Token aggregate root.
    /// Provides data access methods for Token entity following Repository pattern.
    /// </summary>
    /// <remarks>
    /// This interface abstracts data access for Token entities, supporting
    /// JWT token lifecycle management, blacklisting, and security auditing.
    /// 
    /// Key Responsibilities:
    /// - Token CRUD operations
    /// - Token lookup by various criteria (ID, JTI, User)
    /// - Token validation and status management
    /// - Token blacklisting support
    /// - Security audit queries
    /// 
    /// Benefits:
    /// - Decouples token management from infrastructure
    /// - Enables unit testing with mock repositories
    /// - Provides clear contract for token operations
    /// - Supports distributed token blacklisting
    /// 
    /// Implementation Note:
    /// The concrete implementation should be in the Infrastructure layer,
    /// typically using Entity Framework Core with optimized queries for
    /// token validation (indexed on TokenId and ExpiresAt).
    /// 
    /// Usage Example:
    /// ```csharp
    /// public class BlacklistTokenCommandHandler : IRequestHandler<BlacklistTokenCommand, Result<Unit>>
    /// {
    ///     private readonly ITokenRepository _tokenRepository;
    ///     
    ///     public async Task<Result<Unit>> Handle(BlacklistTokenCommand request, CancellationToken cancellationToken)
    ///     {
    ///         var token = await _tokenRepository.GetByTokenIdAsync(request.TokenId, cancellationToken);
    ///         
    ///         if (token == null || !token.IsValid())
    ///         {
    ///             return Result<Unit>.Fail("Token not found or already invalid");
    ///         }
    ///         
    ///         token.Revoke(request.Reason);
    ///         await _tokenRepository.UpdateAsync(token, cancellationToken);
    ///         await _tokenRepository.SaveChangesAsync(cancellationToken);
    ///         
    ///         return Result<Unit>.Ok();
    ///     }
    /// }
    /// ```
    /// </remarks>
    public interface ITokenRepository
    {
        /// <summary>
        /// Gets a token by its unique ID.
        /// </summary>
        /// <param name="id">The token's unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The token if found, null otherwise.</returns>
        Task<Token?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a token by its JWT ID (JTI claim).
        /// </summary>
        /// <param name="tokenId">The token ID (JTI claim from JWT).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The token if found, null otherwise.</returns>
        /// <remarks>
        /// Primary method for token validation during authentication.
        /// Should be optimized with database index on TokenId.
        /// </remarks>
        Task<Token?> GetByTokenIdAsync(string tokenId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tokens for a specific user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of tokens belonging to the user.</returns>
        /// <remarks>
        /// Used for user session management and security auditing.
        /// Returns all tokens (active, revoked, expired).
        /// </remarks>
        Task<IReadOnlyList<Token>> GetTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active (valid) tokens for a specific user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active tokens belonging to the user.</returns>
        /// <remarks>
        /// Used to display current user sessions or revoke all sessions.
        /// Only returns tokens with Active status and not expired.
        /// </remarks>
        Task<IReadOnlyList<Token>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tokens by type for a specific user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="tokenType">The token type (Access or Refresh).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of tokens of the specified type.</returns>
        Task<IReadOnlyList<Token>> GetTokensByUserAndTypeAsync(Guid userId, TokenType tokenType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all revoked tokens (for security auditing).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all revoked tokens.</returns>
        /// <remarks>
        /// Used for security auditing and blacklist management.
        /// Consider pagination for production use.
        /// </remarks>
        Task<IReadOnlyList<Token>> GetRevokedTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets expired tokens that need cleanup.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of expired tokens.</returns>
        /// <remarks>
        /// Used by background cleanup jobs to remove old token records.
        /// Consider pagination for production use.
        /// </remarks>
        Task<IReadOnlyList<Token>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a token ID exists and is valid (not revoked/expired).
        /// </summary>
        /// <param name="tokenId">The token ID (JTI claim).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if token exists and is valid, false otherwise.</returns>
        /// <remarks>
        /// Optimized query for fast token validation during authentication.
        /// Should use indexed queries for performance.
        /// </remarks>
        Task<bool> IsTokenValidAsync(string tokenId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a token is blacklisted (revoked).
        /// </summary>
        /// <param name="tokenId">The token ID (JTI claim).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if token is blacklisted, false otherwise.</returns>
        /// <remarks>
        /// Used during authentication to reject blacklisted tokens.
        /// Should be fast query with database index.
        /// </remarks>
        Task<bool> IsTokenBlacklistedAsync(string tokenId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new token to the repository.
        /// </summary>
        /// <param name="token">The token entity to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Creates a new token record in the database.
        /// Typically called after JWT generation.
        /// </remarks>
        Task AddAsync(Token token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing token in the repository.
        /// </summary>
        /// <param name="token">The token entity to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Updates token status, revocation info, etc.
        /// Should handle domain events if any were raised.
        /// </remarks>
        Task UpdateAsync(Token token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a token (hard delete, typically for cleanup).
        /// </summary>
        /// <param name="token">The token entity to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// Hard delete for expired tokens cleanup.
        /// For revocation, use Update with token.Revoke() instead.
        /// </remarks>
        Task DeleteAsync(Token token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple expired tokens in batch (for cleanup efficiency).
        /// </summary>
        /// <param name="tokens">List of tokens to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of tokens deleted.</returns>
        /// <remarks>
        /// Optimized batch operation for cleanup jobs.
        /// More efficient than deleting one by one.
        /// </remarks>
        Task<int> DeleteExpiredTokensAsync(IEnumerable<Token> tokens, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes all active tokens for a specific user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="reason">Reason for revocation (for audit).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of tokens revoked.</returns>
        /// <remarks>
        /// Used for "logout all sessions" functionality or security actions.
        /// Bulk operation that revokes all user's active tokens.
        /// </remarks>
        Task<int> RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets token statistics for monitoring and auditing.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics about token usage.</returns>
        /// <remarks>
        /// Provides insights for security monitoring and capacity planning.
        /// Returns counts of active, revoked, expired tokens.
        /// </remarks>
        Task<TokenStatistics> GetTokenStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of entities affected.</returns>
        /// <remarks>
        /// Commits the unit of work transaction.
        /// Should be called after Add/Update/Delete operations.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about token usage for monitoring purposes.
    /// </summary>
    public class TokenStatistics
    {
        /// <summary>
        /// Total number of active tokens in the system.
        /// </summary>
        public int ActiveTokens { get; set; }

        /// <summary>
        /// Total number of revoked tokens.
        /// </summary>
        public int RevokedTokens { get; set; }

        /// <summary>
        /// Total number of expired tokens.
        /// </summary>
        public int ExpiredTokens { get; set; }

        /// <summary>
        /// Total number of access tokens.
        /// </summary>
        public int AccessTokens { get; set; }

        /// <summary>
        /// Total number of refresh tokens.
        /// </summary>
        public int RefreshTokens { get; set; }

        /// <summary>
        /// Number of tokens created in the last 24 hours.
        /// </summary>
        public int TokensCreatedLast24Hours { get; set; }

        /// <summary>
        /// Number of tokens revoked in the last 24 hours.
        /// </summary>
        public int TokensRevokedLast24Hours { get; set; }

        /// <summary>
        /// When these statistics were calculated.
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
