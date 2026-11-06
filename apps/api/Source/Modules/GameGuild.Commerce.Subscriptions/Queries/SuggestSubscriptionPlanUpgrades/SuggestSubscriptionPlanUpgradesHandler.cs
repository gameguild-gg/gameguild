using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for <see cref="SuggestSubscriptionPlanUpgradesQuery" /> (issue #1205 G-8):
///     suggests active plans whose limits accommodate the stated usage requirements and
///     that are priced above the current plan, cheapest first. Returns an empty sequence
///     when the current plan does not exist (nothing to upgrade from).
/// </summary>
public sealed class SuggestSubscriptionPlanUpgradesHandler(ISubscriptionPlanRepository planRepository)
    : IQueryHandler<SuggestSubscriptionPlanUpgradesQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(
        SuggestSubscriptionPlanUpgradesQuery request,
        CancellationToken cancellationToken)
    {
        var currentPlan = await planRepository
            .GetByIdAsync(request.CurrentPlanId, cancellationToken)
            .ConfigureAwait(false);
        if (currentPlan is null)
        {
            return [];
        }

        var activePlans = await planRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);

        return activePlans
            .Where(plan => plan.Id != currentPlan.Id)
            .Where(plan => plan.AllowsUserCount(request.Users))
            .Where(plan => plan.AllowsStorage(request.StorageMb))
            .Where(plan => plan.AllowsApiCalls(request.ApiCalls))
            .Where(plan => plan.MonthlyPriceInCents > currentPlan.MonthlyPriceInCents)
            .OrderBy(plan => plan.MonthlyPriceInCents)
            .ToList();
    }
}
