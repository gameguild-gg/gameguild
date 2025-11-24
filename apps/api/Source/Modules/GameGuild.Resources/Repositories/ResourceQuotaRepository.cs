using GameGuild.Abstractions;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources.Repositories;

/// <summary>
///     Repository implementation for ResourceQuota entity
/// </summary>
public class ResourceQuotaRepository(IApplicationDbContext context) : IResourceQuotaRepository
{
    private DbSet<ResourceQuota> ResourceQuotas { get => context.Set<ResourceQuota>(); }

    public async Task<ResourceQuota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await ResourceQuotas.FirstOrDefaultAsync(q => q.Id == id, cancellationToken); }

    public async Task<IEnumerable<ResourceQuota>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await ResourceQuotas.Where(q => q.TenantId!.Value == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<ResourceQuota?> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await ResourceQuotas.FirstOrDefaultAsync(q => q.TenantId!.Value == tenantId && q.Type == type, cancellationToken);
    }

    public async Task<ResourceQuota> CreateAsync(ResourceQuota quota, CancellationToken cancellationToken = default)
    {
        ResourceQuotas.Add(quota);
        await context.SaveChangesAsync(cancellationToken);

        return quota;
    }

    public async Task<ResourceQuota> UpdateAsync(ResourceQuota quota, CancellationToken cancellationToken = default)
    {
        ResourceQuotas.Update(quota);
        await context.SaveChangesAsync(cancellationToken);

        return quota;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quota = await GetByIdAsync(id, cancellationToken);

        if (quota == null) return false;

        ResourceQuotas.Remove(quota);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<ResourceQuota>> GetActiveQuotasAsync(CancellationToken cancellationToken = default)
    {
        return await ResourceQuotas.Where(q => q.IsActive).OrderBy(q => q.TenantId!.Value).ThenBy(q => q.Type).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(CancellationToken cancellationToken = default)
    {
        var quotas = await ResourceQuotas.Where(q => q.IsActive).ToListAsync(cancellationToken);

        // Filter in memory using entity business logic, get unique tenant IDs
        return quotas.Where(q => q.IsHardLimitExceeded()).Select(q => q.TenantId!.Value).Distinct();
    }

    public async Task<IEnumerable<ResourceQuota>> GetQuotasDueForResetAsync(CancellationToken cancellationToken = default)
    {
        var quotas = await ResourceQuotas.Where(q => q.IsActive).ToListAsync(cancellationToken);

        // Use entity business logic to determine which quotas should reset
        return quotas.Where(q => q.ShouldReset());
    }

    public async Task<IEnumerable<ResourceQuota>> GetQuotasExceedingLimitsAsync(ResourceUsageType? type = null, bool softLimitOnly = false, CancellationToken cancellationToken = default)
    {
        var query = ResourceQuotas.Where(q => q.IsActive);

        if (type.HasValue) { query = query.Where(q => q.Type == type.Value); }

        var quotas = await query.ToListAsync(cancellationToken);

        // Filter in memory using entity business logic
        return quotas.Where(q => softLimitOnly ? q.IsSoftLimitExceeded() : q.IsHardLimitExceeded());
    }

    public async Task<IEnumerable<ResourceQuota>> GetQuotasExceedingLimitsAsync(CancellationToken cancellationToken = default)
    {
        var quotas = await ResourceQuotas.Where(q => q.IsActive).ToListAsync(cancellationToken);

        // Filter in memory using entity business logic
        return quotas.Where(q => q.IsHardLimitExceeded());
    }
}
