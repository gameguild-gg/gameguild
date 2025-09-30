using System.Text.Json;

namespace GameGuild.Modules.Resources;

/// <summary> Implementation of resource quota management service </summary>
public class ResourceQuotaService(IResourceQuotaRepository repository, ILogger<ResourceQuotaService> logger) : IResourceQuotaService
{
    private readonly IResourceQuotaRepository _repository = repository;

    public async Task<ResourceQuota> SetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        long? softLimit,
        long? hardLimit,
        ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly,
        CancellationToken cancellationToken = default
    )
    {
        ResourceQuota? existingQuota = await _repository.GetQuotaAsync(tenantId, type, cancellationToken);

        if (existingQuota != null)
        {
            existingQuota.SoftLimit = softLimit;
            existingQuota.HardLimit = hardLimit;
            existingQuota.Period = period;
            existingQuota.UpdatedAt = DateTime.UtcNow;
            existingQuota = await _repository.UpdateQuotaAsync(existingQuota, cancellationToken);
        }
        else
        {
            existingQuota = new ResourceQuota { TenantId = tenantId, Type = type, SoftLimit = softLimit, HardLimit = hardLimit, Period = period, LastReset = DateTime.UtcNow };
            existingQuota = await _repository.CreateQuotaAsync(existingQuota, cancellationToken);
        }

        logger.LogInformation("Set quota for tenant {TenantId}, type {Type}: Soft={SoftLimit}, Hard={HardLimit}", tenantId, type, softLimit, hardLimit);

        return existingQuota;
    }

    public async Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default) { return await _repository.GetQuotaAsync(tenantId, type, cancellationToken); }

    public async Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default) { return await _repository.GetTenantQuotasAsync(tenantId, cancellationToken); }

    public async Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        bool deleted = await _repository.DeleteQuotaAsync(tenantId, type, cancellationToken);

        if (deleted) { logger.LogInformation("Deleted quota for tenant {TenantId}, type {Type}", tenantId, type); }

        return deleted;
    }

    public async Task<bool> RecordUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        Guid? userId = null,
        string? source = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Update quota usage
            ResourceQuota? quota = await GetQuotaAsync(tenantId, type, cancellationToken);

            if (quota != null)
            {
                // Check if quota needs reset
                if (quota.ShouldReset()) { quota.Reset(); }

                quota.CurrentUsage += amount;
                quota.UpdatedAt = DateTime.UtcNow;
            }

            // Record usage history
            DateTime today = DateTime.UtcNow.Date;
            ResourceUsageRecord? usageRecord = await _repository.GetUsageRecordAsync(tenantId, type, today, cancellationToken);

            if (usageRecord != null)
            {
                usageRecord.Count += amount;
                usageRecord.UpdatedAt = DateTime.UtcNow;

                // Update peak usage if this is higher
                if (usageRecord.PeakUsage == null || usageRecord.Count > usageRecord.PeakUsage)
                {
                    usageRecord.PeakUsage = usageRecord.Count;
                    usageRecord.PeakUsageDate = DateTime.UtcNow;
                }

                await _repository.UpdateUsageRecordAsync(usageRecord, cancellationToken);
            }
            else
            {
                usageRecord = ResourceUsageRecord.CreateDaily(type, tenantId, amount, today, userId, source);
                usageRecord.PeakUsage = amount;
                usageRecord.PeakUsageDate = DateTime.UtcNow;

                if (metadata != null) { usageRecord.Metadata = JsonSerializer.Serialize(metadata); }

                await _repository.CreateUsageRecordAsync(usageRecord, cancellationToken);
            }

            logger.LogDebug("Recorded usage for tenant {TenantId}, type {Type}: amount={Amount}", tenantId, type, amount);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording usage for tenant {TenantId}, type {Type}", tenantId, type);

            return false;
        }
    }

    public async Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        ResourceQuota? quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota?.ShouldReset() != true) return quota?.CurrentUsage ?? 0;

        quota.Reset();
        await _repository.UpdateQuotaAsync(quota, cancellationToken);

        return 0;
    }

    public async Task<IEnumerable<ResourceUsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        return await _repository.GetUsageHistoryAsync(tenantId, type, fromDate, toDate, cancellationToken);
    }

    public async Task<ResourceLimitCheckResponse> CheckLimitsAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default)
    {
        ResourceQuota? quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota == null)
        {
            // No quota means unlimited
            return ResourceLimitCheckResponse.Success(type, requestedAmount, null, null, "No quota set - unlimited");
        }

        // Check if quota needs reset
        if (quota.ShouldReset())
        {
            quota.Reset();
            _ = await _repository.UpdateQuotaAsync(quota, cancellationToken);
        }

        long currentUsage = quota.CurrentUsage;
        long projectedUsage = currentUsage + requestedAmount;

        // Check hard limit
        if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
        {
            return ResourceLimitCheckResponse.LimitExceeded(type, currentUsage, quota.SoftLimit, quota.HardLimit, $"Hard limit exceeded. Requested: {requestedAmount}, Current: {currentUsage}, Limit: {quota.HardLimit}");
        }

        // Check soft limit
        bool softLimitExceeded = quota.SoftLimit.HasValue && projectedUsage > quota.SoftLimit.Value;
        string message = softLimitExceeded ? $"Soft limit exceeded but within hard limit. Requested: {requestedAmount}, Current: {currentUsage}, Soft Limit: {quota.SoftLimit}" : "Within limits";

        return ResourceLimitCheckResponse.Success(type, currentUsage, quota.SoftLimit, quota.HardLimit, message);
    }

    public async Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(
        Guid tenantId,
        Dictionary<ResourceUsageType, long> requestedAmounts,
        CancellationToken cancellationToken = default
    )
    {
        var results = new Dictionary<ResourceUsageType, ResourceLimitCheckResponse>();

        foreach (var kvp in requestedAmounts) { results[kvp.Key] = await CheckLimitsAsync(tenantId, kvp.Key, kvp.Value, cancellationToken); }

        return results;
    }

    public async Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default)
    {
        ResourceLimitCheckResponse limitCheck = await CheckLimitsAsync(tenantId, type, amount, cancellationToken);

        if (limitCheck.CanProceed)
        {
            await RecordUsageAsync(tenantId, type, amount, userId, source, cancellationToken : cancellationToken);
            logger.LogDebug("Successfully consumed {Amount} units of {Type} for tenant {TenantId}", amount, type, tenantId);
        }
        else { logger.LogWarning("Failed to consume {Amount} units of {Type} for tenant {TenantId}: {Reason}", amount, type, tenantId, limitCheck.Message); }

        return limitCheck;
    }

    public async Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default)
    {
        ResourceQuota? quota = await GetQuotaAsync(tenantId, type, cancellationToken);
        DateTime fromDate = DateTime.UtcNow.AddDays(-historyDays);
        var history = await GetUsageHistoryAsync(tenantId, type, fromDate, null, cancellationToken);

        var response = new ResourceUsageResponse
        {
            Type = type,
            CurrentUsage = quota?.CurrentUsage ?? 0,
            SoftLimit = quota?.SoftLimit,
            HardLimit = quota?.HardLimit,
            Period = quota?.Period ?? ResourceQuotaPeriod.Monthly,
            LastReset = quota?.LastReset,
            IsActive = quota?.IsActive ?? false,
            History = history.Select(h => new ResourceUsageHistoryItem { Date = h.PeriodStart, Count = h.Count, PeakUsage = h.PeakUsage }).ToList()
        };

        if (quota?.HardLimit is null or <= 0) { return response; }

        long hardLimitValue = quota.HardLimit.Value;
        response.UsagePercentage = (double) response.CurrentUsage / hardLimitValue * 100;
        response.RemainingQuota = Math.Max(0, hardLimitValue - response.CurrentUsage);

        return response;
    }

    public async Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default)
    {
        return await _repository.GetTenantsExceedingLimitsAsync(type, hardLimitOnly, cancellationToken);
    }

    public async Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default)
    {
        var expiredQuotas = await _repository.GetActiveQuotasWithLastResetAsync(cancellationToken);

        var resetCount = 0;

        foreach (ResourceQuota quota in expiredQuotas.Where(q => q.ShouldReset()))
        {
            quota.Reset();
            _ = await _repository.UpdateQuotaAsync(quota, cancellationToken);
            resetCount++;
        }

        if (resetCount <= 0) { return resetCount; }

        logger.LogInformation("Reset {Count} expired quotas", resetCount);

        return resetCount;
    }

    public async Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var oldRecords = await _repository.GetOldUsageRecordsAsync(olderThan, cancellationToken);

        if (oldRecords.Count <= 0) { return oldRecords.Count; }

        await _repository.RemoveUsageRecordsAsync(oldRecords, cancellationToken);
        logger.LogInformation("Cleaned up {Count} old usage records older than {Date}", oldRecords.Count, olderThan);

        return oldRecords.Count;
    }

    public async Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        try
        {
            ResourceQuota? quota = await GetQuotaAsync(tenantId, type, cancellationToken);

            if (quota == null) { return false; }

            // Get current period start based on quota period and last reset
            DateTime periodStart = quota.LastReset ?? DateTime.UtcNow.Date;

            long totalUsage = await _repository.GetTotalUsageAsync(tenantId, type, periodStart, cancellationToken);

            quota.CurrentUsage = totalUsage;
            quota.UpdatedAt = DateTime.UtcNow;

            _ = await _repository.UpdateQuotaAsync(quota, cancellationToken);

            logger.LogInformation("Recalculated usage for tenant {TenantId}, type {Type}: {Usage}", tenantId, type, totalUsage);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recalculating usage for tenant {TenantId}, type {Type}", tenantId, type);

            return false;
        }
    }
}
