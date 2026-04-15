using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for DowngradeSubscriptionPlanCommand.
///     Delegates to the lifecycle service which handles the plan change and effective date logic.
/// </summary>
public sealed class DowngradeSubscriptionPlanCommandHandler(ISubscriptionLifecycleService lifecycleService)
    : ICommandHandler<DowngradeSubscriptionPlanCommand, SubscriptionDowngradeResult>
{
    public async Task<SubscriptionDowngradeResult> Handle(DowngradeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        return await lifecycleService.DowngradePlanAsync(request.SubscriptionId, request.NewPlanId, request.EffectiveDate, cancellationToken).ConfigureAwait(false);
    }
}
