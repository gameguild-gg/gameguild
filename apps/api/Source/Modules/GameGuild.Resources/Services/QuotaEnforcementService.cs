using System.Diagnostics;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Handles quota enforcement: limit checks, atomic consumption, and decrement.
/// </summary>
public class QuotaEnforcementService(
    IResourceQuotaRepository quotaRepository,
    IQuotaManagementService managementService,
    IPublisher publisher,
    ILogger<QuotaEnforcementService> logger) : IQuotaEnforcementService
{
    public static readonly ActivitySource ActivitySource = new("GameGuild.Resources.QuotaEnforcement", "1.0.0");

    private const string CheckLimitsOperation = "quota.check_limits";
    private const string ConsumeResourceOperation = "quota.consume";
    private const string AtomicConsumeOperation = "quota.atomic_consume";
    private const string DecrementUsageOperation = "quota.decrement";

    /// <inheritdoc/>
    public async Task<ResourceLimitCheckResponse> CheckLimitsAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(CheckLimitsOperation, ActivityKind.Internal);
        activity?.SetTag("tenant.id", tenantId.ToString());
        activity?.SetTag("resource.type", type.ToString());
        activity?.SetTag("quota.requested_amount", requestedAmount);

        var quota = await managementService.GetQuotaAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

        if (quota == null)
        {
            activity?.SetTag("quota.exists", false);
            activity?.SetTag("quota.can_proceed", true);
            return new ResourceLimitCheckResponse { Type = type, CanProceed = true, CurrentUsage = 0, SoftLimit = null, HardLimit = null };
        }

        activity?.SetTag("quota.exists", true);

        var effectiveCurrentUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
        var projectedUsage = effectiveCurrentUsage + requestedAmount;

        activity?.SetTag("quota.effective_usage", effectiveCurrentUsage);
        activity?.SetTag("quota.projected_usage", projectedUsage);

        if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
        {
            activity?.SetTag("quota.can_proceed", false);
            activity?.SetTag("quota.would_exceed", "hard_limit");
            return new ResourceLimitCheckResponse { Type = type, CanProceed = false, CurrentUsage = effectiveCurrentUsage, SoftLimit = quota.SoftLimit, HardLimit = quota.HardLimit };
        }

        activity?.SetTag("quota.can_proceed", true);

        return new ResourceLimitCheckResponse { Type = type, CanProceed = true, CurrentUsage = effectiveCurrentUsage, SoftLimit = quota.SoftLimit, HardLimit = quota.HardLimit };
    }

    /// <inheritdoc/>
    public async Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(
        Guid tenantId,
        Dictionary<ResourceUsageType, long> requestedAmounts,
        CancellationToken cancellationToken = default)
    {
        var quotas = await quotaRepository.GetByTenantAndTypesAsync(
            tenantId,
            requestedAmounts.Keys,
            cancellationToken).ConfigureAwait(false);

        var results = new Dictionary<ResourceUsageType, ResourceLimitCheckResponse>();

        foreach (var kvp in requestedAmounts)
        {
            var type = kvp.Key;
            var requestedAmount = kvp.Value;

            if (!quotas.TryGetValue(type, out var quota))
            {
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

            var effectiveCurrentUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
            var projectedUsage = effectiveCurrentUsage + requestedAmount;

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

    /// <inheritdoc/>
    public async Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default)
    {
        var (success, currentUsage, hardLimit) = await TryAtomicConsumeAsync(tenantId, type, amount, cancellationToken).ConfigureAwait(false);

        if (success)
        {
            logger.LogDebug("Successfully consumed {Amount} units of {Type} for tenant {TenantId}", amount, type, tenantId);
        }
        else
        {
            logger.LogWarning("Failed to consume {Amount} units of {Type} for tenant {TenantId} - quota exceeded", amount, type, tenantId);
        }

        var quota = await managementService.GetQuotaAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

        return new ResourceLimitCheckResponse
        {
            Type = type,
            CanProceed = success,
            CurrentUsage = currentUsage,
            SoftLimit = quota?.SoftLimit,
            HardLimit = hardLimit
        };
    }

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

        var (success, quota) = await quotaRepository.TryIncrementUsageAsync(tenantId, type, amount, cancellationToken).ConfigureAwait(false);

        if (quota == null)
        {
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

    /// <inheritdoc/>
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
            var quotaBefore = await managementService.GetQuotaAsync(tenantId, type, cancellationToken).ConfigureAwait(false);
            var previousUsage = quotaBefore?.CurrentUsage ?? 0;

            var decremented = await quotaRepository.DecrementUsageAsync(tenantId, type, amount, cancellationToken).ConfigureAwait(false);

            activity?.SetTag("quota.success", decremented);
            activity?.SetTag("quota.previous_usage", previousUsage);

            if (decremented)
            {
                activity?.SetTag("quota.new_usage", Math.Max(0, previousUsage - amount));
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
}
