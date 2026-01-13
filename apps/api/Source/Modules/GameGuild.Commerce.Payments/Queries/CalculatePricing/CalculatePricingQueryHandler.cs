using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CalculatePricingQuery
/// </summary>
public sealed class CalculatePricingQueryHandler : IQueryHandler<CalculatePricingQuery, PricingCalculationResult>
{
    // TODO: Inject IPricingService when Subscriptions module is available

    public Task<PricingCalculationResult> Handle(CalculatePricingQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implement pricing calculation logic
        // This will require:
        // 1. Fetching subscription plan from Subscriptions module
        // 2. Applying pricing rules (PricingRule entity)
        // 3. Applying discount codes if provided
        // 4. Calculating final price with promo stacking rules

        throw new NotImplementedException("This feature requires Subscriptions module integration");
    }
}
