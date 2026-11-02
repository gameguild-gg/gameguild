namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository for the per-user seen-device registry (issue #1085). Presence of a
///     (user, fingerprint) row means the device already signed in before; its absence
///     marks a first-seen device.
/// </summary>
public interface IUserDeviceFingerprintRepository
{
    /// <summary>
    ///     Gets the seen-device record for a user and fingerprint, if the device signed
    ///     in before.
    /// </summary>
    Task<UserDeviceFingerprint?> GetByUserAndFingerprintAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records a first-seen device for the user.
    /// </summary>
    Task<UserDeviceFingerprint> CreateAsync(UserDeviceFingerprint fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the record's last-seen timestamp.
    /// </summary>
    Task<UserDeviceFingerprint> UpdateAsync(UserDeviceFingerprint fingerprint, CancellationToken cancellationToken = default);
}
