using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Applies subscription billing and lifecycle updates after a successful payment.
/// </summary>
public sealed class PaymentSubscriptionSyncService(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionBillingService billingService,
    ISubscriptionLifecycleService lifecycleService,
    ILogger<PaymentSubscriptionSyncService> logger) : IPaymentSubscriptionSyncService
{
    public async Task SyncSuccessfulPaymentAsync(
        string paymentReference,
        Guid? subscriptionId,
        decimal amount,
        string currency,
        DateTime processedAt,
        CancellationToken cancellationToken = default)
    {
        if (!subscriptionId.HasValue)
        {
            logger.LogDebug(
                "Skipping subscription sync for payment {PaymentReference} because it is not linked to a subscription.",
                paymentReference);
            return;
        }

        logger.LogInformation(
            "Recording successful payment {PaymentReference} for subscription {SubscriptionId}",
            paymentReference,
            subscriptionId.Value);

        var subscription = await subscriptionRepository.GetByIdAsync(
            subscriptionId.Value,
            cancellationToken).ConfigureAwait(false);

        if (subscription is null)
        {
            logger.LogWarning(
                "Subscription {SubscriptionId} was not found while syncing successful payment {PaymentReference}.",
                subscriptionId.Value,
                paymentReference);
            return;
        }

        subscription = await billingService.RecordPaymentAsync(
            subscription.Id,
            amount,
            currency,
            processedAt,
            cancellationToken).ConfigureAwait(false);

        switch (subscription.Status)
        {
            case SubscriptionStatus.PendingActivation:
                await lifecycleService.ActivateAsync(subscription.Id, cancellationToken).ConfigureAwait(false);
                break;

            case SubscriptionStatus.PastDue:
            case SubscriptionStatus.Suspended:
                await lifecycleService.ReactivateAsync(subscription.Id, cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
