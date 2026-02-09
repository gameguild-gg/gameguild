using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for checking resource usage limits against quotas
/// </summary>
public class CheckResourceUsageLimitsQueryHandler(
    IResourceQuotaRepository resourceQuotaRepository,
    IApplicationDbContext dbContext) : IQueryHandler<CheckResourceUsageLimitsQuery, Dictionary<ResourceUsageType, bool>>
{
    public async Task<Dictionary<ResourceUsageType, bool>> Handle(CheckResourceUsageLimitsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new Dictionary<ResourceUsageType, bool>();

        // Get quotas for the tenant
        IEnumerable<ResourceQuota> quotas;

        if (request.ResourceUsageType.HasValue)
        {
            var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType.Value, cancellationToken).ConfigureAwait(false);
            quotas = quota != null ? new[ ] { quota } : Array.Empty<ResourceQuota>();
        }
        else { quotas = await resourceQuotaRepository.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false); }

        var quotasToUpdate = new List<ResourceQuota>();

        foreach (var quota in quotas.Where(q => q.IsActive))
        {
            // Check if quota needs reset
            if (quota.ShouldReset())
            {
                quota.ResetUsage();
                quotasToUpdate.Add(quota);
            }

            // Check if hard limit is exceeded (this means the limit is violated)
            var isLimitExceeded = quota.IsHardLimitExceeded();
            result[quota.Type] = isLimitExceeded;
        }

        // If specific usage type was requested and no quota exists, return false (no limit exceeded)
        if (request.ResourceUsageType.HasValue && !result.ContainsKey(request.ResourceUsageType.Value)) { result[request.ResourceUsageType.Value] = false; }

        // Batch save all updated quotas in a single transaction (avoid N+1 writes)
        if (quotasToUpdate.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}
