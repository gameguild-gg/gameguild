

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of subscription downgrade. Promo fields (issue #418) are populated when
///     promo codes were supplied with the plan change.
/// </summary>
public class SubscriptionDowngradeResult
{
    public bool Success { get; init; }

    public Subscription? UpdatedSubscription { get; init; }

    public DateTime? EffectiveDate { get; init; }

    public Money? CreditIssued { get; init; }

    public string? FailureReason { get; init; }

    /// <summary>Total discount applied by promo codes to the downgrade charge (issue #418).</summary>
    public decimal PromoDiscountApplied { get; init; }

    /// <summary>Promo codes applied to the downgrade charge (issue #418).</summary>
    public IReadOnlyList<Commerce.SubscriptionPromoCodeApplication> AppliedPromoCodes { get; init; } = [];

    /// <summary>Promo codes that were quoted but not applied, with reasons (issue #418).</summary>
    public IReadOnlyList<Commerce.SubscriptionPromoRejectedCode> RejectedPromoCodes { get; init; } = [];

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

    /// <summary>
    ///     Returns a copy of this result carrying the promo application outcome
    ///     (issue #418).
    /// </summary>
    public SubscriptionDowngradeResult WithPromo(
        decimal discountTotal,
        IReadOnlyList<Commerce.SubscriptionPromoCodeApplication> appliedCodes,
        IReadOnlyList<Commerce.SubscriptionPromoRejectedCode> rejectedCodes)
    {
        return new SubscriptionDowngradeResult
        {
            Success = Success,
            UpdatedSubscription = UpdatedSubscription,
            EffectiveDate = EffectiveDate,
            CreditIssued = CreditIssued,
            FailureReason = FailureReason,
            PromoDiscountApplied = discountTotal,
            AppliedPromoCodes = appliedCodes,
            RejectedPromoCodes = rejectedCodes
        };
    }
}
