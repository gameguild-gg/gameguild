

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of subscription upgrade. Promo fields (issue #418) are populated when
///     promo codes were applied to the prorated upgrade charge.
/// </summary>
public class SubscriptionUpgradeResult
{
    public bool Success { get; init; }

    public Subscription? UpdatedSubscription { get; init; }

    public Money? ProratedAmount { get; init; }

    public Money? CreditApplied { get; init; }

    public string? FailureReason { get; init; }

    /// <summary>Prorated charge after promo discounts (issue #418; null when no promo applied).</summary>
    public Money? DiscountedProratedAmount { get; init; }

    /// <summary>Total discount applied by promo codes to the prorated charge (issue #418).</summary>
    public decimal PromoDiscountApplied { get; init; }

    /// <summary>Promo codes applied to the upgrade charge (issue #418).</summary>
    public IReadOnlyList<Commerce.SubscriptionPromoCodeApplication> AppliedPromoCodes { get; init; } = [];

    /// <summary>Promo codes that were quoted but not applied, with reasons (issue #418).</summary>
    public IReadOnlyList<Commerce.SubscriptionPromoRejectedCode> RejectedPromoCodes { get; init; } = [];

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

    /// <summary>
    ///     Returns a copy of this successful result carrying the promo application outcome
    ///     (issue #418): the discounted prorated charge, the discount total, and the
    ///     applied/rejected code lists.
    /// </summary>
    public SubscriptionUpgradeResult WithPromo(
        Money discountedProratedAmount,
        decimal discountTotal,
        IReadOnlyList<Commerce.SubscriptionPromoCodeApplication> appliedCodes,
        IReadOnlyList<Commerce.SubscriptionPromoRejectedCode> rejectedCodes)
    {
        return new SubscriptionUpgradeResult
        {
            Success = Success,
            UpdatedSubscription = UpdatedSubscription,
            ProratedAmount = ProratedAmount,
            CreditApplied = CreditApplied,
            FailureReason = FailureReason,
            DiscountedProratedAmount = discountedProratedAmount,
            PromoDiscountApplied = discountTotal,
            AppliedPromoCodes = appliedCodes,
            RejectedPromoCodes = rejectedCodes
        };
    }
}
