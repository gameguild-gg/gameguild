using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting subscriptions by tenant
/// </summary>
public class GetTenantSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetTenantSubscriptionsQuery, IEnumerable<Subscription>>
{
    public async Task<IEnumerable<Subscription>> Handle(GetTenantSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}
