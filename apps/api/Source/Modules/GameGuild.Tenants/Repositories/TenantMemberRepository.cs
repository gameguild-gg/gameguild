using GameGuild.Abstractions;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tenants.Repositories;

/// <summary>
///     Repository implementation for TenantMember entity
/// </summary>
public class TenantMemberRepository(IApplicationDbContext context) : ITenantMemberRepository
{
    public async Task<TenantMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>().Include(tm => tm.Tenant).FirstOrDefaultAsync(tm => tm.Id == id && !tm.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantMember>> GetByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TenantMember>().Include(tm => tm.Tenant).Where(tm => tm.TenantId == tenantId && !tm.IsDeleted);

        if (!includeInactive) { query = query.Where(tm => tm.IsActive); }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantMember?> GetByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>().Include(tm => tm.Tenant).FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TenantId == tenantId && !tm.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantMember> CreateAsync(TenantMember member, CancellationToken cancellationToken = default)
    {
        var entity = context.Set<TenantMember>().Add(member);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.Entity;
    }

    public async Task<TenantMember> UpdateAsync(TenantMember member, CancellationToken cancellationToken = default)
    {
        context.Set<TenantMember>().Update(member);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return member;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (member != null)
        {
            member.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMember>().AnyAsync(tm => tm.UserId == userId && tm.TenantId == tenantId && !tm.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<TenantMember> Members, int TotalCount)> GetPagedAsync(Guid tenantId, int page, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TenantMember>().Include(tm => tm.Tenant).Where(tm => tm.TenantId == tenantId && !tm.IsDeleted);

        if (!includeInactive) { query = query.Where(tm => tm.IsActive); }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var members = await query.OrderBy(tm => tm.JoinedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (members, totalCount);
    }
}
