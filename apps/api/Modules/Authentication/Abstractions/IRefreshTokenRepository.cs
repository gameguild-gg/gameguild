namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository interface for refresh token data access operations
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Get refresh token by ID
    /// </summary>
    /// <param name="id">The token ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token or null if not found</returns>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refresh token by token value
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token or null if not found</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active refresh tokens</returns>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all refresh tokens for a user (including inactive)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all refresh tokens</returns>
    Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created refresh token</returns>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated refresh token</returns>
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="revokedByIp">IP address that revoked the token</param>
    /// <param name="replacedByToken">Optional replacement token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if revoked, false if not found</returns>
    Task<bool> RevokeTokenAsync(string token, string revokedByIp, string? replacedByToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="revokedByIp">IP address that revoked the tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllUserTokensAsync(Guid userId, string revokedByIp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a refresh token
    /// </summary>
    /// <param name="id">The token ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired and revoked tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens cleaned up</returns>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}
