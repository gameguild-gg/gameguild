using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for GetExpiringSubscriptionsQuery
/// </summary>
public sealed class GetExpiringSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IQueryHandler<GetExpiringSubscriptionsQuery, IEnumerable<Subscription>>
{
    public async Task<IEnumerable<Subscription>> Handle(
        GetExpiringSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetExpiringSoonAsync(request.Days, cancellationToken).ConfigureAwait(false);
    }
}
