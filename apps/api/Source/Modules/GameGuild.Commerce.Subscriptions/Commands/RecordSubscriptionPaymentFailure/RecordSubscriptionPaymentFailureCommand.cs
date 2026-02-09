using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to record a payment failure for a subscription
/// </summary>
public sealed record RecordSubscriptionPaymentFailureCommand(Guid SubscriptionId, string Reason, DateTime FailureDate) : ICommand;
