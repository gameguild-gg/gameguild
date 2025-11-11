using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to record a successful payment for a subscription
/// </summary>
public record RecordSubscriptionPaymentCommand(Guid SubscriptionId, decimal Amount, string Currency, DateTime PaymentDate) : ICommand;
