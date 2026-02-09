using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting featured subscription plans
/// </summary>
public sealed class GetFeaturedSubscriptionPlansQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository) 
    : IQueryHandler<GetFeaturedSubscriptionPlansQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(GetFeaturedSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionPlanRepository.GetFeaturedAsync(cancellationToken).ConfigureAwait(false);
    }
}
