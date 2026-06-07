using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Basic implementation of usage tracking service.
/// </summary>
public class UsageService(
    IUsageRecordRepository usageRecordRepository,
    IResourceQuotaRepository quotaRepository,
    ILogger<UsageService> logger) : IUsageService
{
    public async Task<TenantResourceUsage> GetCurrentUsageAsync(Guid tenantId)
    {
        ValidateTenantId(tenantId);

        var now = SystemClock.UtcNow;
        var quotas = (await quotaRepository.GetByTenantAsync(tenantId).ConfigureAwait(false)).ToList();
        var usageRecords = await usageRecordRepository.GetByTenantAsync(
            tenantId,
            type: null,
            fromDate: new DateTime(now.Year, now.Month, 1),
            toDate: now).ConfigureAwait(false);

        var currentUsage = usageRecords
            .GroupBy(r => r.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(r => r.UsageAmount), StringComparer.OrdinalIgnoreCase);

        foreach (var quota in quotas)
        {
            currentUsage[quota.Type.ToString()] = Math.Max(
                currentUsage.GetValueOrDefault(quota.Type.ToString()),
                quota.CurrentUsage);
        }

        return new TenantResourceUsage
        {
            TenantId = tenantId,
            CurrentUsage = currentUsage,
            Limits = quotas
                .Where(q => q.HardLimit.HasValue)
                .ToDictionary(q => q.Type.ToString(), q => q.HardLimit!.Value, StringComparer.OrdinalIgnoreCase),
            PeriodStart = new DateTime(now.Year, now.Month, 1),
            PeriodEnd = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc)
        };
    }

    public async Task<IEnumerable<ResourceUsageHistory>> GetUsageHistoryAsync(Guid tenantId, int months)
    {
        ValidateTenantId(tenantId);

        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), months, "History window must be greater than zero.");
        }

        var now = SystemClock.UtcNow;
        var fromDate = now.Date.AddMonths(-months);
        var records = await usageRecordRepository.GetByTenantAsync(
            tenantId,
            type: null,
            fromDate,
            now).ConfigureAwait(false);

        return records
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => new ResourceUsageHistory
            {
                Id = r.Id,
                TenantId = tenantId,
                ResourceType = r.Type.ToString(),
                Amount = r.UsageAmount,
                RecordedAt = r.CreatedAt,
                PeriodStart = r.PeriodStart,
                PeriodEnd = r.PeriodEnd
            })
            .ToList();
    }

    public async Task TrackUsageAsync(Guid tenantId, string resourceType, int amount)
    {
        ValidateTenantId(tenantId);

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Usage amount must be greater than zero.");
        }

        var type = ParseResourceType(resourceType);
        var now = SystemClock.UtcNow;

        var (incremented, quota) = await quotaRepository
            .TryIncrementUsageAsync(tenantId, type, amount)
            .ConfigureAwait(false);

        await usageRecordRepository.CreateAsync(
            UsageRecord.CreateDaily(type, tenantId, amount, now, source: nameof(UsageService)))
            .ConfigureAwait(false);

        logger.LogInformation(
            "Tracked usage for tenant {TenantId}, type {ResourceType}, amount {Amount}, quotaIncremented {QuotaIncremented}, currentUsage {CurrentUsage}",
            tenantId,
            type,
            amount,
            incremented,
            quota?.CurrentUsage);
    }

    public async Task<bool> IsWithinLimitsAsync(Guid tenantId, string resourceType, int requestedAmount)
    {
        ValidateTenantId(tenantId);

        if (requestedAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedAmount), requestedAmount, "Requested amount cannot be negative.");
        }

        var type = ParseResourceType(resourceType);
        var quota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, type).ConfigureAwait(false);

        if (quota is null || !quota.IsActive || !quota.HardLimit.HasValue)
        {
            return true;
        }

        return quota.CurrentUsage + requestedAmount <= quota.HardLimit.Value;
    }

    private static void ValidateTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID must be a non-empty GUID.", nameof(tenantId));
        }
    }

    private static ResourceUsageType ParseResourceType(string resourceType)
    {
        if (Enum.TryParse<ResourceUsageType>(resourceType, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Unknown resource usage type '{resourceType}'.", nameof(resourceType));
    }
}
