using GameGuild.Modules.Payments.Models;
using GameGuild.Shared;

namespace GameGuild.Modules.Payments.Models;

/// <summary>
///     Result of pricing calculation
/// </summary>
public class PricingCalculationResult
{
    public Money BasePrice { get; init; } = Money.Zero();

    public Money AddOnPrice { get; init; } = Money.Zero();

    public Money Discount { get; init; } = Money.Zero();

    public Money Tax { get; init; } = Money.Zero();

    public Money TotalPrice { get; init; } = Money.Zero();

    public BillingCycle BillingCycle { get; init; }

    public Dictionary<string, Money> AddOnBreakdown { get; init; } = new Dictionary<string, Money>();

    public List<AppliedDiscount> AppliedDiscounts { get; init; } = new List<AppliedDiscount>();
}

