using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription payment is successfully processed
/// </summary>
public sealed class SubscriptionPaymentProcessedEvent(Guid subscriptionId, Guid tenantId, decimal amount, string currency, DateTime paymentDate) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;

    public DateTime PaymentDate { get; } = paymentDate;
}
