using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources;

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

    // User-level quota methods
    public async Task<ResourceQuota?> GetByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await ResourceQuotas.FirstOrDefaultAsync(q => q.UserId == userId && q.Type == type, cancellationToken);
    }

    public async Task<IEnumerable<ResourceQuota>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await ResourceQuotas.Where(q => q.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var quota = await GetByUserAndTypeAsync(userId, type, cancellationToken);

        if (quota == null) return false;

        ResourceQuotas.Remove(quota);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<(bool Success, ResourceQuota? Quota)> TryIncrementUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;

        for (var retryCount = 0; retryCount < maxRetries; retryCount++)
        {
            // Fresh query each retry to get latest state
            var quota = await ResourceQuotas
                .FirstOrDefaultAsync(q => q.TenantId!.Value == tenantId && q.Type == type, cancellationToken);

            // No quota means unlimited - allow operation
            if (quota == null)
                return (true, null);

            // Inactive quota - allow operation
            if (!quota.IsActive)
                return (true, quota);

            // Check if quota needs reset
            if (quota.ShouldReset())
            {
                quota.ResetUsage();
            }

            // Validate against hard limit
            var projectedUsage = quota.CurrentUsage + amount;
            if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
            {
                return (false, quota);
            }

            // Increment usage
            quota.CurrentUsage = projectedUsage;
            quota.UpdatedAt = DateTime.UtcNow;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return (true, quota);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request modified the quota concurrently
                // The entity is already detached after SaveChanges failure, just retry
                if (retryCount >= maxRetries - 1)
                {
                    throw new InvalidOperationException(
                        $"Failed to increment quota after {maxRetries} retries due to concurrent modifications. " +
                        $"Tenant: {tenantId}, Type: {type}, Amount: {amount}");
                }
                // Continue to next iteration with fresh query
            }
        }

        // Should not reach here
        return (false, null);
    }

    /// <inheritdoc/>
    public async Task<bool> DecrementUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount,
        CancellationToken cancellationToken = default)
    {
        var quota = await ResourceQuotas
            .FirstOrDefaultAsync(q => q.TenantId!.Value == tenantId && q.Type == type, cancellationToken);

        if (quota == null)
            return false;

        // Use entity method to ensure usage never goes negative
        quota.RemoveUsage(amount);
        quota.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
