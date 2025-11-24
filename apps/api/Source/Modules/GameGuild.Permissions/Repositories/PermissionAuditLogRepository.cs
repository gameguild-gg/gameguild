using GameGuild.Abstractions;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Permissions.Repositories;

/// <summary>
///     Repository implementation for PermissionAuditLog entities
/// </summary>
public class PermissionAuditLogRepository(IApplicationDbContext context) : IPermissionAuditLogRepository
{
    private DbSet<PermissionAuditLog> AuditLogs { get => context.Set<PermissionAuditLog>(); }

    public async Task<PermissionAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken); }

    public async Task<List<PermissionAuditLog>> GetByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId.HasValue ? new TenantId(tenantId.Value) : (TenantId?) null;

        return await AuditLogs.AsNoTracking().Where(a => a.TenantId == tenant).OrderByDescending(a => a.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<List<PermissionAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId.HasValue ? new TenantId(tenantId.Value) : (TenantId?) null;

        return await AuditLogs.AsNoTracking().Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate && a.TenantId == tenant).OrderByDescending(a => a.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<List<PermissionAuditLog>> GetByUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId.HasValue ? new TenantId(tenantId.Value) : (TenantId?) null;

        return await AuditLogs.AsNoTracking().Where(a => (a.UserId == userId || a.PerformedBy == userId) && a.TenantId == tenant).OrderByDescending(a => a.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<PermissionAuditLog> CreateAsync(PermissionAuditLog log, CancellationToken cancellationToken = default)
    {
        log.Timestamp = DateTime.UtcNow;

        AuditLogs.Add(log);
        await context.SaveChangesAsync(cancellationToken);

        return log;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await AuditLogs.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (log == null) return false;

        AuditLogs.Remove(log);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
