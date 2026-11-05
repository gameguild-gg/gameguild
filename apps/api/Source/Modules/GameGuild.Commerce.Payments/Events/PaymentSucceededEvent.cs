using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Domain event raised when a payment succeeds.
/// </summary>
public sealed class PaymentSucceededEvent(
    Guid paymentId,
    Guid tenantId,
    Guid? subscriptionId,
    decimal amount,
    string currency,
    DateTime processedAt) : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;

    public Guid TenantId { get; } = tenantId;

    public Guid? SubscriptionId { get; } = subscriptionId;

    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;

    public DateTime ProcessedAt { get; } = processedAt;
}