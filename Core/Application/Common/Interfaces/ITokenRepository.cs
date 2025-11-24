using CleanArchitecture.ApiTemplate.Core.Domain.Entities;
using CleanArchitecture.ApiTemplate.Core.Domain.Enums;

namespace CleanArchitecture.ApiTemplate.Core.Application.Common.Interfaces
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
        /// <summary>
/// Retrieves a token by its unique identifier.
/// </summary>
/// <param name="id">The token's unique identifier.</param>
/// <returns>The token with the specified identifier, or null if not found.</returns>
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
        /// <summary>
/// Retrieves a token by its JTI (token identifier).
/// </summary>
/// <param name="tokenId">The token's JTI (JWT ID) to look up.</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
/// <returns>The matching <see cref="Token"/> if found, or `null` if no token exists for the specified `tokenId`.</returns>
/// <remarks>
/// This lookup is intended for fast validation paths and is typically backed by an index on the token identifier.
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
        /// <summary>
/// Retrieves all tokens associated with the specified user, including active, revoked, and expired tokens.
/// </summary>
/// <param name="userId">The unique identifier of the user whose tokens are being retrieved.</param>
/// <returns>An IReadOnlyList of Token entities belonging to the user; the list may include active, revoked, and expired tokens.</returns>
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
        /// <summary>
/// Gets active (not revoked and not expired) tokens for the specified user.
/// </summary>
/// <param name="userId">The unique identifier of the user whose active tokens to retrieve.</param>
/// <returns>A read-only list of tokens that are active for the specified user; the list is empty if none are found.</returns>
        Task<IReadOnlyList<Token>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tokens by type for a specific user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="tokenType">The token type (Access or Refresh).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <summary>
/// Retrieves tokens belonging to the specified user filtered by the given token type.
/// </summary>
/// <param name="userId">The unique identifier of the user whose tokens to retrieve.</param>
/// <param name="tokenType">The token type to filter by (e.g., Access or Refresh).</param>
/// <returns>An IReadOnlyList of tokens of the specified type for the user; empty if none are found.</returns>
        Task<IReadOnlyList<Token>> GetTokensByUserAndTypeAsync(Guid userId, TokenType tokenType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all revoked tokens (for security auditing).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all revoked tokens.</returns>
        /// <remarks>
        /// Used for security auditing and blacklist management.
        /// Consider pagination for production use.
        /// <summary>
/// Retrieves all tokens that have been revoked for auditing and blacklist management.
/// </summary>
/// <returns>A read-only list of revoked Token entities; empty if none are found.</returns>
        Task<IReadOnlyList<Token>> GetRevokedTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets expired tokens that need cleanup.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of expired tokens.</returns>
        /// <remarks>
        /// Used by background cleanup jobs to remove old token records.
        /// Consider pagination for production use.
        /// <summary>
/// Retrieves tokens that have passed their expiration time and are candidates for cleanup.
/// </summary>
/// <returns>An IReadOnlyList of tokens that are expired and eligible for removal.</returns>
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
        /// <summary>
/// Determines whether a token with the specified token ID (JTI) exists and is currently valid.
/// </summary>
/// <param name="tokenId">The token's unique identifier (JTI).</param>
/// <returns>`true` if the token exists and is not revoked or expired, `false` otherwise.</returns>
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
        /// <summary>
/// Determines whether a token identified by its JTI is blacklisted.
/// </summary>
/// <param name="tokenId">The token's unique identifier (JTI).</param>
/// <returns>`true` if the token is blacklisted (revoked), `false` otherwise.</returns>
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
        /// <summary>
/// Adds a new Token entity to the repository.
/// </summary>
/// <param name="token">The Token entity to add (typically created after JWT issuance).</param>
/// <remarks>
/// The addition is staged in the repository; call SaveChangesAsync to persist changes to the underlying store.
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
        /// <summary>
/// Updates an existing token entity in the repository.
/// </summary>
/// <param name="token">The token entity with changes to apply (must identify an existing token).</param>
/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
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
        /// <summary>
/// Permanently removes the specified token from the repository.
/// </summary>
/// <param name="token">The token entity to delete.</param>
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
        /// <summary>
/// Deletes the provided expired tokens in a single batch operation.
/// </summary>
/// <param name="tokens">Collection of expired Token entities to remove.</param>
/// <returns>The number of tokens that were deleted.</returns>
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
        /// <summary>
/// Revokes all active tokens belonging to the specified user.
/// </summary>
/// <param name="userId">The unique identifier of the user whose tokens will be revoked.</param>
/// <param name="reason">A brief reason to record for the revocation (audit/metadata).</param>
/// <returns>The number of tokens that were revoked.</returns>
        Task<int> RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets token statistics for monitoring and auditing.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics about token usage.</returns>
        /// <remarks>
        /// Provides insights for security monitoring and capacity planning.
        /// Returns counts of active, revoked, expired tokens.
        /// <summary>
/// Retrieves aggregated token usage and lifecycle metrics for monitoring and auditing.
/// </summary>
/// <returns>A <see cref="TokenStatistics"/> instance containing counts of active, revoked, and expired tokens, counts by token type (access/refresh), deltas for the last 24 hours, and the timestamp when the metrics were calculated.</returns>
        Task<TokenStatistics> GetTokenStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of entities affected.</returns>
        /// <remarks>
        /// Commits the unit of work transaction.
        /// Should be called after Add/Update/Delete operations.
        /// <summary>
/// Persists pending repository changes to the underlying data store.
/// </summary>
/// <returns>The number of state entries written to the data store.</returns>
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