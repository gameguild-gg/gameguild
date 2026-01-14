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
/// </remarks>
public class SubscriptionPlanChangedQuotaSyncHandler(
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
            cancellationToken);

        if (newPlan is null)
        {
            logger.LogWarning(
                "New subscription plan {NewPlanId} not found during quota sync. Skipping.",
                notification.NewPlanId);
            return;
        }

        var tenantId = notification.TenantId;
        var isUpgrade = notification.NewAmount.Amount > notification.OldAmount.Amount;

        // Sync quotas from new plan limits to tenant
        var syncTasks = new List<Task>();

        if (newPlan.MaxUsers.HasValue)
        {
            syncTasks.Add(SetQuotaAsync(
                tenantId,
                ResourceUsageType.Users,
                newPlan.MaxUsers.Value,
                isUpgrade,
                cancellationToken));
        }

        if (newPlan.MaxStorageMb.HasValue)
        {
            // Convert MB to bytes for storage quota
            var storageBytesLimit = newPlan.MaxStorageMb.Value * 1024 * 1024;
            syncTasks.Add(SetQuotaAsync(
                tenantId,
                ResourceUsageType.Storage,
                storageBytesLimit,
                isUpgrade,
                cancellationToken));
        }

        if (newPlan.MaxApiCallsPerMonth.HasValue)
        {
            syncTasks.Add(SetQuotaAsync(
                tenantId,
                ResourceUsageType.ApiCalls,
                newPlan.MaxApiCallsPerMonth.Value,
                isUpgrade,
                cancellationToken));
        }

        // Wait for all quota updates to complete
        await Task.WhenAll(syncTasks);

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

    private async Task SetQuotaAsync(
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
                cancellationToken);

            var action = isUpgrade ? "Upgraded" : "Downgraded";
            logger.LogDebug(
                "{Action} {ResourceType} quota for tenant {TenantId}: soft={SoftLimit}, hard={HardLimit}",
                action,
                type,
                tenantId,
                softLimit,
                hardLimit);
        }
        catch (Exception ex)
        {
            // Log but don't fail the entire sync if one quota fails
            // This is a best-effort sync; quotas can be manually corrected
            // For downgrades, this is especially important - we don't want to block
            // the plan change if quota update fails
            logger.LogError(
                ex,
                "Failed to update {ResourceType} quota for tenant {TenantId} during plan change",
                type,
                tenantId);
        }
    }
}
