using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to record a payment failure for a subscription
/// </summary>
public record RecordSubscriptionPaymentFailureCommand(Guid SubscriptionId, string Reason, DateTime FailureDate) : ICommand;
