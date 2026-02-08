

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of subscription renewal
/// </summary>
public class SubscriptionRenewalResult
{
    /// <summary>
    ///     Whether the renewal was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Subscription ID that was renewed
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    ///     Updated subscription entity
    /// </summary>
    public Subscription? UpdatedSubscription { get; init; }

    /// <summary>
    ///     Next renewal date
    /// </summary>
    public DateTime? NextRenewalDate { get; init; }

    /// <summary>
    ///     Amount that was charged
    /// </summary>
    public Money? ChargedAmount { get; init; }

    /// <summary>
    ///     Current billing cycle count
    /// </summary>
    public int BillingCycleCount { get; init; }

    /// <summary>
    ///     Failure reason if renewal failed
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    ///     Payment result from renewal
    /// </summary>
    public PaymentResult? PaymentResult { get; init; }

    /// <summary>
    ///     Creates a successful renewal result
    /// </summary>
    public static SubscriptionRenewalResult CreateSuccess(Guid subscriptionId, int billingCycleCount, Money chargedAmount)
    {
        return new SubscriptionRenewalResult { Success = true, SubscriptionId = subscriptionId, BillingCycleCount = billingCycleCount, ChargedAmount = chargedAmount };
    }

    /// <summary>
    ///     Creates a failed renewal result
    /// </summary>
    public static SubscriptionRenewalResult Failed(Guid subscriptionId, string failureReason) { return new SubscriptionRenewalResult { Success = false, SubscriptionId = subscriptionId, FailureReason = failureReason }; }
}
