using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Repository implementation for TenantDomain entity
/// </summary>
public class TenantDomainsRepository(IApplicationDbContext context) : ITenantDomainsRepository
{
    public async Task<TenantDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantDomain>().Include(td => td.Tenant).FirstOrDefaultAsync(td => td.Id == id && td.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantDomain>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantDomain>()
            .Include(td => td.Tenant)
            .Where(td => td.TenantId == tenantId && td.DeletedAt == null)
            .OrderByDescending(td => td.IsMainDomain)
            .ThenBy(td => td.TopLevelDomain)
            .ThenBy(td => td.Subdomain)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantDomain?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        // Split domain into subdomain and top-level parts
        var parts = domain.Split('.', 2);

        if (parts.Length == 1)
        {
            // No subdomain
            return await context.Set<TenantDomain>().Include(td => td.Tenant).FirstOrDefaultAsync(td => td.TopLevelDomain == domain && td.Subdomain == null && td.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        }

        // Has subdomain
        var subdomain = parts[0];
        var topLevel = parts[1];

        return await context.Set<TenantDomain>().Include(td => td.Tenant).FirstOrDefaultAsync(td => td.TopLevelDomain == topLevel && td.Subdomain == subdomain && td.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantDomain?> GetMainDomainAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantDomain>().Include(td => td.Tenant).FirstOrDefaultAsync(td => td.TenantId == tenantId && td.IsMainDomain && td.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DomainExistsAsync(string topLevelDomain, string? subdomain = null, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TenantDomain>().Where(td => td.TopLevelDomain == topLevelDomain && td.Subdomain == subdomain && td.DeletedAt == null);

        if (excludeId.HasValue) { query = query.Where(td => td.Id != excludeId.Value); }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantDomain> CreateAsync(TenantDomain domain, CancellationToken cancellationToken = default)
    {
        var entity = context.Set<TenantDomain>().Add(domain);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.Entity;
    }

    public async Task<TenantDomain> UpdateAsync(TenantDomain domain, CancellationToken cancellationToken = default)
    {
        context.Set<TenantDomain>().Update(domain);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return domain;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var domain = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (domain != null)
        {
            domain.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> IsDomainUniqueAsync(string domain, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        // Split domain into subdomain and top-level parts
        var parts = domain.Split('.', 2);
        IQueryable<TenantDomain> query;

        if (parts.Length == 1)
        {
            // No subdomain
            query = context.Set<TenantDomain>().Where(td => td.TopLevelDomain == domain && td.Subdomain == null && td.DeletedAt == null);
        }
        else
        {
            // Has subdomain
            var subdomain = parts[0];
            var topLevel = parts[1];
            query = context.Set<TenantDomain>().Where(td => td.TopLevelDomain == topLevel && td.Subdomain == subdomain && td.DeletedAt == null);
        }

        if (excludeId.HasValue) { query = query.Where(td => td.Id != excludeId.Value); }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }
}
