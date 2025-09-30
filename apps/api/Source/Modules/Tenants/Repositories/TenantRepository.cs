using GameGuild.Database;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Simple repository implementation for tenant CRUD operations.
/// </summary>
public class TenantRepository(ApplicationDbContext context) : ITenantRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _context.Tenants.Where(tenant => tenant.IsActive && tenant.DeletedAt == null).AsNoTracking().OrderBy(tenant => tenant.Name).ToListAsync(cancellationToken);

        return tenants.AsReadOnly();
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants.FirstOrDefaultAsync(tenant => tenant.Id == id && tenant.DeletedAt == null, cancellationToken);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = slug.ToLowerInvariant();

        return await _context.Tenants.FirstOrDefaultAsync(tenant => tenant.Slug.ToLower() == normalizedSlug && tenant.DeletedAt == null, cancellationToken);
    }

    public async Task<Tenant?> GetDefaultAsync(CancellationToken cancellationToken = default) { return await _context.Tenants.FirstOrDefaultAsync(tenant => tenant.IsDefault && tenant.DeletedAt == null, cancellationToken); }

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

        var query = _context.Tenants.Where(tenant => tenant.Slug.ToLower() == normalizedSlug && tenant.DeletedAt == null);

        if (excludeId.HasValue) { query = query.Where(tenant => tenant.Id != excludeId.Value); }

        return !await query.AnyAsync(cancellationToken);
    }
}
