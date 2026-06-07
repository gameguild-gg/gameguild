using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Bridges successful payment events back into subscription billing state.
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
        if (!subscriptionId.HasValue || subscriptionId.Value == Guid.Empty)
        {
            logger.LogDebug("Payment {PaymentId} has no subscription to synchronize", paymentId);
            return;
        }

        var subscription = await subscriptionRepository.GetByIdAsync(subscriptionId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null)
        {
            logger.LogWarning("Payment {PaymentId} referenced missing subscription {SubscriptionId}",
                paymentId, subscriptionId.Value);
            return;
        }

        var result = subscription.RecordPayment(
            amount,
            currency,
            processedAt,
            paymentId.ToString("N"));

        if (!result.IsSuccess)
        {
            logger.LogInformation(
                "Payment {PaymentId} was already synchronized or rejected for subscription {SubscriptionId}: {Reason}",
                paymentId,
                subscriptionId.Value,
                result.Message);
            return;
        }

        await subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }
}
