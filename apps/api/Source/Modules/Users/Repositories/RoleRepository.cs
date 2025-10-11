using GameGuild.Database;
using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Repositories;

/// <summary>
///     Repository implementation for Role data access operations
/// </summary>
public class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.DeletedAt == null, cancellationToken);
    }

    public async Task<List<Role>> GetByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => roleIds.Contains(r.Id) && r.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name && r.DeletedAt == null, cancellationToken);
    }

    public async Task<List<Role>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.IsActive && r.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AnyAsync(r => r.Name == name && r.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var roleIdsList = roleIds.ToList();
        var existingCount = await _context.Roles
            .Where(r => roleIdsList.Contains(r.Id) && r.DeletedAt == null)
            .CountAsync(cancellationToken);

        return existingCount == roleIdsList.Count;
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(roleId, cancellationToken);
        if (role == null || role.IsSystemRole)
        {
            return;
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
