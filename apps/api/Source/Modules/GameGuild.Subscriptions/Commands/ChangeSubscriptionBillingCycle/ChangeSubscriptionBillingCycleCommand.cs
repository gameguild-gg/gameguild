using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to change billing cycle
/// </summary>
public record ChangeSubscriptionBillingCycleCommand(Guid SubscriptionId, BillingCycle NewBillingCycle) : ICommand;
