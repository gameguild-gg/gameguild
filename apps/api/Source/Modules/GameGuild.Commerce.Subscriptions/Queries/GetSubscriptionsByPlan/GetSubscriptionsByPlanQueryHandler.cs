using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting all subscriptions on a plan — the per-plan
///     companion of <see cref="GetSubscriptionsByStatusQuery"/>.
/// </summary>
public sealed class GetSubscriptionsByPlanQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IQueryHandler<GetSubscriptionsByPlanQuery, IEnumerable<Subscription>>
{
    public async Task<IEnumerable<Subscription>> Handle(GetSubscriptionsByPlanQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetByPlanIdAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
    }
}
