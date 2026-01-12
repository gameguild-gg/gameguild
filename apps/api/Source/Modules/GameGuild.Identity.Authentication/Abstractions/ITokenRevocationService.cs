namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for managing JWT token revocation.
///     Tracks revoked token IDs (JTI) to enable immediate token invalidation.
/// </summary>
/// <remarks>
///     <para>
///         <b>Design Note:</b> This interface is designed to be Redis-ready.
///         The default implementation uses in-memory storage, but can be replaced
///         with a Redis implementation for distributed scenarios.
///     </para>
///     <para>
///         <b>Usage:</b> Call <see cref="RevokeTokenAsync"/> when a user logs out
///         or when immediate token invalidation is required. Call <see cref="IsRevokedAsync"/>
///         in the authentication pipeline to reject revoked tokens.
///     </para>
/// </remarks>
public interface ITokenRevocationService
{
    /// <summary>
    ///     Revokes a token by its JTI (JWT ID).
    /// </summary>
    /// <param name="jti">The unique JWT ID to revoke</param>
    /// <param name="expiresAt">When the token naturally expires (for cleanup)</param>
    /// <param name="reason">Optional reason for revocation (for audit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeTokenAsync(string jti, DateTime expiresAt, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes all tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user whose tokens should be revoked</param>
    /// <param name="reason">Optional reason for revocation (for audit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserTokensAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a token has been revoked.
    /// </summary>
    /// <param name="jti">The JWT ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the token is revoked, false otherwise</returns>
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if all tokens for a user have been revoked.
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="tokenIssuedAt">When the token was issued</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user tokens issued before the revocation time are revoked</returns>
    Task<bool> IsUserTokenRevokedAsync(Guid userId, DateTime tokenIssuedAt, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cleans up expired revocation entries to prevent unbounded growth.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entries cleaned up</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
