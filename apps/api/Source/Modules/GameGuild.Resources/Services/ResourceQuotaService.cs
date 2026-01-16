using System.Diagnostics;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Implementation of resource quota management service with OpenTelemetry tracing
/// </summary>
public class ResourceQuotaService(
    IResourceQuotaRepository quotaRepository,
    IUsageRecordRepository usageRepository,
    IPublisher publisher,
    ILogger<ResourceQuotaService> logger) : IResourceQuotaService
{
    /// <summary>
    ///     ActivitySource for OpenTelemetry distributed tracing of quota operations.
    ///     Name follows convention: {Assembly}.{Component}
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("GameGuild.Resources.Quota", "1.0.0");

    // Activity operation names for consistent tracing
    private const string SetQuotaOperation = "quota.set";
    private const string GetQuotaOperation = "quota.get";
    private const string DeleteQuotaOperation = "quota.delete";
    private const string CheckLimitsOperation = "quota.check_limits";
    private const string ConsumeResourceOperation = "quota.consume";
    private const string AtomicConsumeOperation = "quota.atomic_consume";
    private const string DecrementUsageOperation = "quota.decrement";
    private const string ResetQuotasOperation = "quota.reset_expired";
    private const string RecalculateUsageOperation = "quota.recalculate";
    public async Task<ResourceQuota> SetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        long? softLimit,
        long? hardLimit,
        ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = ActivitySource.StartActivity(SetQuotaOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());
        activity?.SetTag("quota.soft_limit", softLimit?.ToString() ?? "unlimited");
        activity?.SetTag("quota.hard_limit", hardLimit?.ToString() ?? "unlimited");
        activity?.SetTag("quota.period", period.ToString());

        var existingQuota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken);
        var isNew = existingQuota == null;
        var previousUsage = existingQuota?.CurrentUsage;

        activity?.SetTag("quota.is_new", isNew);

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
        using var activity = ActivitySource.StartActivity(GetQuotaOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());

        var quota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken);
        activity?.SetTag("quota.found", quota != null);
        if (quota != null)
        {
            activity?.SetTag("quota.current_usage", quota.CurrentUsage);
            activity?.SetTag("quota.hard_limit", quota.HardLimit?.ToString() ?? "unlimited");
        }

        return quota;
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
        using var activity = ActivitySource.StartActivity(CheckLimitsOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());
        activity?.SetTag("quota.requested_amount", requestedAmount);

        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota == null)
        {
            // No quota means unlimited
            activity?.SetTag("quota.exists", false);
            activity?.SetTag("quota.can_proceed", true);
            return new ResourceLimitCheckResponse { Type = type, CanProceed = true, CurrentUsage = 0, SoftLimit = null, HardLimit = null };
        }

        activity?.SetTag("quota.exists", true);

        // Calculate effective current usage considering if reset is due
        // NOTE: Do NOT mutate state during read operations - reset happens during actual usage recording
        var effectiveCurrentUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
        var projectedUsage = effectiveCurrentUsage + requestedAmount;

        activity?.SetTag("quota.effective_usage", effectiveCurrentUsage);
        activity?.SetTag("quota.projected_usage", projectedUsage);

        // Check hard limit
        if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
        {
            activity?.SetTag("quota.can_proceed", false);
            activity?.SetTag("quota.would_exceed", "hard_limit");
            return new ResourceLimitCheckResponse { Type = type, CanProceed = false, CurrentUsage = effectiveCurrentUsage, SoftLimit = quota.SoftLimit, HardLimit = quota.HardLimit };
        }

        // Note: Soft limit check is performed for future warning/notification purposes
        // but doesn't block the request
        activity?.SetTag("quota.can_proceed", true);

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
        using var activity = ActivitySource.StartActivity(AtomicConsumeOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());
        activity?.SetTag("quota.requested_amount", amount);

        var (success, quota) = await quotaRepository.TryIncrementUsageAsync(tenantId, type, amount, cancellationToken);

        if (quota == null)
        {
            // No quota exists = unlimited
            activity?.SetTag("quota.exists", false);
            activity?.SetTag("quota.result", "unlimited");
            return (true, 0, null);
        }

        activity?.SetTag("quota.exists", true);
        activity?.SetTag("quota.success", success);
        activity?.SetTag("quota.current_usage", quota.CurrentUsage);
        activity?.SetTag("quota.hard_limit", quota.HardLimit?.ToString() ?? "unlimited");

        if (success)
        {
            activity?.SetTag("quota.result", "consumed");
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
            activity?.SetTag("quota.result", "exceeded");
            activity?.SetStatus(ActivityStatusCode.Error, "Quota exceeded");
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
        using var activity = ActivitySource.StartActivity(DecrementUsageOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());
        activity?.SetTag("quota.decrement_amount", amount);
        activity?.SetTag("quota.source", source ?? "unknown");

        try
        {
            // Get current quota for audit purposes
            var quotaBefore = await GetQuotaAsync(tenantId, type, cancellationToken);
            var previousUsage = quotaBefore?.CurrentUsage ?? 0;

            var decremented = await quotaRepository.DecrementUsageAsync(tenantId, type, amount, cancellationToken);

            activity?.SetTag("quota.success", decremented);
            activity?.SetTag("quota.previous_usage", previousUsage);

            if (decremented)
            {
                activity?.SetTag("quota.new_usage", Math.Max(0, previousUsage - amount));
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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            logger.LogError(ex, "Error decrementing usage for tenant {TenantId}, type {Type}", tenantId, type);
            return false;
        }
    }

    public async Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(ResetQuotasOperation, ActivityKind.Internal);

        var quotasDueForReset = await quotaRepository.GetQuotasDueForResetAsync(cancellationToken);
        activity?.SetTag("quota.candidates_count", quotasDueForReset.Count());

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

        activity?.SetTag("quota.reset_count", resetCount);

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
        using var activity = ActivitySource.StartActivity(RecalculateUsageOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());

        try
        {
            var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

            if (quota == null)
            {
                activity?.SetTag("quota.exists", false);
                return false;
            }

            activity?.SetTag("quota.exists", true);

            // Get current period start based on quota period and last reset
            var periodStart = quota.LastReset ?? DateTime.UtcNow.Date;

            var usageRecords = await usageRepository.GetByDateRangeAsync(tenantId, type, periodStart, DateTime.UtcNow, cancellationToken);

            var previousUsage = quota.CurrentUsage;
            quota.CurrentUsage = usageRecords.Sum(u => u.UsageAmount);
            quota.UpdatedAt = DateTime.UtcNow;

            await quotaRepository.UpdateAsync(quota, cancellationToken);

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
