namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Service interface for subscription-related notifications.
///     Handles renewal reminders, trial expiration warnings, and payment failure notifications.
/// </summary>
public interface ISubscriptionNotificationService
{
    /// <summary>
    ///     Sends a renewal reminder notification for a subscription.
    /// </summary>
    /// <param name="subscription">The subscription due for renewal</param>
    /// <param name="daysUntilRenewal">Number of days until renewal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendRenewalReminderAsync(Subscription subscription, int daysUntilRenewal, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a trial expiration reminder notification.
    /// </summary>
    /// <param name="subscription">The subscription with an expiring trial</param>
    /// <param name="daysUntilExpiration">Number of days until trial expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendTrialExpirationReminderAsync(Subscription subscription, int daysUntilExpiration, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a payment failure notification.
    /// </summary>
    /// <param name="subscription">The subscription with a failed payment</param>
    /// <param name="failureReason">The reason for the payment failure</param>
    /// <param name="retryAttempt">The current retry attempt number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPaymentFailureNotificationAsync(Subscription subscription, string failureReason, int retryAttempt, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a subscription activated notification.
    /// </summary>
    /// <param name="subscription">The activated subscription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendSubscriptionActivatedNotificationAsync(Subscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a subscription cancelled notification.
    /// </summary>
    /// <param name="subscription">The cancelled subscription</param>
    /// <param name="cancellationReason">The reason for cancellation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendSubscriptionCancelledNotificationAsync(Subscription subscription, CancellationReason cancellationReason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a subscription suspended notification (e.g., due to payment failure).
    /// </summary>
    /// <param name="subscription">The suspended subscription</param>
    /// <param name="suspensionReason">The reason for suspension</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendSubscriptionSuspendedNotificationAsync(Subscription subscription, string? suspensionReason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a subscription reactivated notification.
    /// </summary>
    /// <param name="subscription">The reactivated subscription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendSubscriptionReactivatedNotificationAsync(Subscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a plan upgrade notification.
    /// </summary>
    /// <param name="subscription">The upgraded subscription</param>
    /// <param name="oldPlanId">The previous plan ID</param>
    /// <param name="newPlanId">The new plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPlanUpgradeNotificationAsync(Subscription subscription, Guid oldPlanId, Guid newPlanId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a plan downgrade notification.
    /// </summary>
    /// <param name="subscription">The downgraded subscription</param>
    /// <param name="oldPlanId">The previous plan ID</param>
    /// <param name="newPlanId">The new plan ID</param>
    /// <param name="effectiveDate">When the downgrade takes effect</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPlanDowngradeNotificationAsync(Subscription subscription, Guid oldPlanId, Guid newPlanId, DateTime effectiveDate, CancellationToken cancellationToken = default);
}
