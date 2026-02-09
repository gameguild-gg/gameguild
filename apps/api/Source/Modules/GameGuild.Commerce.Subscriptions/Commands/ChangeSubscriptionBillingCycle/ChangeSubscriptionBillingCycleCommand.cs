using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to change billing cycle
/// </summary>
public sealed record ChangeSubscriptionBillingCycleCommand(Guid SubscriptionId, BillingCycle NewBillingCycle) : ICommand;
