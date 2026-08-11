using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Repository implementation for TenantMember entity
/// </summary>
public class TenantMemberRepository(IApplicationDbContext context) : ITenantMemberRepository
{
    public async Task<TenantMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>().Include(tm => tm.Tenant).FirstOrDefaultAsync(tm => tm.Id == id && tm.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantMember>> GetByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TenantMember>().Include(tm => tm.Tenant).Where(tm => tm.TenantId == tenantId && tm.DeletedAt == null);

        if (!includeInactive) { query = query.Where(tm => tm.IsActive); }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantMember?> GetByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>().Include(tm => tm.Tenant).FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TenantId == tenantId && tm.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantMember?> GetByUserAndTenantIncludingDeletedAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>()
            .IgnoreQueryFilters()
            .Include(tm => tm.Tenant)
            .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantMember> CreateAsync(TenantMember member, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultMembershipRemainsActiveAsync(member, cancellationToken).ConfigureAwait(false);
        var entity = context.Set<TenantMember>().Add(member);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.Entity;
    }

    public async Task<TenantMember> UpdateAsync(TenantMember member, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultMembershipRemainsActiveAsync(member, cancellationToken).ConfigureAwait(false);
        context.Set<TenantMember>().Update(member);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return member;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (member == null)
        {
            return;
        }

        if (await IsDefaultTenantMembershipAsync(member, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("The default tenant membership cannot be removed.");

        member.SoftDelete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>().AnyAsync(tm => tm.UserId == userId && tm.TenantId == tenantId && tm.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantMember>> GetByUserIdAsync(Guid userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TenantMember>()
            .Include(tm => tm.Tenant)
            .Where(tm => tm.UserId == userId && tm.DeletedAt == null);

        if (!includeInactive)
        {
            query = query.Where(tm => tm.IsActive);
        }

        return await query.OrderBy(tm => tm.JoinedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<TenantMember> Members, int TotalCount)> GetPagedAsync(Guid tenantId, int page, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TenantMember>().Include(tm => tm.Tenant).Where(tm => tm.TenantId == tenantId && tm.DeletedAt == null);

        if (!includeInactive) { query = query.Where(tm => tm.IsActive); }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var members = await query.OrderBy(tm => tm.JoinedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (members, totalCount);
    }

    private async Task EnsureDefaultMembershipRemainsActiveAsync(TenantMember member, CancellationToken cancellationToken)
    {
        if (member.IsActive && member.DeletedAt is null)
        {
            return;
        }

        if (await IsDefaultTenantMembershipAsync(member, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("The default tenant membership must remain active.");
    }

    private async Task<bool> IsDefaultTenantMembershipAsync(TenantMember member, CancellationToken cancellationToken)
    {
        if (member.Tenant?.IsDefault == true)
        {
            return true;
        }

        return await context.Set<Tenant>()
            .AsNoTracking()
            .AnyAsync(tenant => tenant.Id == member.TenantId && tenant.IsDefault && tenant.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }
}
