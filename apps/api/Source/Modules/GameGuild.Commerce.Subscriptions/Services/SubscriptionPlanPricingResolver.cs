using GameGuild.Commerce.Payments;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions.Services;

/// <summary>
///     Adapter implementation of <see cref="IPlanPricingResolver"/> that wraps the Subscriptions module's
///     plan service to provide pricing information to the Payments module.
/// </summary>
/// <remarks>
///     This implementation follows the Adapter pattern, translating between the Subscriptions
///     module's domain model and the Payments module's pricing abstraction.
/// </remarks>
public sealed class SubscriptionPlanPricingResolver : IPlanPricingResolver
{
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionPlanPricingResolver(ISubscriptionPlanService planService)
    {
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
    }

    /// <inheritdoc />
    public async Task<Money?> GetPlanMonthlyPriceAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planService.GetByIdAsync(planId, cancellationToken);
        
        if (plan is null)
            return null;

        return Money.FromCents(plan.MonthlyPriceInCents, plan.Currency);
    }

    /// <inheritdoc />
    public async Task<Money?> GetPlanPriceAsync(Guid planId, BillingCycle billingCycle, CancellationToken cancellationToken = default)
    {
        var plan = await _planService.GetByIdAsync(planId, cancellationToken);
        
        if (plan is null)
            return null;

        var priceInCents = billingCycle switch
        {
            BillingCycle.Annual => plan.AnnualPriceInCents ?? plan.MonthlyPriceInCents * 12,
            BillingCycle.Monthly => plan.MonthlyPriceInCents,
            _ => plan.MonthlyPriceInCents
        };

        return Money.FromCents(priceInCents, plan.Currency);
    }

    /// <inheritdoc />
    public async Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planService.GetByIdAsync(planId, cancellationToken);
        return plan is not null;
    }
}
