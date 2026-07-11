using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

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
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
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
            .ToListAsync(cancellationToken).ConfigureAwait(false);
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

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        role.Touch();

        Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        role.Touch();

        Roles.Update(role);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role != null)
        {
            Roles.Remove(role);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<Role>> GetUserRolesAsync(Guid userId, bool includeExpired = false, CancellationToken cancellationToken = default)
    {
        var query = UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId);

        if (!includeExpired)
        {
            var now = SystemClock.UtcNow;
            query = query.Where(ur => ur.ExpiresAt == null || ur.ExpiresAt > now);
        }

        return await query
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.Role!)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserRole> AssignRoleToUserAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        // Check if the role is already assigned to the user
        var existingUserRole = await UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userRole.UserId && ur.RoleId == userRole.RoleId, cancellationToken).ConfigureAwait(false);

        if (existingUserRole != null)
        {
            if (existingUserRole.IsExpired())
            {
                existingUserRole.AssignedBy = userRole.AssignedBy;
                existingUserRole.AssignedAt = SystemClock.UtcNow;
                existingUserRole.ExpiresAt = userRole.ExpiresAt;
                existingUserRole.Touch();
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return existingUserRole;
        }

        userRole.Touch();
        userRole.AssignedAt = SystemClock.UtcNow;

        UserRoles.Add(userRole);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return userRole;
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken).ConfigureAwait(false);

        if (userRole != null)
        {
            UserRoles.Remove(userRole);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        return await UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId 
                && ur.RoleId == roleId 
                && (ur.ExpiresAt == null || ur.ExpiresAt > now), 
                cancellationToken).ConfigureAwait(false);
    }
}
