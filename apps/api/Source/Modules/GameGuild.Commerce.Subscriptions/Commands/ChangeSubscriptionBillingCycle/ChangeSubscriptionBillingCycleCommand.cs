using GameGuild.CQRS;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to change billing cycle
/// </summary>
public record ChangeSubscriptionBillingCycleCommand(Guid SubscriptionId, BillingCycle NewBillingCycle) : ICommand;
