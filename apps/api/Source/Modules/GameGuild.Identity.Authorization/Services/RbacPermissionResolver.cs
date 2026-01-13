using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Repository for dynamic roles.
/// </summary>
public interface IDynamicRoleRepository
{
    Task<DynamicRole?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DynamicRole?> GetByNameAsync(string name, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<DynamicRole>> GetByTenantAsync(Guid? tenantId, bool includeGlobal = true, CancellationToken ct = default);
    Task<IReadOnlyList<DynamicRole>> GetActiveByTenantAsync(Guid? tenantId, bool includeGlobal = true, CancellationToken ct = default);
    Task<DynamicRole> CreateAsync(DynamicRole role, CancellationToken ct = default);
    Task UpdateAsync(DynamicRole role, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DynamicRole>> GetRoleHierarchyAsync(Guid roleId, CancellationToken ct = default);
}

/// <summary>
///     Repository for role assignments.
/// </summary>
public interface IDynamicRoleAssignmentRepository
{
    Task<IReadOnlyList<DynamicRoleAssignment>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<DynamicRoleAssignment>> GetValidByUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default);
    Task<DynamicRoleAssignment> CreateAsync(DynamicRoleAssignment assignment, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task<int> CountByRoleAsync(Guid roleId, CancellationToken ct = default);
}

/// <summary>
///     Database implementation of dynamic role repository.
/// </summary>
public class DynamicRoleRepository(IApplicationDbContext context) : IDynamicRoleRepository
{
    private DbSet<DynamicRole> DbSet => context.Set<DynamicRole>();

    public async Task<DynamicRole?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.Include(r => r.ParentRole).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<DynamicRole?> GetByNameAsync(string name, Guid? tenantId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(r => r.Name == name && r.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<DynamicRole>> GetByTenantAsync(Guid? tenantId, bool includeGlobal = true, CancellationToken ct = default)
    {
        var query = DbSet.AsQueryable();
        if (includeGlobal)
            query = query.Where(r => r.TenantId == tenantId || r.TenantId == null);
        else
            query = query.Where(r => r.TenantId == tenantId);
        return await query.Include(r => r.ParentRole).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DynamicRole>> GetActiveByTenantAsync(Guid? tenantId, bool includeGlobal = true, CancellationToken ct = default)
    {
        var query = DbSet.Where(r => r.IsActive);
        if (includeGlobal)
            query = query.Where(r => r.TenantId == tenantId || r.TenantId == null);
        else
            query = query.Where(r => r.TenantId == tenantId);
        return await query.Include(r => r.ParentRole).ToListAsync(ct);
    }

    public async Task<DynamicRole> CreateAsync(DynamicRole role, CancellationToken ct = default)
    {
        DbSet.Add(role);
        await context.SaveChangesAsync(ct);
        return role;
    }

    public async Task UpdateAsync(DynamicRole role, CancellationToken ct = default)
    {
        DbSet.Update(role);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await DbSet.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role != null)
        {
            DbSet.Remove(role);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<DynamicRole>> GetRoleHierarchyAsync(Guid roleId, CancellationToken ct = default)
    {
        var hierarchy = new List<DynamicRole>();
        var currentRole = await GetByIdAsync(roleId, ct);
        
        while (currentRole != null)
        {
            hierarchy.Add(currentRole);
            if (currentRole.ParentRoleId.HasValue)
            {
                currentRole = await GetByIdAsync(currentRole.ParentRoleId.Value, ct);
            }
            else
            {
                currentRole = null;
            }
            
            // Prevent infinite loops
            if (hierarchy.Count > 20) break;
        }
        
        return hierarchy;
    }
}

/// <summary>
///     Database implementation of role assignment repository.
/// </summary>
public class DynamicRoleAssignmentRepository(IApplicationDbContext context) : IDynamicRoleAssignmentRepository
{
    private DbSet<DynamicRoleAssignment> DbSet => context.Set<DynamicRoleAssignment>();

    public async Task<IReadOnlyList<DynamicRoleAssignment>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
        => await DbSet
            .Include(a => a.Role)
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DynamicRoleAssignment>> GetValidByUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Include(a => a.Role)
            .Where(a => a.UserId == userId && a.TenantId == tenantId && a.IsActive)
            .Where(a => !a.StartsAt.HasValue || a.StartsAt.Value <= now)
            .Where(a => !a.ExpiresAt.HasValue || a.ExpiresAt.Value > now)
            .ToListAsync(ct);
    }

    public async Task<DynamicRoleAssignment> CreateAsync(DynamicRoleAssignment assignment, CancellationToken ct = default)
    {
        DbSet.Add(assignment);
        await context.SaveChangesAsync(ct);
        return assignment;
    }

    public async Task DeleteAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var assignment = await DbSet.FirstOrDefaultAsync(a => a.UserId == userId && a.RoleId == roleId, ct);
        if (assignment != null)
        {
            DbSet.Remove(assignment);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CountByRoleAsync(Guid roleId, CancellationToken ct = default)
        => await DbSet.CountAsync(a => a.RoleId == roleId && a.IsActive, ct);
}

/// <summary>
///     Service for RBAC permission resolution.
/// </summary>
public interface IRbacPermissionResolver
{
    /// <summary>
    ///     Gets all permissions for a user based on their roles.
    /// </summary>
    Task<RbacResolutionResult> ResolvePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken ct = default);
}

/// <summary>
///     Result of RBAC permission resolution.
/// </summary>
public record RbacResolutionResult(
    IReadOnlySet<string> Permissions,
    IReadOnlyList<RoleContribution> RoleContributions);

/// <summary>
///     Implementation of RBAC permission resolver.
/// </summary>
public class RbacPermissionResolver(
    IDynamicRoleRepository roleRepository,
    IDynamicRoleAssignmentRepository assignmentRepository,
    ILogger<RbacPermissionResolver> logger
) : IRbacPermissionResolver
{
    public async Task<RbacResolutionResult> ResolvePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var allPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var contributions = new List<RoleContribution>();

        // Get valid role assignments for the user
        var assignments = await assignmentRepository.GetValidByUserAsync(userId, tenantId, ct);

        foreach (var assignment in assignments)
        {
            if (assignment.Role == null || !assignment.Role.IsActive) continue;

            // Get role hierarchy (current role + all parent roles)
            var hierarchy = await roleRepository.GetRoleHierarchyAsync(assignment.RoleId, ct);

            foreach (var role in hierarchy)
            {
                var isInherited = role.Id != assignment.RoleId;
                var rolePermissions = new List<string>();

                // Add static permissions for built-in roles
                var staticPerms = StaticRolePermissions.GetStaticPermissions(role.Name);
                rolePermissions.AddRange(staticPerms);

                // Add dynamic permissions from database
                rolePermissions.AddRange(role.Permissions);

                // Add to total permissions
                foreach (var perm in rolePermissions)
                {
                    allPermissions.Add(perm);
                }

                // Track contribution
                contributions.Add(new RoleContribution(
                    role.Id,
                    role.Name,
                    rolePermissions,
                    isInherited,
                    isInherited ? assignment.RoleId : null));

                logger.LogDebug(
                    "Resolved {Count} permissions from role {RoleName} (inherited: {IsInherited}) for user {UserId}",
                    rolePermissions.Count, role.Name, isInherited, userId);
            }
        }

        return new RbacResolutionResult(allPermissions, contributions);
    }
}
