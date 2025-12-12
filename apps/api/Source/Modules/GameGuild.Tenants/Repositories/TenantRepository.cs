using GameGuild.Abstractions;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tenants.Repositories;

/// <summary>
///     Repository implementation for Tenant entity
/// </summary>
public class TenantRepository(IApplicationDbContext context) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tenant>().Where(t => t.Slug == slug && !t.IsDeleted);

        if (excludeId.HasValue) { query = query.Where(t => t.Id != excludeId.Value); }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().Where(t => t.IsActive && !t.IsDeleted).OrderBy(t => t.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().Where(t => !t.IsDeleted).OrderBy(t => t.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tenant>().Where(t => !t.IsDeleted);

        if (isActive.HasValue) { query = query.Where(t => t.IsActive == isActive.Value); }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query.OrderBy(t => t.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Set<Tenant>().Update(tenant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tenant;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (tenant != null)
        {
            tenant.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant != null)
        {
            tenant.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<IQueryable<Tenant>> GetQueryableAsync(CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tenant>().Where(t => !t.IsDeleted).AsQueryable();

        return Task.FromResult(query);
    }
}
