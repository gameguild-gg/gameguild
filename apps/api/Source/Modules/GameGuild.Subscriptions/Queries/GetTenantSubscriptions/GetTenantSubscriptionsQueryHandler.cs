using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

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
