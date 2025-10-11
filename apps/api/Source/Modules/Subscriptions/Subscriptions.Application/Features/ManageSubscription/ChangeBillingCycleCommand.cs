using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to change billing cycle
/// </summary>
public record ChangeBillingCycleCommand(
    Guid SubscriptionId,
    BillingCycle NewBillingCycle
) : ICommand;

