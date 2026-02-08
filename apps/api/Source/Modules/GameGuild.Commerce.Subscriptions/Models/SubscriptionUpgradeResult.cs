

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of subscription upgrade
/// </summary>
public class SubscriptionUpgradeResult
{
    public bool Success { get; init; }

    public Subscription? UpdatedSubscription { get; init; }

    public Money? ProratedAmount { get; init; }

    public Money? CreditApplied { get; init; }

    public string? FailureReason { get; init; }

    /// <summary>
    ///     Creates a successful upgrade result
    /// </summary>
    public static SubscriptionUpgradeResult CreateSuccess(Subscription updatedSubscription, Money? proratedAmount = null, Money? creditApplied = null)
    {
        return new SubscriptionUpgradeResult { Success = true, UpdatedSubscription = updatedSubscription, ProratedAmount = proratedAmount, CreditApplied = creditApplied };
    }

    /// <summary>
    ///     Creates a failed upgrade result
    /// </summary>
    public static SubscriptionUpgradeResult Failed(string failureReason) { return new SubscriptionUpgradeResult { Success = false, FailureReason = failureReason }; }
}
