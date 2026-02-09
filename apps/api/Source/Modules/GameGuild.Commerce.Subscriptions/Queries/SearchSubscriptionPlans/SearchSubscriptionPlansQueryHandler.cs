using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for searching subscription plans
/// </summary>
public sealed class SearchSubscriptionPlansQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository) 
    : IQueryHandler<SearchSubscriptionPlansQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(SearchSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionPlanRepository.SearchByNameAsync(request.SearchTerm, cancellationToken).ConfigureAwait(false);
    }
}
