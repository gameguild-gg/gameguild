using GameGuild.Commerce.Payments;

namespace GameGuild.Commerce.Subscriptions;

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
        var plan = await _planService.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        
        if (plan is null)
            return null;

        return CentsToMoney(plan.MonthlyPriceInCents, plan.Currency);
    }

    /// <inheritdoc />
    public async Task<Money?> GetPlanPriceAsync(Guid planId, BillingCycle billingCycle, CancellationToken cancellationToken = default)
    {
        var plan = await _planService.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        
        if (plan is null)
            return null;

        var priceInCents = billingCycle switch
        {
            BillingCycle.Annually => plan.AnnualPriceInCents ?? plan.MonthlyPriceInCents * 12,
            BillingCycle.SemiAnnually => plan.MonthlyPriceInCents * 6,
            BillingCycle.Quarterly => plan.MonthlyPriceInCents * 3,
            BillingCycle.Monthly => plan.MonthlyPriceInCents,
            BillingCycle.Weekly => plan.MonthlyPriceInCents / 4, // Approximate weekly from monthly
            BillingCycle.Biannually => plan.MonthlyPriceInCents * 24,
            _ => plan.MonthlyPriceInCents
        };

        return CentsToMoney(priceInCents, plan.Currency);
    }

    /// <inheritdoc />
    public async Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planService.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        return plan is not null;
    }
    
    /// <summary>
    ///     Converts cents to Money value object.
    /// </summary>
    private static Money CentsToMoney(long cents, string currency)
        => new(cents / 100m, currency);
}
