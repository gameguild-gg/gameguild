using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Repository implementation for TenantSettings entity
/// </summary>
public class TenantSettingsRepository(IApplicationDbContext context) : ITenantSettingsRepository
{
    public async Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantSettings>().Include(ts => ts.Tenant).FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken cancellationToken = default)
    {
        var entity = context.Set<TenantSettings>().Add(settings);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.Entity;
    }

    public async Task<TenantSettings> UpdateAsync(TenantSettings settings, CancellationToken cancellationToken = default)
    {
        context.Set<TenantSettings>().Update(settings);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return settings;
    }

    public async Task DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (settings != null)
        {
            settings.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
