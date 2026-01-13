
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of subscription downgrade
/// </summary>
public class SubscriptionDowngradeResult
{
    public bool Success { get; init; }

    public Subscription? UpdatedSubscription { get; init; }

    public DateTime? EffectiveDate { get; init; }

    public Money? CreditIssued { get; init; }

    public string? FailureReason { get; init; }

    /// <summary>
    ///     Creates a successful downgrade result
    /// </summary>
    public static SubscriptionDowngradeResult CreateSuccess(Subscription updatedSubscription, DateTime? effectiveDate = null, Money? creditIssued = null)
    {
        return new SubscriptionDowngradeResult { Success = true, UpdatedSubscription = updatedSubscription, EffectiveDate = effectiveDate, CreditIssued = creditIssued };
    }

    /// <summary>
    ///     Creates a failed downgrade result
    /// </summary>
    public static SubscriptionDowngradeResult Failed(string failureReason) { return new SubscriptionDowngradeResult { Success = false, FailureReason = failureReason }; }
}
