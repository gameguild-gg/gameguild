using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Repositories;

/// <summary>
///     Repository implementation for UserRole data access operations
/// </summary>
public class UserRoleRepository(ApplicationDbContext context) : IUserRoleRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<UserRole> AssignAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _context.UserRoles.AddAsync(userRole, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return userRole;
    }

    public async Task<List<UserRole>> AssignBulkAsync(IEnumerable<UserRole> userRoles, CancellationToken cancellationToken = default)
    {
        var userRolesList = userRoles.ToList();
        await _context.UserRoles.AddRangeAsync(userRolesList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return userRolesList;
    }

    public async Task UnassignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await GetAsync(userId, roleId, cancellationToken);
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId && ur.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserRole>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.User)
            .Where(ur => ur.RoleId == roleId && ur.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.DeletedAt == null, cancellationToken);
    }

    public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.DeletedAt == null, cancellationToken);
    }

    public async Task RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredRoles = await _context.UserRoles
            .Where(ur => ur.ExpiresAt.HasValue && ur.ExpiresAt.Value <= DateTime.UtcNow && ur.DeletedAt == null)
            .ToListAsync(cancellationToken);

        _context.UserRoles.RemoveRange(expiredRoles);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
