using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for managing JWT signing key rotation with automatic expiry and cleanup
/// </summary>
public class KeyRotationService : IKeyRotationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<KeyRotationService> _logger;

    public KeyRotationService(
        IApplicationDbContext dbContext,
        ILogger<KeyRotationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<JwtSigningKey?> GetActiveSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<JwtSigningKey>()
            .Where(k => k.IsActive && k.ValidFrom <= DateTime.UtcNow && k.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(k => k.KeyVersion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<JwtSigningKey>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Set<JwtSigningKey>()
            .Where(k => k.ValidFrom <= now && k.ExpiresAt > now)
            .OrderByDescending(k => k.KeyVersion)
            .ToListAsync(cancellationToken);
    }

    public async Task<JwtSigningKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<JwtSigningKey>()
            .FirstOrDefaultAsync(k => k.KeyId == keyId, cancellationToken);
    }

    public async Task<JwtSigningKey> RotateKeyAsync(string reason = "scheduled", int validityDays = 90, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting JWT key rotation. Reason: {Reason}", reason);

        // Get current active key
        var currentKey = await GetActiveSigningKeyAsync(cancellationToken);
        var nextVersion = (currentKey?.KeyVersion ?? 0) + 1;

        // Create new key
        var validFrom = DateTime.UtcNow;
        var validity = TimeSpan.FromDays(validityDays);
        var newKey = JwtSigningKey.CreateNew(nextVersion, validFrom, validity);

        // Save new key
        _dbContext.Set<JwtSigningKey>().Add(newKey);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Activate new key
        newKey.Activate();

        // Rotate out old key (but keep it valid for token verification during overlap period)
        if (currentKey != null)
        {
            currentKey.Rotate(reason);
            _logger.LogInformation(
                "Rotated key {OldKeyId} (version {OldVersion}) to {NewKeyId} (version {NewVersion})",
                currentKey.KeyId, currentKey.KeyVersion, newKey.KeyId, newKey.KeyVersion);
        }
        else
        {
            _logger.LogInformation("Created initial signing key {KeyId} (version {Version})",
                newKey.KeyId, newKey.KeyVersion);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return newKey;
    }

    public async Task<int> CleanupExpiredKeysAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        
        var expiredKeys = await _dbContext.Set<JwtSigningKey>()
            .Where(k => k.ExpiresAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (expiredKeys.Count == 0)
        {
            _logger.LogDebug("No expired keys to clean up");
            return 0;
        }

        _dbContext.Set<JwtSigningKey>().RemoveRange(expiredKeys);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} expired JWT signing keys older than {CutoffDate}",
            expiredKeys.Count, cutoffDate);

        return expiredKeys.Count;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var activeKey = await GetActiveSigningKeyAsync(cancellationToken);
        if (activeKey == null)
        {
            _logger.LogWarning("No active JWT signing key found. Creating initial key...");
            await RotateKeyAsync("initialization", validityDays: 90, cancellationToken);
        }
        else
        {
            _logger.LogInformation("JWT signing key initialized. Active key: {KeyId}, expires: {ExpiresAt}",
                activeKey.KeyId, activeKey.ExpiresAt);
        }
    }
}
