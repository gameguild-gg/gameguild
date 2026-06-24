using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Bridges successful payment events into subscription billing state.
/// </summary>
public sealed class PaymentSubscriptionSyncService(
    ISubscriptionRepository subscriptionRepository,
    ILogger<PaymentSubscriptionSyncService> logger) : IPaymentSubscriptionSyncService
{
    public async Task SyncSuccessfulPaymentAsync(
        Guid paymentId,
        Guid? subscriptionId,
        decimal amount,
        string currency,
        DateTime processedAt,
        CancellationToken cancellationToken = default)
    {
        if (!subscriptionId.HasValue)
        {
            logger.LogDebug("Payment {PaymentId} has no subscription link; skipping subscription sync", paymentId);
            return;
        }

        var subscription = await subscriptionRepository.GetByIdAsync(subscriptionId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null)
        {
            logger.LogWarning(
                "Payment {PaymentId} references missing subscription {SubscriptionId}; skipping subscription sync",
                paymentId,
                subscriptionId.Value);
            return;
        }

        var idempotencyKey = $"payment:{paymentId}";
        var result = subscription.RecordPayment(amount, currency, processedAt, idempotencyKey);

        if (result.IsSuccess)
        {
            await subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Payment {PaymentId} synced to subscription {SubscriptionId}",
                paymentId,
                subscriptionId.Value);
            return;
        }

        if (result.IsAlreadyProcessed)
        {
            logger.LogInformation(
                "Payment {PaymentId} was already synced to subscription {SubscriptionId}",
                paymentId,
                subscriptionId.Value);
            return;
        }

        logger.LogWarning(
            "Payment {PaymentId} could not be synced to subscription {SubscriptionId}: {Reason}",
            paymentId,
            subscriptionId.Value,
            result.Message);
    }
}
