using GameGuild.Commerce.Subscriptions;
using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.API.Integration;

/// <summary>
///     Cross-module event handler that syncs resource quotas when a subscription is activated.
///     Economic invariant: Activated subscription → Tenant quotas reflect plan limits.
/// </summary>
/// <remarks>
///     This handler coordinates between Commerce.Subscriptions and Resources modules,
///     residing in the API composition root to maintain module independence.
/// </remarks>
public sealed class SubscriptionActivatedQuotaSyncHandler(
    ISubscriptionRepository subscriptionRepository,
    IResourceQuotaService resourceQuotaService,
    ILogger<SubscriptionActivatedQuotaSyncHandler> logger
) : INotificationHandler<SubscriptionActivatedEvent>
{
    public async Task Handle(SubscriptionActivatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing quota sync for activated subscription {SubscriptionId} on tenant {TenantId}",
            notification.SubscriptionId,
            notification.TenantId);

        // Load subscription with plan details (Plan is eagerly loaded by repository)
        var subscription = await subscriptionRepository.GetByIdAsync(
            notification.SubscriptionId,
            cancellationToken);

        if (subscription is null)
        {
            logger.LogWarning(
                "Subscription {SubscriptionId} not found during quota sync. Skipping.",
                notification.SubscriptionId);
            return;
        }

        if (subscription.Plan is null)
        {
            logger.LogWarning(
                "Subscription {SubscriptionId} has no plan loaded. Skipping quota sync.",
                notification.SubscriptionId);
            return;
        }

        var plan = subscription.Plan;
        var tenantId = notification.TenantId;

        // Sync quotas from plan limits to tenant
        // Only set quotas that have defined limits in the plan
        var syncTasks = new List<Task>();

        if (plan.MaxUsers.HasValue)
        {
            syncTasks.Add(SetQuotaAsync(
                tenantId,
                ResourceUsageType.Users,
                plan.MaxUsers.Value,
                cancellationToken));
        }

        if (plan.MaxStorageMb.HasValue)
        {
            // Convert MB to bytes for storage quota
            var storageBytesLimit = plan.MaxStorageMb.Value * 1024 * 1024;
            syncTasks.Add(SetQuotaAsync(
                tenantId,
                ResourceUsageType.Storage,
                storageBytesLimit,
                cancellationToken));
        }

        if (plan.MaxApiCallsPerMonth.HasValue)
        {
            syncTasks.Add(SetQuotaAsync(
                tenantId,
                ResourceUsageType.ApiCalls,
                plan.MaxApiCallsPerMonth.Value,
                cancellationToken));
        }

        // Wait for all quota updates to complete
        await Task.WhenAll(syncTasks);

        logger.LogInformation(
            "Quota sync completed for subscription {SubscriptionId}. " +
            "Users: {MaxUsers}, Storage: {MaxStorageMb}MB, ApiCalls: {MaxApiCalls}",
            notification.SubscriptionId,
            plan.MaxUsers,
            plan.MaxStorageMb,
            plan.MaxApiCallsPerMonth);
    }

    private async Task SetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        long hardLimit,
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

            logger.LogDebug(
                "Set {ResourceType} quota for tenant {TenantId}: soft={SoftLimit}, hard={HardLimit}",
                type,
                tenantId,
                softLimit,
                hardLimit);
        }
        catch (Exception ex)
        {
            // Log but don't fail the entire sync if one quota fails
            // This is a best-effort sync; quotas can be manually corrected
            logger.LogError(
                ex,
                "Failed to set {ResourceType} quota for tenant {TenantId}",
                type,
                tenantId);
        }
    }
}
