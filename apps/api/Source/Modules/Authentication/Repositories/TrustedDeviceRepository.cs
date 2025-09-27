using GameGuild.Database;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository implementation for trusted device data access operations
/// </summary>
public class TrustedDeviceRepository(ApplicationDbContext context) : ITrustedDeviceRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<TrustedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TrustedDevices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<TrustedDevice?> GetByUserAndFingerprintAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        return await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint, cancellationToken);
    }

    public async Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(Guid userId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TrustedDevice> query = _context.TrustedDevices
            .Where(d => d.UserId == userId);

        if (activeOnly)
            query = query.Where(d => d.IsValid);

        return await query
            .OrderByDescending(d => d.LastUsedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        return await _context.TrustedDevices
            .AnyAsync(d => d.UserId == userId &&
                          d.DeviceFingerprint == deviceFingerprint &&
                          d.IsValid, cancellationToken);
    }

    public async Task<TrustedDevice> CreateAsync(TrustedDevice trustedDevice, CancellationToken cancellationToken = default)
    {
        trustedDevice.Id = Guid.NewGuid();
        trustedDevice.CreatedAt = DateTime.UtcNow;
        trustedDevice.UpdatedAt = DateTime.UtcNow;
        trustedDevice.TrustedAt = DateTime.UtcNow;
        trustedDevice.LastUsedAt = DateTime.UtcNow;

        _context.TrustedDevices.Add(trustedDevice);
        await _context.SaveChangesAsync(cancellationToken);

        return trustedDevice;
    }

    public async Task<TrustedDevice> UpdateAsync(TrustedDevice trustedDevice, CancellationToken cancellationToken = default)
    {
        trustedDevice.UpdatedAt = DateTime.UtcNow;

        _context.TrustedDevices.Update(trustedDevice);
        await _context.SaveChangesAsync(cancellationToken);

        return trustedDevice;
    }

    public async Task<bool> UpdateLastUsedAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        TrustedDevice? device = await GetByUserAndFingerprintAsync(userId, deviceFingerprint, cancellationToken);
        if (device == null || !device.IsValid)
            return false;

        device.LastUsedAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(device, cancellationToken);
        return true;
    }

    public async Task<bool> RevokeTrustAsync(Guid id, CancellationToken cancellationToken = default)
    {
        TrustedDevice? device = await GetByIdAsync(id, cancellationToken);
        if (device == null)
            return false;

        device.IsActive = false;
        device.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(device, cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllUserDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<TrustedDevice> userDevices = await _context.TrustedDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync(cancellationToken);

        if (userDevices.Count == 0)
            return 0;

        DateTime now = DateTime.UtcNow;
        foreach (TrustedDevice device in userDevices)
        {
            device.IsActive = false;
            device.UpdatedAt = now;
        }

        _context.TrustedDevices.UpdateRange(userDevices);
        await _context.SaveChangesAsync(cancellationToken);

        return userDevices.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        TrustedDevice? device = await GetByIdAsync(id, cancellationToken);
        if (device == null)
            return false;

        _context.TrustedDevices.Remove(device);
        int changes = await _context.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }

    public async Task<int> CleanupExpiredDevicesAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        List<TrustedDevice> expiredDevices = await _context.TrustedDevices
            .Where(d => (d.ExpiresAt.HasValue && d.ExpiresAt.Value < now) ||
                       (!d.IsActive && d.UpdatedAt < now.AddDays(-90)))
            .ToListAsync(cancellationToken);

        if (expiredDevices.Count == 0)
            return 0;

        _context.TrustedDevices.RemoveRange(expiredDevices);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredDevices.Count;
    }
}