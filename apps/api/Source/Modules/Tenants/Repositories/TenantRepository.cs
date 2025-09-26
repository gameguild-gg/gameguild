using GameGuild.Database;
using GameGuild.Modules.Localization;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Simple repository implementation for tenant CRUD operations.
/// </summary>
public class TenantRepository(ApplicationDbContext context, ILanguageRepository? languageRepository = null) : ITenantRepository
{
    private readonly ApplicationDbContext _context = context;

    private readonly ILanguageRepository _languageRepository = languageRepository ?? new LanguageRepository(context);

    public async Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        List<Tenant> tenants = await _context.Tenants.Where(tenant => tenant.IsActive && !tenant.IsDeleted).AsNoTracking().OrderBy(tenant => tenant.Name).ToListAsync(cancellationToken);

        return tenants.AsReadOnly();
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants.FirstOrDefaultAsync(tenant => tenant.Id == id && !tenant.IsDeleted, cancellationToken);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = slug.ToLowerInvariant();

        return await _context.Tenants.FirstOrDefaultAsync(tenant => tenant.Slug.ToLower() == normalizedSlug && !tenant.IsDeleted, cancellationToken);
    }

    public async Task<Tenant?> GetDefaultAsync(CancellationToken cancellationToken = default) { return await _context.Tenants.FirstOrDefaultAsync(tenant => tenant.IsDefault && !tenant.IsDeleted, cancellationToken); }

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _ = _context.Tenants.Add(tenant);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _ = _context.Tenants.Update(tenant);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await GetByIdAsync(id, cancellationToken);

        if (tenant == null) { return; }

        tenant.SoftDelete();
        _ = await UpdateAsync(tenant, cancellationToken);
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = slug.ToLowerInvariant();

        IQueryable<Tenant> query = _context.Tenants.Where(tenant => tenant.Slug.ToLower() == normalizedSlug && !tenant.IsDeleted);

        if (excludeId.HasValue) { query = query.Where(tenant => tenant.Id != excludeId.Value); }

        return !await query.AnyAsync(cancellationToken);
    }

    // === TENANT SETTINGS OPERATIONS ===

    public async Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantSettings.FirstOrDefaultAsync(settings => settings.TenantId == tenantId, cancellationToken);
    }

    public async Task<TenantSettings> CreateOrUpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default)
    {
        settings.TenantId = tenantId;
        settings.UpdatedAt = DateTime.UtcNow;

        if (settings.DefaultLanguageId == Guid.Empty)
        {
            Language? defaultLanguage = await _languageRepository.GetDefaultAsync(cancellationToken);

            if (defaultLanguage != null) { settings.DefaultLanguageId = defaultLanguage.Id; }
        }

        TenantSettings? existingSettings = await _context.TenantSettings.FirstOrDefaultAsync(entry => entry.TenantId == tenantId, cancellationToken);

        if (existingSettings != null)
        {
            _context.Entry(existingSettings).CurrentValues.SetValues(settings);
            _ = _context.TenantSettings.Update(existingSettings);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return existingSettings;
        }

        settings.Id = Guid.NewGuid();
        settings.CreatedAt = DateTime.UtcNow;
        _ = _context.TenantSettings.Add(settings);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return settings;
    }

    // === TENANT DOMAINS OPERATIONS ===

    public async Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        List<TenantDomain> domains = await _context.TenantDomains.Where(domain => domain.TenantId == tenantId).ToListAsync(cancellationToken);

        return domains.AsReadOnly();
    }

    public async Task<TenantDomain> CreateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default)
    {
        _ = _context.TenantDomains.Add(tenantDomain);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return tenantDomain;
    }

    public async Task<TenantDomain?> FindTenantDomainByMatchAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default)
    {
        string normalizedTopLevel = topLevelDomain.ToLowerInvariant();
        string? normalizedSubdomain = subdomain?.ToLowerInvariant();

        return await _context.TenantDomains.FirstOrDefaultAsync(domain => domain.TopLevelDomain == normalizedTopLevel && domain.Subdomain == normalizedSubdomain, cancellationToken);
    }
}
