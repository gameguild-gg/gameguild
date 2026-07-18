

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
    ///     Whether provider payment confirmation is required before renewal can complete.
    /// </summary>
    public bool PaymentRequired { get; init; }

    /// <summary>
    ///     Specific billing cycle that the provider payment must confirm.
    /// </summary>
    public int? RequiredBillingCycle { get; init; }

    /// <summary>
    ///     Authoritative amount due for the required billing cycle. This is not recognized revenue.
    /// </summary>
    public Money? AmountDue { get; init; }

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
    ///     Creates a non-mutating renewal preparation result awaiting provider payment confirmation.
    /// </summary>
    public static SubscriptionRenewalResult RequiresPayment(
        Guid subscriptionId,
        int billingCycleCount,
        int requiredBillingCycle,
        Money amountDue)
    {
        return new SubscriptionRenewalResult
        {
            Success = false,
            SubscriptionId = subscriptionId,
            BillingCycleCount = billingCycleCount,
            PaymentRequired = true,
            RequiredBillingCycle = requiredBillingCycle,
            AmountDue = amountDue,
            FailureReason = $"Provider payment confirmation is required for billing cycle {requiredBillingCycle}; renewal quote is {amountDue}"
        };
    }

    /// <summary>
    ///     Creates a failed renewal result
    /// </summary>
    public static SubscriptionRenewalResult Failed(Guid subscriptionId, string failureReason) { return new SubscriptionRenewalResult { Success = false, SubscriptionId = subscriptionId, FailureReason = failureReason }; }
}
