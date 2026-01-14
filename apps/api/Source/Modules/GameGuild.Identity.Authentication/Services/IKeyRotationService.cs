namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for managing JWT signing key rotation
/// </summary>
public interface IKeyRotationService
{
    /// <summary>
    ///     Get the current active signing key
    /// </summary>
    Task<JwtSigningKey?> GetActiveSigningKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all valid keys for token validation (active + recently rotated)
    /// </summary>
    Task<List<JwtSigningKey>> GetValidationKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a specific key by KeyId (for token validation)
    /// </summary>
    Task<JwtSigningKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rotate to a new signing key
    /// </summary>
    /// <param name="reason">Reason for rotation (scheduled, compromised, manual)</param>
    /// <param name="validityDays">How long the new key should be valid (default 90 days)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created active key</returns>
    Task<JwtSigningKey> RotateKeyAsync(string reason = "scheduled", int validityDays = 90, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clean up expired keys (keys that are no longer valid for validation)
    /// </summary>
    /// <param name="retentionDays">Keep keys for this many days after expiry for audit purposes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of keys deleted</returns>
    Task<int> CleanupExpiredKeysAsync(int retentionDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initialize the key rotation system (create first key if none exists)
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
