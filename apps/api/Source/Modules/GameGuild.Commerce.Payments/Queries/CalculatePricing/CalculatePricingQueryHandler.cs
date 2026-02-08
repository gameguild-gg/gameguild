using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CalculatePricingQuery.
///     Uses IPlanPricingResolver to fetch plan pricing without direct Subscriptions module dependency.
/// </summary>
/// <remarks>
///     Pricing calculation workflow:
///     1. Fetch subscription plan pricing via IPlanPricingResolver
///     2. Apply pricing rules based on billing cycle
///     3. Apply discount codes if provided
///     4. Return final calculated price
/// </remarks>
public sealed class CalculatePricingQueryHandler : IQueryHandler<CalculatePricingQuery, PricingCalculationResult>
{
    private readonly IPlanPricingResolver _pricingResolver;

    public CalculatePricingQueryHandler(IPlanPricingResolver pricingResolver)
    {
        _pricingResolver = pricingResolver ?? throw new ArgumentNullException(nameof(pricingResolver));
    }

    public async Task<PricingCalculationResult> Handle(CalculatePricingQuery request, CancellationToken cancellationToken)
    {
        // Check if plan exists
        var planExists = await _pricingResolver.PlanExistsAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (!planExists)
        {
            throw new InvalidOperationException($"Subscription plan {request.PlanId} not found");
        }

        // Get base pricing from plan
        var basePrice = await _pricingResolver.GetPlanMonthlyPriceAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (basePrice == null)
        {
            throw new InvalidOperationException($"Unable to retrieve pricing for plan {request.PlanId}");
        }
        var discount = Money.Zero();
        var appliedDiscounts = new List<AppliedDiscount>();

        // Apply discount code if provided
        if (!string.IsNullOrEmpty(request.DiscountCode))
        {
            // PLANNED: Integrate with discount/promo code service when available (depends on GameGuild.Commerce.Promotions)
            // For now, apply a placeholder discount calculation
            // This should be replaced with actual promo code validation and application
            // Example: var promoResult = await _promoService.ValidateAndApplyAsync(request.DiscountCode, basePrice, cancellationToken);
        }

        // Calculate final price
        var totalPrice = basePrice - discount;

        return new ConcretePricingCalculationResult
        {
            BasePrice = basePrice,
            AddOnPrice = Money.Zero(),
            Discount = discount,
            Tax = Money.Zero(), // Tax calculation would integrate with tax service
            TotalPrice = totalPrice,
            BillingCycle = BillingCycle.Monthly,
            AppliedDiscounts = appliedDiscounts
        };
    }
}

/// <summary>
///     Concrete implementation of PricingCalculationResult for instantiation.
/// </summary>
internal sealed class ConcretePricingCalculationResult : PricingCalculationResult;
