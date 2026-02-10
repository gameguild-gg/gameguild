using System.Diagnostics;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Handles analytics, reporting, and background maintenance for quotas.
/// </summary>
public class QuotaMaintenanceService(
    IResourceQuotaRepository quotaRepository,
    IUsageRecordRepository usageRepository,
    IQuotaManagementService managementService,
    IPublisher publisher,
    ILogger<QuotaMaintenanceService> logger) : IQuotaMaintenanceService
{
    public static readonly ActivitySource ActivitySource = new("GameGuild.Resources.QuotaMaintenance", "1.0.0");

    private const string ResetQuotasOperation = "quota.reset_expired";
    private const string RecalculateUsageOperation = "quota.recalculate";

    /// <inheritdoc/>
    public async Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default)
    {
        var quota = await managementService.GetQuotaAsync(tenantId, type, cancellationToken).ConfigureAwait(false);
        var fromDate = SystemClock.UtcNow.AddDays(-historyDays);
        var history = await managementService.GetUsageHistoryAsync(tenantId, type, fromDate, null, cancellationToken).ConfigureAwait(false);

        var response = new ResourceUsageResponse
        {
            TenantId = tenantId,
            CurrentUsage = quota?.CurrentUsage ?? 0,
            PeriodStart = quota?.LastReset ?? SystemClock.UtcNow.AddMonths(-1),
            PeriodEnd = SystemClock.UtcNow,
            RemainingQuota = Math.Max(0, (quota?.HardLimit ?? 0) - (quota?.CurrentUsage ?? 0)),
            History = history.Select(h => new ResourceUsageHistoryItem { Timestamp = h.PeriodStart, Amount = h.UsageAmount, PeakUsage = h.PeakUsage }).ToList()
        };

        if (quota?.HardLimit > 0)
        {
            var hardLimitValue = quota.HardLimit.Value;
            response.UsagePercentage = (double) response.CurrentUsage / hardLimitValue * 100;
            response.RemainingQuota = Math.Max(0, hardLimitValue - response.CurrentUsage);
        }

        return response;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default)
    {
        var exceedingQuotas = await quotaRepository.GetQuotasExceedingLimitsAsync(type, !hardLimitOnly, cancellationToken).ConfigureAwait(false);

        return exceedingQuotas.Select(q => q.TenantId!.Value).Distinct();
    }

    /// <inheritdoc/>
    public async Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(ResetQuotasOperation, ActivityKind.Internal);

        var quotasDueForReset = await quotaRepository.GetQuotasDueForResetAsync(cancellationToken).ConfigureAwait(false);
        activity?.SetTag("quota.candidates_count", quotasDueForReset.Count());

        var resetCount = 0;

        foreach (var quota in quotasDueForReset.Where(q => q.ShouldReset()))
        {
            var previousUsage = quota.CurrentUsage;
            var tenantId = quota.TenantId!.Value;

            quota.ResetUsage();
            await quotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);

            await publisher.Publish(new QuotaChangedEvent(
                TenantId: tenantId,
                ResourceType: quota.Type,
                ChangeType: QuotaChangeType.Reset,
                PreviousUsage: previousUsage,
                CurrentUsage: 0,
                SoftLimit: quota.SoftLimit,
                HardLimit: quota.HardLimit,
                Source: "ResetExpiredQuotasAsync",
                ActorId: null,
                Timestamp: SystemClock.UtcNow), cancellationToken);

            resetCount++;
        }

        activity?.SetTag("quota.reset_count", resetCount);

        if (resetCount > 0) { logger.LogInformation("Reset {Count} expired quotas", resetCount); }

        return resetCount;
    }

    /// <inheritdoc/>
    public async Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var deleted = await usageRepository.DeleteOlderThanAsync(olderThan, cancellationToken).ConfigureAwait(false);

        if (deleted) { logger.LogInformation("Cleaned up old usage records older than {Date}", olderThan); }

        return deleted ? 1 : 0;
    }

    /// <inheritdoc/>
    public async Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(RecalculateUsageOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());

        try
        {
            var quota = await managementService.GetQuotaAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

            if (quota == null)
            {
                activity?.SetTag("quota.exists", false);
                return false;
            }

            activity?.SetTag("quota.exists", true);

            var periodStart = quota.LastReset ?? SystemClock.UtcNow.Date;

            var usageRecords = await usageRepository.GetByDateRangeAsync(tenantId, type, periodStart, SystemClock.UtcNow, cancellationToken).ConfigureAwait(false);

            var previousUsage = quota.CurrentUsage;
            quota.CurrentUsage = usageRecords.Sum(u => u.UsageAmount);
            quota.Touch();

            await quotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);

            activity?.SetTag("quota.previous_usage", previousUsage);
            activity?.SetTag("quota.recalculated_usage", quota.CurrentUsage);
            activity?.SetTag("quota.records_processed", usageRecords.Count());

            logger.LogInformation("Recalculated usage for tenant {TenantId}, type {Type}: {Usage}", tenantId, type, quota.CurrentUsage);

            return true;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            logger.LogError(ex, "Error recalculating usage for tenant {TenantId}, type {Type}", tenantId, type);

            return false;
        }
    }
}
