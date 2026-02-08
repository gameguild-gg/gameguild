using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Implementation of the usage tracking service for tenant resources
/// </summary>
public class UsageTrackingService(IApplicationDbContext context) : IUsageTrackingService
{
    /// <inheritdoc />
    public async Task<Guid> TrackUsageAsync(UsageTracking usageTracking, CancellationToken cancellationToken = default)
    {
        context.Set<UsageTracking>().Add(usageTracking);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return usageTracking.Id;
    }

    /// <inheritdoc />
    public async Task<List<UsageTracking>> GetUsageAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        string? resourceType = null,
        string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<UsageTracking>()
            .Where(u => u.TenantId == tenantId && u.DeletedAt == null)
            .Where(u => u.Date >= startDate && u.Date <= endDate);

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(u => u.ResourceType == resourceType);
        }

        return await query
            .OrderByDescending(u => u.Date)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UsageSummary> GetUsageSummaryAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var usageData = await context.Set<UsageTracking>()
            .Where(u => u.TenantId == tenantId && u.DeletedAt == null)
            .Where(u => u.Date >= startDate && u.Date <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var actionCounts = usageData
            .GroupBy(u => u.ResourceType)
            .ToDictionary(g => g.Key, g => g.Count());

        var resourceCounts = usageData
            .GroupBy(u => u.ResourceType)
            .ToDictionary(g => g.Key, g => (int)g.Sum(u => u.UsageAmount));

        var resourceCosts = usageData
            .GroupBy(u => u.ResourceType)
            .ToDictionary(g => g.Key, g => g.Sum(u => u.Cost));

        return new TenantUsageSummary
        {
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            TotalActions = usageData.Count,
            TotalCost = usageData.Sum(u => u.Cost),
            ActionCounts = actionCounts,
            ResourceCounts = resourceCounts,
            ResourceCosts = resourceCosts
        };
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldUsageDataAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldRecords = await context.Set<UsageTracking>()
            .Where(u => u.Date < cutoffDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in oldRecords)
        {
            record.SoftDelete();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return oldRecords.Count;
    }
}

/// <summary>
///     Concrete implementation of UsageSummary for tenant usage
/// </summary>
public record TenantUsageSummary : UsageSummary;
