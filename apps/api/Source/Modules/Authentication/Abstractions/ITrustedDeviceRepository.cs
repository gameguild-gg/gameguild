namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository interface for trusted device data access operations
/// </summary>
public interface ITrustedDeviceRepository
{
    /// <summary>
    /// Get trusted device by ID
    /// </summary>
    /// <param name="id">The device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The trusted device or null if not found</returns>
    Task<TrustedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get trusted device by fingerprint and user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceFingerprint">The device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The trusted device or null if not found</returns>
    Task<TrustedDevice?> GetByUserAndFingerprintAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all trusted devices for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="activeOnly">Whether to return only active devices</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of trusted devices</returns>
    Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(Guid userId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a device is trusted for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceFingerprint">The device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if device is trusted and valid</returns>
    Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new trusted device
    /// </summary>
    /// <param name="trustedDevice">The trusted device to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created trusted device</returns>
    Task<TrustedDevice> CreateAsync(TrustedDevice trustedDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing trusted device
    /// </summary>
    /// <param name="trustedDevice">The trusted device to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated trusted device</returns>
    Task<TrustedDevice> UpdateAsync(TrustedDevice trustedDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last used time for a trusted device
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceFingerprint">The device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> UpdateLastUsedAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke trust for a device
    /// </summary>
    /// <param name="id">The device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if revoked, false if not found</returns>
    Task<bool> RevokeTrustAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke trust for all devices of a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of devices that had trust revoked</returns>
    Task<int> RevokeAllUserDevicesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a trusted device
    /// </summary>
    /// <param name="id">The device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired trusted devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of devices cleaned up</returns>
    Task<int> CleanupExpiredDevicesAsync(CancellationToken cancellationToken = default);
}
