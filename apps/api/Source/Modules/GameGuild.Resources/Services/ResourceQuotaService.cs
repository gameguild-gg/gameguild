using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Implementation of resource quota management service
/// </summary>
public class ResourceQuotaService(
    IResourceQuotaRepository quotaRepository,
    IUsageRecordRepository usageRepository,
    IPublisher publisher,
    ILogger<ResourceQuotaService> logger) : IResourceQuotaService
{
    public async Task<ResourceQuota> SetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        long? softLimit,
        long? hardLimit,
        ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly,
        CancellationToken cancellationToken = default
    )
    {
        var existingQuota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken);
        var isNew = existingQuota == null;
        var previousUsage = existingQuota?.CurrentUsage;

        if (existingQuota != null)
        {
            existingQuota.SoftLimit = softLimit;
            existingQuota.HardLimit = hardLimit;
            existingQuota.Period = period;
            existingQuota.UpdatedAt = DateTime.UtcNow;
            existingQuota = await quotaRepository.UpdateAsync(existingQuota, cancellationToken);
        }
        else
        {
            existingQuota = new ResourceQuota { Type = type, SoftLimit = softLimit, HardLimit = hardLimit, Period = period, CurrentUsage = 0, LastReset = DateTime.UtcNow, IsActive = true };
            existingQuota.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });
            existingQuota = await quotaRepository.CreateAsync(existingQuota, cancellationToken);
        }

        // Publish audit event for quota change
        await publisher.Publish(new QuotaChangedEvent(
            TenantId: tenantId,
            ResourceType: type,
            ChangeType: isNew ? QuotaChangeType.Created : QuotaChangeType.LimitsUpdated,
            PreviousUsage: previousUsage,
            CurrentUsage: existingQuota.CurrentUsage,
            SoftLimit: softLimit,
            HardLimit: hardLimit,
            Source: "SetQuotaAsync",
            ActorId: null,
            Timestamp: DateTime.UtcNow), cancellationToken);

        logger.LogInformation("Set quota for tenant {TenantId}, type {Type}: Soft={SoftLimit}, Hard={HardLimit}", tenantId, type, softLimit, hardLimit);

        return existingQuota;
    }

    public async Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await quotaRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken);
    }

    public async Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default) { return await quotaRepository.GetByTenantAsync(tenantId, cancellationToken); }

    public async Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var quota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken);

        if (quota == null) return false;

        var previousUsage = quota.CurrentUsage;
        var deleted = await quotaRepository.DeleteAsync(quota.Id, cancellationToken);

        if (deleted)
        {
            // Publish audit event for quota deletion
            await publisher.Publish(new QuotaChangedEvent(
                TenantId: tenantId,
                ResourceType: type,
                ChangeType: QuotaChangeType.Deleted,
                PreviousUsage: previousUsage,
                CurrentUsage: 0,
                SoftLimit: quota.SoftLimit,
                HardLimit: quota.HardLimit,
                Source: "DeleteQuotaAsync",
                ActorId: null,
                Timestamp: DateTime.UtcNow), cancellationToken);

            logger.LogInformation("Deleted quota for tenant {TenantId}, type {Type}", tenantId, type);
        }

        return deleted;
    }

    public async Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota == null) return 0;

        // Return effective usage (0 if reset is due, otherwise current)
        // NOTE: Do NOT mutate state during read operations - reset happens during actual usage recording
        return quota.ShouldReset() ? 0 : quota.CurrentUsage;
    }

    public async Task<IEnumerable<UsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        return await usageRepository.GetByTenantAsync(tenantId, type, fromDate, toDate, cancellationToken);
    }

    /// <summary>
    ///     Check if a resource usage would exceed limits.
    ///     <para>
    ///     <b>ADVISORY ONLY:</b> This method is read-only and does not consume quota.
    ///     Use for UI/UX purposes (e.g., showing "approaching limit" warnings) or pre-flight checks.
    ///     For authoritative enforcement, use <see cref="TryAtomicConsumeAsync"/> instead.
    ///     </para>
    /// </summary>
    /// <inheritdoc/>
    public async Task<ResourceLimitCheckResponse> CheckLimitsAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota == null)
        {
            // No quota means unlimited
            return new ResourceLimitCheckResponse { Type = type, CanProceed = true, CurrentUsage = 0, SoftLimit = null, HardLimit = null };
        }

        // Calculate effective current usage considering if reset is due
        // NOTE: Do NOT mutate state during read operations - reset happens during actual usage recording
        var effectiveCurrentUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
        var projectedUsage = effectiveCurrentUsage + requestedAmount;

        // Check hard limit
        if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
        {
            return new ResourceLimitCheckResponse { Type = type, CanProceed = false, CurrentUsage = effectiveCurrentUsage, SoftLimit = quota.SoftLimit, HardLimit = quota.HardLimit };
        }

        // Note: Soft limit check is performed for future warning/notification purposes
        // but doesn't block the request

        return new ResourceLimitCheckResponse { Type = type, CanProceed = true, CurrentUsage = effectiveCurrentUsage, SoftLimit = quota.SoftLimit, HardLimit = quota.HardLimit };
    }

    public async Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(
        Guid tenantId,
        Dictionary<ResourceUsageType, long> requestedAmounts,
        CancellationToken cancellationToken = default
    )
    {
        // Batch query to avoid N+1: fetch all needed quotas in a single DB roundtrip
        var quotas = await quotaRepository.GetByTenantAndTypesAsync(
            tenantId,
            requestedAmounts.Keys,
            cancellationToken);

        var results = new Dictionary<ResourceUsageType, ResourceLimitCheckResponse>();

        foreach (var kvp in requestedAmounts)
        {
            var type = kvp.Key;
            var requestedAmount = kvp.Value;

            if (!quotas.TryGetValue(type, out var quota))
            {
                // No quota means unlimited
                results[type] = new ResourceLimitCheckResponse
                {
                    Type = type,
                    CanProceed = true,
                    CurrentUsage = 0,
                    SoftLimit = null,
                    HardLimit = null
                };
                continue;
            }

            // Calculate effective current usage considering if reset is due
            var effectiveCurrentUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
            var projectedUsage = effectiveCurrentUsage + requestedAmount;

            // Check hard limit
            if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
            {
                results[type] = new ResourceLimitCheckResponse
                {
                    Type = type,
                    CanProceed = false,
                    CurrentUsage = effectiveCurrentUsage,
                    SoftLimit = quota.SoftLimit,
                    HardLimit = quota.HardLimit
                };
            }
            else
            {
                results[type] = new ResourceLimitCheckResponse
                {
                    Type = type,
                    CanProceed = true,
                    CurrentUsage = effectiveCurrentUsage,
                    SoftLimit = quota.SoftLimit,
                    HardLimit = quota.HardLimit
                };
            }
        }

        return results;
    }

    /// <summary>
    ///     Attempt to consume resources with atomic enforcement.
    ///     <para>
    ///     <b>AUTHORITATIVE:</b> Delegates to <see cref="TryAtomicConsumeAsync"/> for atomic enforcement.
    ///     </para>
    /// </summary>
    /// <inheritdoc/>
    public async Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default)
    {
        // AUTHORITATIVE: Use atomic consume for thread-safe quota enforcement
        var (success, currentUsage, hardLimit) = await TryAtomicConsumeAsync(tenantId, type, amount, cancellationToken);

        if (success)
        {
            logger.LogDebug("Successfully consumed {Amount} units of {Type} for tenant {TenantId}", amount, type, tenantId);
        }
        else
        {
            logger.LogWarning("Failed to consume {Amount} units of {Type} for tenant {TenantId} - quota exceeded", amount, type, tenantId);
        }

        // Get quota for soft limit info
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        return new ResourceLimitCheckResponse
        {
            Type = type,
            CanProceed = success,
            CurrentUsage = currentUsage,
            SoftLimit = quota?.SoftLimit,
            HardLimit = hardLimit
        };
    }

    /// <summary>
    ///     Atomically attempts to consume resources with optimistic concurrency.
    ///     <para>
    ///     <b>AUTHORITATIVE:</b> This is the core atomic operation for quota enforcement.
    ///     Uses RowVersion concurrency with retry logic to prevent race conditions.
    ///     </para>
    /// </summary>
    /// <inheritdoc/>
    public async Task<(bool Success, long CurrentUsage, long? HardLimit)> TryAtomicConsumeAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        CancellationToken cancellationToken = default)
    {
        var (success, quota) = await quotaRepository.TryIncrementUsageAsync(tenantId, type, amount, cancellationToken);

        if (quota == null)
        {
            // No quota exists = unlimited
            return (true, 0, null);
        }

        if (success)
        {
            // Publish audit event for successful consumption
            await publisher.Publish(new QuotaChangedEvent(
                TenantId: tenantId,
                ResourceType: type,
                ChangeType: QuotaChangeType.UsageIncremented,
                PreviousUsage: quota.CurrentUsage - amount,
                CurrentUsage: quota.CurrentUsage,
                SoftLimit: quota.SoftLimit,
                HardLimit: quota.HardLimit,
                Source: "TryAtomicConsumeAsync",
                ActorId: null,
                Timestamp: DateTime.UtcNow), cancellationToken);
        }
        else
        {
            // Publish event for quota exceeded attempt
            await publisher.Publish(new QuotaExceededEvent(
                TenantId: tenantId,
                ResourceType: type,
                CurrentUsage: quota.CurrentUsage,
                RequestedAmount: amount,
                HardLimit: quota.HardLimit ?? 0,
                Source: "TryAtomicConsumeAsync",
                ActorId: null,
                Timestamp: DateTime.UtcNow), cancellationToken);
        }

        return (success, quota.CurrentUsage, quota.HardLimit);
    }

    public async Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
        var fromDate = DateTime.UtcNow.AddDays(-historyDays);
        var history = await GetUsageHistoryAsync(tenantId, type, fromDate, null, cancellationToken);

        var response = new ResourceUsageResponse
        {
            TenantId = tenantId,
            CurrentUsage = quota?.CurrentUsage ?? 0,
            PeriodStart = quota?.LastReset ?? DateTime.UtcNow.AddMonths(-1),
            PeriodEnd = DateTime.UtcNow,
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

    public async Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default)
    {
        var exceedingQuotas = await quotaRepository.GetQuotasExceedingLimitsAsync(type, !hardLimitOnly, cancellationToken);

        // Results are already filtered by the repository method

        return exceedingQuotas.Select(q => q.TenantId!.Value).Distinct();
    }

    public async Task<bool> DecrementUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        Guid? userId = null,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current quota for audit purposes
            var quotaBefore = await GetQuotaAsync(tenantId, type, cancellationToken);
            var previousUsage = quotaBefore?.CurrentUsage ?? 0;

            var decremented = await quotaRepository.DecrementUsageAsync(tenantId, type, amount, cancellationToken);

            if (decremented)
            {
                // Publish audit event for decrement
                await publisher.Publish(new QuotaChangedEvent(
                    TenantId: tenantId,
                    ResourceType: type,
                    ChangeType: QuotaChangeType.UsageDecremented,
                    PreviousUsage: previousUsage,
                    CurrentUsage: Math.Max(0, previousUsage - amount),
                    SoftLimit: quotaBefore?.SoftLimit,
                    HardLimit: quotaBefore?.HardLimit,
                    Source: source ?? "DecrementUsageAsync",
                    ActorId: userId,
                    Timestamp: DateTime.UtcNow), cancellationToken);

                logger.LogInformation(
                    "Decremented {Amount} {Type} usage for tenant {TenantId} (source: {Source})",
                    amount, type, tenantId, source ?? "unknown");
            }

            return decremented;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error decrementing usage for tenant {TenantId}, type {Type}", tenantId, type);
            return false;
        }
    }

    public async Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default)
    {
        var quotasDueForReset = await quotaRepository.GetQuotasDueForResetAsync(cancellationToken);

        var resetCount = 0;

        foreach (var quota in quotasDueForReset.Where(q => q.ShouldReset()))
        {
            var previousUsage = quota.CurrentUsage;
            var tenantId = quota.TenantId!.Value;

            quota.ResetUsage();
            await quotaRepository.UpdateAsync(quota, cancellationToken);

            // Publish audit event for quota reset
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
                Timestamp: DateTime.UtcNow), cancellationToken);

            resetCount++;
        }

        if (resetCount > 0) { logger.LogInformation("Reset {Count} expired quotas", resetCount); }

        return resetCount;
    }

    public async Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var deleted = await usageRepository.DeleteOlderThanAsync(olderThan, cancellationToken);

        if (deleted) { logger.LogInformation("Cleaned up old usage records older than {Date}", olderThan); }

        return deleted ? 1 : 0;
    }

    public async Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        try
        {
            var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

            if (quota == null) return false;

            // Get current period start based on quota period and last reset
            var periodStart = quota.LastReset ?? DateTime.UtcNow.Date;

            var usageRecords = await usageRepository.GetByDateRangeAsync(tenantId, type, periodStart, DateTime.UtcNow, cancellationToken);

            quota.CurrentUsage = usageRecords.Sum(u => u.UsageAmount);
            quota.UpdatedAt = DateTime.UtcNow;

            await quotaRepository.UpdateAsync(quota, cancellationToken);

            logger.LogInformation("Recalculated usage for tenant {TenantId}, type {Type}: {Usage}", tenantId, type, quota.CurrentUsage);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recalculating usage for tenant {TenantId}, type {Type}", tenantId, type);

            return false;
        }
    }
}
