using GameGuild.Commerce.Subscriptions;
using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.API.Integration;

/// <summary>
///     Cross-module event handler that syncs resource quotas when a subscription plan changes.
///     Economic invariant: Plan change (upgrade/downgrade) → Tenant quotas reflect new plan limits.
/// </summary>
/// <remarks>
///     This handler coordinates between Commerce.Subscriptions and Resources modules,
///     residing in the API composition root to maintain module independence.
///
///     IMPORTANT: This handler updates quotas to the new plan's limits. For downgrades,
///     if current usage exceeds new limits, the system will enforce soft-limit warnings
///     but allow continued operation until the next billing cycle (grace period).
///
///     A quota update failure is never swallowed: every per-quota failure is logged as an
///     error and, if any quota failed to apply, the handler throws an aggregate failure so
///     the plan change surfaces as failed instead of silently leaving stale (typically
///     higher) limits in place.
/// </remarks>
public sealed class SubscriptionPlanChangedQuotaSyncHandler(
    ISubscriptionPlanRepository planRepository,
    IResourceQuotaService resourceQuotaService,
    ILogger<SubscriptionPlanChangedQuotaSyncHandler> logger
) : INotificationHandler<SubscriptionPlanChangedEvent>
{
    public async Task Handle(SubscriptionPlanChangedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing quota sync for plan change on subscription {SubscriptionId}. " +
            "Tenant: {TenantId}, OldPlan: {OldPlanId}, NewPlan: {NewPlanId}",
            notification.SubscriptionId,
            notification.TenantId,
            notification.OldPlanId,
            notification.NewPlanId);

        // Load the new plan to get its limits
        var newPlan = await planRepository.GetByIdAsync(
            notification.NewPlanId,
            cancellationToken).ConfigureAwait(false);

        if (newPlan is null)
        {
            logger.LogWarning(
                "New subscription plan {NewPlanId} not found during quota sync. Skipping.",
                notification.NewPlanId);
            return;
        }

        var tenantId = notification.TenantId;
        var isUpgrade = notification.NewAmount.Amount > notification.OldAmount.Amount;

        // Sync quotas from new plan limits to tenant, capturing every per-quota outcome
        // so a partial failure cannot pass silently (a skipped downgrade limit would
        // leave the tenant on the old, higher quota).
        var quotaUpdates = new List<Task<(ResourceUsageType Type, Exception? Error)>>();

        Task<(ResourceUsageType, Exception?)> Enqueue(ResourceUsageType type, long hardLimit) =>
            TrySetQuotaAsync(tenantId, type, hardLimit, isUpgrade, cancellationToken);

        if (newPlan.MaxUsers.HasValue)
        {
            quotaUpdates.Add(Enqueue(ResourceUsageType.Users, newPlan.MaxUsers.Value));
        }

        if (newPlan.MaxStorageMb.HasValue)
        {
            // Convert MB to bytes for storage quota
            var storageBytesLimit = newPlan.MaxStorageMb.Value * 1024 * 1024;
            quotaUpdates.Add(Enqueue(ResourceUsageType.Storage, storageBytesLimit));
        }

        if (newPlan.MaxApiCallsPerMonth.HasValue)
        {
            quotaUpdates.Add(Enqueue(ResourceUsageType.ApiCalls, newPlan.MaxApiCallsPerMonth.Value));
        }

        // Wait for all quota updates to complete (the wrappers never throw)
        var outcomes = await Task.WhenAll(quotaUpdates).ConfigureAwait(false);

        var failures = outcomes.Where(outcome => outcome.Error is not null).ToList();
        if (failures.Count > 0)
        {
            // Surface the partial failure: quota limits were not fully applied for the new
            // plan, so the plan change cannot be reported as successful.
            throw new InvalidOperationException(
                $"Quota sync failed for plan change on subscription {notification.SubscriptionId}: " +
                $"{failures.Count}/{outcomes.Length} quota updates failed " +
                $"({string.Join(", ", failures.Select(f => f.Type))}). " +
                "Quotas must be re-synced or corrected manually.",
                new AggregateException(failures.Select(f => f.Error!)));
        }

        logger.LogInformation(
            "Quota sync completed for plan change on subscription {SubscriptionId}. " +
            "IsUpgrade: {IsUpgrade}, NewPlan: {PlanName}, " +
            "Users: {MaxUsers}, Storage: {MaxStorageMb}MB, ApiCalls: {MaxApiCalls}",
            notification.SubscriptionId,
            isUpgrade,
            newPlan.Name,
            newPlan.MaxUsers,
            newPlan.MaxStorageMb,
            newPlan.MaxApiCallsPerMonth);
    }

    /// <summary>
    ///     Applies one quota update. A failure is logged as an error and returned (never
    ///     thrown) so the handler can aggregate all failures and decide.
    /// </summary>
    private async Task<(ResourceUsageType Type, Exception? Error)> TrySetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        long hardLimit,
        bool isUpgrade,
        CancellationToken cancellationToken)
    {
        try
        {
            // Set soft limit at 80% of hard limit for warning notifications
            var softLimit = (long)(hardLimit * 0.8);

            await resourceQuotaService.SetQuotaAsync(
                tenantId,
                type,
                softLimit,
                hardLimit,
                ResourceQuotaPeriod.Monthly,
                cancellationToken).ConfigureAwait(false);

            var action = isUpgrade ? "Upgraded" : "Downgraded";
            logger.LogDebug(
                "{Action} {ResourceType} quota for tenant {TenantId}: soft={SoftLimit}, hard={HardLimit}",
                action,
                type,
                tenantId,
                softLimit,
                hardLimit);

            return (type, null);
        }
        catch (Exception ex)
        {
            // A failed quota update means the tenant keeps the previous limit — for
            // downgrades that is a security/compliance hole, so the failure is logged
            // as an error and re-surfaced as an aggregate failure by the handler.
            logger.LogError(
                ex,
                "Failed to update {ResourceType} quota for tenant {TenantId} during plan change",
                type,
                tenantId);

            return (type, ex);
        }
    }
}
