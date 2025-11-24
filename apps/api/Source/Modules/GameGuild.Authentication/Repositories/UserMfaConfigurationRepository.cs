using GameGuild.Abstractions;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Authentication.Repositories;

/// <summary>
///     Repository implementation for user MFA configuration data access operations
/// </summary>
public class UserMfaConfigurationRepository(IApplicationDbContext context) : IUserMfaConfigurationRepository
{
    private DbSet<UserMfaConfiguration> UserMfaConfigurations { get => context.Set<UserMfaConfiguration>(); }

    public async Task<UserMfaConfiguration?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await UserMfaConfigurations.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await UserMfaConfigurations.AnyAsync(c => c.UserId == userId && c.IsEnabled && c.IsSetupComplete, cancellationToken);
    }

    public async Task<UserMfaConfiguration> CreateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        configuration.Id = Guid.NewGuid();
        configuration.CreatedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;

        UserMfaConfigurations.Add(configuration);
        await context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    public async Task<UserMfaConfiguration> UpdateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        configuration.UpdatedAt = DateTime.UtcNow;

        UserMfaConfigurations.Update(configuration);
        await context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration != null)
        {
            UserMfaConfigurations.Remove(configuration);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MfaMethod?> GetPreferredMethodAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        return configuration?.PreferredMethod;
    }

    public async Task IncrementFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration != null)
        {
            configuration.FailedAttempts++;
            configuration.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(configuration, cancellationToken);
        }
    }

    public async Task ResetFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration != null)
        {
            configuration.FailedAttempts = 0;
            configuration.LastUsedAt = DateTime.UtcNow;
            configuration.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(configuration, cancellationToken);
        }
    }

    public async Task SetLockoutAsync(Guid userId, DateTime lockoutUntil, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration != null)
        {
            configuration.LockedOutUntil = lockoutUntil;
            configuration.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(configuration, cancellationToken);
        }
    }

    public async Task<UserMfaConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await UserMfaConfigurations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken); }

    public async Task<IReadOnlyList<UserMfaConfiguration>> GetEnabledConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await UserMfaConfigurations.Where(c => c.IsEnabled && c.IsSetupComplete).ToListAsync(cancellationToken);
    }

    public async Task<bool> EnableMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration == null) return false;

        configuration.IsEnabled = true;
        configuration.EnabledAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(configuration, cancellationToken);

        return true;
    }

    public async Task<bool> DisableMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration == null) return false;

        configuration.IsEnabled = false;
        configuration.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(configuration, cancellationToken);

        return true;
    }

    public async Task<int> UpdateFailedAttemptsAsync(Guid userId, bool increment, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration == null) return 0;

        if (increment) { configuration.FailedAttempts++; }
        else
        {
            configuration.FailedAttempts = 0;
            configuration.LastUsedAt = DateTime.UtcNow;
        }

        configuration.UpdatedAt = DateTime.UtcNow;
        await UpdateAsync(configuration, cancellationToken);

        return configuration.FailedAttempts;
    }

    public async Task<bool> LockoutUserAsync(Guid userId, DateTime lockoutUntil, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration == null) return false;

        configuration.LockedOutUntil = lockoutUntil;
        configuration.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(configuration, cancellationToken);

        return true;
    }

    public async Task<bool> ClearLockoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByUserIdAsync(userId, cancellationToken);

        if (configuration == null) return false;

        configuration.LockedOutUntil = null;
        configuration.FailedAttempts = 0;
        configuration.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(configuration, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByIdAsync(id, cancellationToken);

        if (configuration == null) return false;

        UserMfaConfigurations.Remove(configuration);
        var changes = await context.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }
}
