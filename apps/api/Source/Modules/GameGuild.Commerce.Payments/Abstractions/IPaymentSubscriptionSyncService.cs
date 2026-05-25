namespace GameGuild.Commerce.Payments;

/// <summary>
///     Synchronizes subscription state after a successful payment.
/// </summary>
public interface IPaymentSubscriptionSyncService
{
    Task SyncSuccessfulPaymentAsync(
        string paymentReference,
        Guid? subscriptionId,
        decimal amount,
        string currency,
        DateTime processedAt,
        CancellationToken cancellationToken = default);
}
