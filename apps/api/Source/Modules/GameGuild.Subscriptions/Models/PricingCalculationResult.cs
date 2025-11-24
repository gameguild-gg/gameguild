namespace GameGuild.Subscriptions.Models;

/// <summary>
///     Result of pricing calculation
/// </summary>
public class PricingCalculationResult
{
    /// <summary>
    ///     Base amount for the plan
    /// </summary>
    public Money BaseAmount { get; init; } = Money.Zero();

    /// <summary>
    ///     Discount amount applied
    /// </summary>
    public Money DiscountAmount { get; init; } = Money.Zero();

    /// <summary>
    ///     Tax amount
    /// </summary>
    public Money TaxAmount { get; init; } = Money.Zero();

    /// <summary>
    ///     Add-ons total amount
    /// </summary>
    public Money AddOnsAmount { get; init; } = Money.Zero();

    /// <summary>
    ///     Total amount after all calculations
    /// </summary>
    public Money TotalAmount { get; init; } = Money.Zero();

    /// <summary>
    ///     Billing cycle for this pricing
    /// </summary>
    public BillingCycle BillingCycle { get; init; }

    /// <summary>
    ///     Applied discounts
    /// </summary>
    public List<AppliedDiscount> AppliedDiscounts { get; init; } = new List<AppliedDiscount>();

    /// <summary>
    ///     Add-ons included in calculation
    /// </summary>
    public Dictionary<string, PricingAddOn> AddOns { get; init; } = new Dictionary<string, PricingAddOn>();

    /// <summary>
    ///     Tax details
    /// </summary>
    public TaxDetails? TaxDetails { get; init; }

    /// <summary>
    ///     Currency used for calculation
    /// </summary>
    public string Currency { get => TotalAmount.Currency; }

    /// <summary>
    ///     Creates a simple pricing result
    /// </summary>
    public static PricingCalculationResult Simple(Money baseAmount, BillingCycle billingCycle) { return new PricingCalculationResult { BaseAmount = baseAmount, TotalAmount = baseAmount, BillingCycle = billingCycle }; }

    /// <summary>
    ///     Creates a pricing result with discount
    /// </summary>
    public static PricingCalculationResult WithDiscount(Money baseAmount, Money discountAmount, BillingCycle billingCycle, List<AppliedDiscount>? appliedDiscounts = null)
    {
        return new PricingCalculationResult
        {
            BaseAmount = baseAmount, DiscountAmount = discountAmount, TotalAmount = baseAmount - discountAmount, BillingCycle = billingCycle, AppliedDiscounts = appliedDiscounts ?? new List<AppliedDiscount>()
        };
    }
}
