using GameGuild.Abstractions;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Authentication.Repositories;

/// <summary>
///     Repository implementation for Role entities
/// </summary>
public class RoleRepository(IApplicationDbContext context) : IRoleRepository
{
    private DbSet<Role> Roles => context.Set<Role>();
    private DbSet<UserRole> UserRoles => context.Set<UserRole>();

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Role>> GetAllAsync(Guid? tenantId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = Roles.AsNoTracking();

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = Roles.AsNoTracking();

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.Name.ToLower() == name.ToLower() && r.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(r => r.Name.ToLower() == name.ToLower() && r.TenantId == null);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        role.CreatedAt = DateTime.UtcNow;
        role.UpdatedAt = DateTime.UtcNow;

        Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        role.UpdatedAt = DateTime.UtcNow;

        Roles.Update(role);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role != null)
        {
            Roles.Remove(role);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? tenantId = null, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var query = Roles.AsNoTracking();

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.Name.ToLower() == name.ToLower() && r.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(r => r.Name.ToLower() == name.ToLower() && r.TenantId == null);
        }

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<List<Role>> GetUserRolesAsync(Guid userId, bool includeExpired = false, CancellationToken cancellationToken = default)
    {
        var query = UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId);

        if (!includeExpired)
        {
            var now = DateTime.UtcNow;
            query = query.Where(ur => ur.ExpiresAt == null || ur.ExpiresAt > now);
        }

        return await query
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.Role!)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRole> AssignRoleToUserAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        userRole.CreatedAt = DateTime.UtcNow;
        userRole.UpdatedAt = DateTime.UtcNow;
        userRole.AssignedAt = DateTime.UtcNow;

        UserRoles.Add(userRole);
        await context.SaveChangesAsync(cancellationToken);

        return userRole;
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole != null)
        {
            UserRoles.Remove(userRole);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId 
                && ur.RoleId == roleId 
                && (ur.ExpiresAt == null || ur.ExpiresAt > now), 
                cancellationToken);
    }
}
