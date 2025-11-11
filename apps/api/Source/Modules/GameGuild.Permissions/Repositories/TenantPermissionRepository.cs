using GameGuild.Abstractions;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Permissions.Repositories;

/// <summary>
///     Repository implementation for TenantPermission entities
/// </summary>
public class TenantPermissionRepository(IApplicationDbContext context) : ITenantPermissionRepository
{
    private DbSet<TenantPermission> TenantPermissions { get => context.Set<TenantPermission>(); }

    public async Task<TenantPermission> CreateAsync(TenantPermission permission, CancellationToken cancellationToken = default)
    {
        permission.CreatedAt = DateTime.UtcNow;
        permission.UpdatedAt = DateTime.UtcNow;

        TenantPermissions.Add(permission);
        await context.SaveChangesAsync(cancellationToken);

        return permission;
    }

    public async Task<TenantPermission> UpdateAsync(TenantPermission permission, CancellationToken cancellationToken = default)
    {
        permission.UpdatedAt = DateTime.UtcNow;
        TenantPermissions.Update(permission);
        await context.SaveChangesAsync(cancellationToken);

        return permission;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await TenantPermissions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (permission == null) return false;

        TenantPermissions.Remove(permission);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<TenantPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await TenantPermissions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken); }

    public async Task<TenantPermission?> GetByUserAndTenantAsync(Guid? userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        return await TenantPermissions.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId, cancellationToken);
    }

    public async Task<List<TenantPermission>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await TenantPermissions.AsNoTracking().Where(p => p.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<List<TenantPermission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await TenantPermissions.AsNoTracking().Where(p => p.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<List<TenantPermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await TenantPermissions.AsNoTracking().Where(p => p.ExpiresAt.HasValue && p.ExpiresAt < DateTime.UtcNow).ToListAsync(cancellationToken);
    }
}
