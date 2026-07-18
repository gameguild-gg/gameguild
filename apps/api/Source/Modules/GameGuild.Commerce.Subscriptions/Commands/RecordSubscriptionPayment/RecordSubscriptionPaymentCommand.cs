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
/// <param name="ForBillingCycle">Specific billing cycle this payment is for</param>
public sealed record RecordSubscriptionPaymentCommand(
    Guid SubscriptionId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string IdempotencyKey,
    int ForBillingCycle = 0) : ICommand<PaymentRecordResult>;
