using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to record a successful payment for a subscription
/// </summary>
/// <param name="SubscriptionId">The subscription to record payment for</param>
/// <param name="Amount">Payment amount</param>
/// <param name="Currency">Currency code</param>
/// <param name="PaymentDate">When the payment was processed</param>
/// <param name="IdempotencyKey">Unique payment key (e.g., external payment ID from provider)</param>
/// <param name="ForBillingCycle">Optional billing cycle this payment is for (prevents out-of-order issues)</param>
public sealed record RecordSubscriptionPaymentCommand(
    Guid SubscriptionId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string IdempotencyKey,
    int? ForBillingCycle = null) : ICommand<PaymentRecordResult>;
