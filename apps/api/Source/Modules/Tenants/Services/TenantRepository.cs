using GameGuild.Database;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Simple repository implementation for tenant CRUD operations
/// </summary>
public class TenantRepository(ApplicationDbContext context) : ITenantRepository
{
    public async Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await context.Tenants.Where(t => t.IsActive && !t.IsDeleted).AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

        return tenants.AsReadOnly();
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await context.Tenants.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken); }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await context.Tenants.FirstOrDefaultAsync(t => t.Slug.ToLower() == slug.ToLower() && !t.IsDeleted, cancellationToken);
    }

    public async Task<Tenant?> GetDefaultAsync(CancellationToken cancellationToken = default) { return await context.Tenants.FirstOrDefaultAsync(t => t.IsDefault && !t.IsDeleted, cancellationToken); }

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Tenants.Update(tenant);
        await context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(id, cancellationToken);

        if (tenant != null)
        {
            tenant.SoftDelete();
            await UpdateAsync(tenant, cancellationToken);
        }
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Tenants.Where(t => t.Slug.ToLower() == slug.ToLower() && !t.IsDeleted);

        if (excludeId.HasValue) { query = query.Where(t => t.Id != excludeId.Value); }

        return !await query.AnyAsync(cancellationToken);
    }

    // === TENANT SETTINGS OPERATIONS ===

    public async Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
    }

    public async Task<TenantSettings> CreateOrUpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default)
    {
        settings.TenantId = tenantId;
        settings.UpdatedAt = DateTime.UtcNow;

        var existingSettings = await context.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (existingSettings != null)
        {
            context.Entry(existingSettings).CurrentValues.SetValues(settings);
            context.TenantSettings.Update(existingSettings);
            await context.SaveChangesAsync(cancellationToken);

            return existingSettings;
        }
        else
        {
            settings.Id = Guid.NewGuid();
            settings.CreatedAt = DateTime.UtcNow;
            context.TenantSettings.Add(settings);
            await context.SaveChangesAsync(cancellationToken);

            return settings;
        }
    }

    // === TENANT DOMAINS OPERATIONS ===

    public async Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var domains = await context.TenantDomains.Where(d => d.TenantId == tenantId).ToListAsync(cancellationToken);

        return domains.AsReadOnly();
    }

    public async Task<TenantDomain> CreateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default)
    {
        context.TenantDomains.Add(tenantDomain);
        await context.SaveChangesAsync(cancellationToken);

        return tenantDomain;
    }

    public async Task<TenantDomain?> FindTenantDomainByMatchAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default)
    {
        var normalizedTopLevel = topLevelDomain.ToLowerInvariant();
        var normalizedSubdomain = subdomain?.ToLowerInvariant();

        return await context.TenantDomains.FirstOrDefaultAsync(d => d.TopLevelDomain == normalizedTopLevel && d.Subdomain == normalizedSubdomain, cancellationToken);
    }
}
