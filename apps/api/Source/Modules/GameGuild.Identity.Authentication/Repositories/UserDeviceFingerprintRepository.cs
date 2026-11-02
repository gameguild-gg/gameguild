using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for the per-user seen-device registry (issue #1085).
/// </summary>
public class UserDeviceFingerprintRepository(IApplicationDbContext context) : IUserDeviceFingerprintRepository
{
    private DbSet<UserDeviceFingerprint> Fingerprints { get => context.Set<UserDeviceFingerprint>(); }

    public async Task<UserDeviceFingerprint?> GetByUserAndFingerprintAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        return await Fingerprints.FirstOrDefaultAsync(
            record => record.UserId == userId && record.DeviceFingerprint == deviceFingerprint,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserDeviceFingerprint> CreateAsync(UserDeviceFingerprint fingerprint, CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        fingerprint.Id = Guid.NewGuid();
        fingerprint.FirstSeenAt = now;
        fingerprint.LastSeenAt = now;
        fingerprint.CreatedAt = now;
        fingerprint.UpdatedAt = now;

        Fingerprints.Add(fingerprint);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return fingerprint;
    }

    public async Task<UserDeviceFingerprint> UpdateAsync(UserDeviceFingerprint fingerprint, CancellationToken cancellationToken = default)
    {
        fingerprint.LastSeenAt = SystemClock.UtcNow;
        fingerprint.UpdatedAt = SystemClock.UtcNow;

        Fingerprints.Update(fingerprint);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return fingerprint;
    }
}
