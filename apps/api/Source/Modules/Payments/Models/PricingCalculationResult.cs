using GameGuild;

namespace GameGuild.Modules.Payments.Models;

/// <summary>
/// Result of pricing calculations
/// </summary>
public class PricingCalculationResult
{
    /// <summary>
    /// Base price before any modifications
    /// </summary>
    public Money BasePrice { get; init; } = Money.Zero();

    /// <summary>
    /// Additional charges (add-ons, upgrades, etc.)
    /// </summary>
    public Money AdditionalCharges { get; init; } = Money.Zero();

    /// <summary>
    /// Total discount applied
    /// </summary>
    public Money Discount { get; init; } = Money.Zero();

    /// <summary>
    /// Tax amount
    /// </summary>
    public Money Tax { get; init; } = Money.Zero();

    /// <summary>
    /// Processing fees
    /// </summary>
    public Money ProcessingFees { get; init; } = Money.Zero();

    /// <summary>
    /// Final total price
    /// </summary>
    public Money TotalPrice { get; init; } = Money.Zero();

    /// <summary>
    /// Billing cycle information
    /// </summary>
    public string? BillingCycle { get; init; }

    /// <summary>
    /// Breakdown of additional charges
    /// </summary>
    public Dictionary<string, Money> ChargeBreakdown { get; init; } = new Dictionary<string, Money>();

    /// <summary>
    /// Applied discounts with details
    /// </summary>
    public List<AppliedDiscount> AppliedDiscounts { get; init; } = new List<AppliedDiscount>();

    /// <summary>
    /// Tax breakdown by region/type
    /// </summary>
    public Dictionary<string, Money> TaxBreakdown { get; init; } = new Dictionary<string, Money>();

    /// <summary>
    /// Currency used in calculation
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Exchange rate used (if applicable)
    /// </summary>
    public decimal? ExchangeRate { get; init; }

    /// <summary>
    /// When this pricing was calculated
    /// </summary>
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// How long this pricing is valid for
    /// </summary>
    public TimeSpan? ValidityPeriod { get; init; }

    /// <summary>
    /// When this pricing expires
    /// </summary>
    public DateTime? ExpiresAt => ValidityPeriod.HasValue ? CalculatedAt.Add(ValidityPeriod.Value) : null;

    /// <summary>
    /// Whether this pricing is still valid
    /// </summary>
    public bool IsValid => !ExpiresAt.HasValue || DateTime.UtcNow < ExpiresAt.Value;

    /// <summary>
    /// Create a simple pricing calculation
    /// </summary>
    public static PricingCalculationResult Create(
        Money basePrice,
        Money? discount = null,
        Money? tax = null,
        string currency = "USD",
        TimeSpan? validityPeriod = null)
    {
        discount ??= Money.Zero();
        tax ??= Money.Zero();

        var totalPrice = basePrice - discount + tax;

        return new PricingCalculationResult
        {
            BasePrice = basePrice,
            Discount = discount,
            Tax = tax,
            TotalPrice = totalPrice,
            Currency = currency,
            ValidityPeriod = validityPeriod
        };
    }
}
