using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
///     Repository implementation for tenant member data access operations
/// </summary>
public sealed class TenantMemberRepository(ApplicationDbContext dbContext) : ITenantMemberRepository
{
    public async Task<IReadOnlyList<TenantMember>> GetMembersByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMember>> GetTenantsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .Where(m => m.UserId == userId)
            .Include(m => m.Tenant)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantMember?> GetMemberAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> IsMemberOfTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive, cancellationToken);
    }

    public async Task<string?> GetMemberRoleAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Set<TenantMember>()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId, cancellationToken);

        return member?.Role;
    }

    public async Task<TenantMember> AddMemberAsync(
        TenantMember member,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TenantMember>().AddAsync(member, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task<TenantMember> UpdateMemberAsync(
        TenantMember member,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<TenantMember>().Update(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task RemoveMemberAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var member = await GetMemberAsync(userId, tenantId, cancellationToken);
        if (member != null)
        {
            dbContext.Set<TenantMember>().Remove(member);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<TenantMember>> GetActiveMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .Where(m => m.TenantId == tenantId && m.IsActive)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMember>> GetMembersByRoleAsync(
        Guid tenantId,
        string role,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .Where(m => m.TenantId == tenantId && m.Role == role)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMember>> GetChildMembersAsync(
        Guid parentMemberId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .Where(m => m.ParentMemberId == parentMemberId)
            .Include(m => m.ChildMembers)
            .OrderBy(m => m.HierarchyLevel)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMember>> GetMemberHierarchyAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Set<TenantMember>()
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);

        if (member == null || string.IsNullOrEmpty(member.HierarchyPath))
            return Array.Empty<TenantMember>();

        // Get all members whose HierarchyPath starts with this member's path
        var hierarchyPathPrefix = $"{member.HierarchyPath}/";
        return await dbContext.Set<TenantMember>()
            .Where(m => m.HierarchyPath != null && m.HierarchyPath.StartsWith(hierarchyPathPrefix))
            .OrderBy(m => m.HierarchyLevel)
            .ThenBy(m => m.HierarchyPath)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMember>> GetRootMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TenantMember>()
            .Where(m => m.TenantId == tenantId && m.ParentMemberId == null)
            .Include(m => m.ChildMembers)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateHierarchyPathAsync(
        Guid memberId,
        string hierarchyPath,
        CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Set<TenantMember>()
            .Include(m => m.ChildMembers)
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);

        if (member == null)
            return;

        var oldPath = member.HierarchyPath;
        member.HierarchyPath = hierarchyPath;
        member.HierarchyLevel = string.IsNullOrEmpty(hierarchyPath) ? 0 : hierarchyPath.Split('/').Length;

        // Update all descendants' hierarchy paths
        if (!string.IsNullOrEmpty(oldPath))
        {
            var oldPathPrefix = $"{oldPath}/";
            var newPathPrefix = $"{hierarchyPath}/";

            var descendants = await dbContext.Set<TenantMember>()
                .Where(m => m.HierarchyPath != null && m.HierarchyPath.StartsWith(oldPathPrefix))
                .ToListAsync(cancellationToken);

            foreach (var descendant in descendants)
            {
                if (descendant.HierarchyPath != null)
                {
                    descendant.HierarchyPath = descendant.HierarchyPath.Replace(oldPathPrefix, newPathPrefix);
                    descendant.HierarchyLevel = descendant.HierarchyPath.Split('/').Length;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
