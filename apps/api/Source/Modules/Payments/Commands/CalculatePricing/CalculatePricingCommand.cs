using GameGuild.CQRS;
using GameGuild.Infrastructure.Common.ValueObjects;
using GameGuild.Infrastructure.CQRS.Abstractions;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Commands.CalculatePricing;

/// <summary>
/// Command to calculate pricing for a product or service
/// </summary>
public record CalculatePricingCommand : ICommand<PricingCalculationResult>
{
    /// <summary>
    /// Product ID for pricing calculation (optional)
    /// </summary>
    public Guid? ProductId { get; init; }

    /// <summary>
    /// Subscription plan ID for pricing calculation (optional)
    /// </summary>
    public Guid? PlanId { get; init; }

    /// <summary>
    /// Base amount if no product/plan specified
    /// </summary>
    public Money? BaseAmount { get; init; }

    /// <summary>
    /// User for personalized pricing (optional)
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Tenant for tenant-specific pricing (optional)
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Discount code to apply
    /// </summary>
    public string? DiscountCode { get; init; }

    /// <summary>
    /// Billing cycle (monthly, yearly, etc.)
    /// </summary>
    public string? BillingCycle { get; init; }

    /// <summary>
    /// Currency for pricing calculation
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Region for tax calculation
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Quantity of items
    /// </summary>
    public int Quantity { get; init; } = 1;

    /// <summary>
    /// Additional add-ons or features
    /// </summary>
    public List<Guid> AddOnIds { get; init; } = new List<Guid>();

    /// <summary>
    /// Whether to include tax in calculation
    /// </summary>
    public bool IncludeTax { get; init; } = true;

    /// <summary>
    /// Whether this is a renewal pricing calculation
    /// </summary>
    public bool IsRenewal { get; init; } = false;

    /// <summary>
    /// Whether this is an upgrade/downgrade
    /// </summary>
    public bool IsChange { get; init; } = false;

    /// <summary>
    /// Previous plan ID for upgrade/downgrade calculations
    /// </summary>
    public Guid? PreviousPlanId { get; init; }

    /// <summary>
    /// Whether to apply prorated pricing for changes
    /// </summary>
    public bool ApplyProration { get; init; } = true;

    /// <summary>
    /// Proration date for calculations
    /// </summary>
    public DateTime? ProrationDate { get; init; }

    /// <summary>
    /// Validity period for the pricing quote
    /// </summary>
    public TimeSpan? ValidityPeriod { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Additional metadata for pricing context
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
