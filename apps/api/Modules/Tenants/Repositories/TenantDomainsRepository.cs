using GameGuild.Database;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Repository implementation for tenant domains data access operations
///     Follows hexagonal architecture principles as an adapter (implementation)
/// </summary>
public class TenantDomainsRepository(ApplicationDbContext context) : ITenantDomainsRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var domains = await _context.TenantDomains.Where(domain => domain.TenantId == tenantId).AsNoTracking().OrderBy(domain => domain.TopLevelDomain).ThenBy(domain => domain.Subdomain).ToListAsync(cancellationToken);

        return domains.AsReadOnly();
    }

    public async Task<TenantDomain?> GetTenantDomainByIdAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantDomains.FirstOrDefaultAsync(domain => domain.Id == domainId, cancellationToken);
    }

    public async Task<TenantDomain> CreateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default)
    {
        // Normalize domain values
        tenantDomain.TopLevelDomain = tenantDomain.TopLevelDomain.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(tenantDomain.Subdomain)) { tenantDomain.Subdomain = tenantDomain.Subdomain.ToLowerInvariant(); }

        tenantDomain.Id = Guid.NewGuid();
        tenantDomain.CreatedAt = DateTime.UtcNow;
        tenantDomain.UpdatedAt = DateTime.UtcNow;

        _ = _context.TenantDomains.Add(tenantDomain);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return tenantDomain;
    }

    public async Task<TenantDomain> UpdateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default)
    {
        // Normalize domain values
        tenantDomain.TopLevelDomain = tenantDomain.TopLevelDomain.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(tenantDomain.Subdomain)) { tenantDomain.Subdomain = tenantDomain.Subdomain.ToLowerInvariant(); }

        tenantDomain.UpdatedAt = DateTime.UtcNow;

        _ = _context.TenantDomains.Update(tenantDomain);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return tenantDomain;
    }

    public async Task<bool> DeleteTenantDomainAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        TenantDomain? domain = await GetTenantDomainByIdAsync(domainId, cancellationToken);

        if (domain == null) { return false; }

        _ = _context.TenantDomains.Remove(domain);
        int changesCount = await _context.SaveChangesAsync(cancellationToken);

        return changesCount > 0;
    }

    public async Task<TenantDomain?> FindTenantDomainByMatchAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default)
    {
        string normalizedTopLevel = topLevelDomain.ToLowerInvariant();
        string? normalizedSubdomain = subdomain?.ToLowerInvariant();

        return await _context.TenantDomains.FirstOrDefaultAsync(domain => domain.TopLevelDomain == normalizedTopLevel && domain.Subdomain == normalizedSubdomain, cancellationToken);
    }

    public async Task<bool> IsDomainAvailableAsync(string topLevelDomain, string? subdomain = null, Guid? excludeDomainId = null, CancellationToken cancellationToken = default)
    {
        string normalizedTopLevel = topLevelDomain.ToLowerInvariant();
        string? normalizedSubdomain = subdomain?.ToLowerInvariant();

        var query = _context.TenantDomains.Where(domain => domain.TopLevelDomain == normalizedTopLevel && domain.Subdomain == normalizedSubdomain);

        if (excludeDomainId.HasValue) { query = query.Where(domain => domain.Id != excludeDomainId.Value); }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantDomain>> GetAllTenantDomainsAsync(CancellationToken cancellationToken = default)
    {
        var domains = await _context.TenantDomains.AsNoTracking().OrderBy(d => d.TopLevelDomain).ThenBy(d => d.Subdomain).ToListAsync(cancellationToken);

        return domains.AsReadOnly();
    }
}
