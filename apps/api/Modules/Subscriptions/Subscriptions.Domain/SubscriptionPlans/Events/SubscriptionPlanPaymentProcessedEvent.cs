using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Events;

/// <summary>
///     Domain event raised when a payment is processed for a subscription plan
/// </summary>
public class SubscriptionPlanPaymentProcessedEvent : DomainEvent {
    public SubscriptionPlanPaymentProcessedEvent(
      Guid subscriptionId,
      Guid tenantId,
      string transactionId,
      Money amount,
      PaymentStatus status,
      string? failureReason = null)
      : base(subscriptionId, "Subscription") {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        TransactionId = transactionId;
        Amount = amount;
        Status = status;
        FailureReason = failureReason;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public string TransactionId { get; }

    public Money Amount { get; }

    public PaymentStatus Status { get; }

    public string? FailureReason { get; }
}

