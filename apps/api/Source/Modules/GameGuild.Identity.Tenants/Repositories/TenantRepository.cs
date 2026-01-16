using GameGuild.Abstractions;
using GameGuild.Models;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Repository implementation for Tenant entity
/// </summary>
public class TenantRepository(IApplicationDbContext context) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().FirstOrDefaultAsync(t => t.Slug == slug && t.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tenant>().Where(t => t.Slug == slug && t.DeletedAt == null);

        if (excludeId.HasValue) { query = query.Where(t => t.Id != excludeId.Value); }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().Where(t => t.IsActive && t.DeletedAt == null).OrderBy(t => t.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<Tenant>().Where(t => t.DeletedAt == null).OrderBy(t => t.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tenant>().Where(t => t.DeletedAt == null);

        if (isActive.HasValue) { query = query.Where(t => t.IsActive == isActive.Value); }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query.OrderBy(t => t.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Set<Tenant>().Update(tenant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tenant;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (tenant != null)
        {
            tenant.SoftDelete();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        tenant.SoftDelete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IQueryable<Tenant>> GetQueryableAsync(CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tenant>().Where(t => t.DeletedAt == null).AsQueryable();

        return Task.FromResult(query);
    }

    public async Task<PagedResult<TenantAuditLogEntry>> GetAuditLogAsync(
        Guid tenantId,
        DateTime? startDate,
        DateTime? endDate,
        string? action,
        Guid? actorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build query for audit entries
        // Note: This is a placeholder implementation. In production, you would query
        // an actual audit log table (e.g., TenantAuditLogs or use a centralized audit service)
        var query = context.Set<TenantAuditLog>()
            .Where(a => a.TenantId.HasValue && a.TenantId.Value == tenantId);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (actorId.HasValue)
        {
            query = query.Where(a => a.ActorId == actorId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Get paginated items
        var skip = (page - 1) * pageSize;
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .Select(a => new TenantAuditLogEntry
            {
                Id = a.Id,
                TenantId = a.TenantId!.Value,
                Timestamp = a.Timestamp,
                Action = a.Action,
                ActorId = a.ActorId,
                ActorName = a.ActorName,
                ActorEmail = a.ActorEmail,
                BeforeValues = a.BeforeValues ?? new Dictionary<string, object?>(),
                AfterValues = a.AfterValues ?? new Dictionary<string, object?>(),
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CorrelationId = a.CorrelationId,
                Metadata = a.Metadata ?? new Dictionary<string, string>()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TenantAuditLogEntry>(items, totalCount, skip, pageSize);
    }
}
