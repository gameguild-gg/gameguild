namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository for managing refresh tokens.
///     Handles token creation, validation, revocation, and cleanup.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    ///     Creates a new refresh token.
    /// </summary>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a refresh token by its token string.
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all refresh tokens for a specific user.
    /// </summary>
    Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active (non-revoked, non-expired) refresh tokens for a user.
    /// </summary>
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing refresh token (for revocation).
    /// </summary>
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes a specific refresh token.
    /// </summary>
    Task RevokeAsync(string token, string? revokedByIp = null, string? replacedByToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes all refresh tokens for a user (for security incidents or logout all sessions).
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, string? revokedByIp = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes expired and revoked tokens (cleanup task).
    /// </summary>
    Task DeleteExpiredAndRevokedAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
