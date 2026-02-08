using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to change billing cycle
/// </summary>
public record ChangeSubscriptionBillingCycleCommand(Guid SubscriptionId, BillingCycle NewBillingCycle) : ICommand;
