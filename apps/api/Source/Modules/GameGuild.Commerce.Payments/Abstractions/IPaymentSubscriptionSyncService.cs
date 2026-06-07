namespace GameGuild.Commerce.Payments;

/// <summary>
///     Synchronizes subscription state after payment success.
/// </summary>
public interface IPaymentSubscriptionSyncService
{
    Task SyncSuccessfulPaymentAsync(
        Guid paymentId,
        Guid? subscriptionId,
        decimal amount,
        string currency,
        DateTime processedAt,
        CancellationToken cancellationToken = default);
}
