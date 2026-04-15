using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for UpgradeSubscriptionPlanCommand.
///     Delegates to the lifecycle service which handles proration and plan change logic.
/// </summary>
public sealed class UpgradeSubscriptionPlanCommandHandler(ISubscriptionLifecycleService lifecycleService)
    : ICommandHandler<UpgradeSubscriptionPlanCommand, SubscriptionUpgradeResult>
{
    public async Task<SubscriptionUpgradeResult> Handle(UpgradeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        return await lifecycleService.UpgradePlanAsync(request.SubscriptionId, request.NewPlanId, request.EffectiveDate, cancellationToken).ConfigureAwait(false);
    }
}
