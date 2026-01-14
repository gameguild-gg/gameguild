using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for GetSubscriptionStatusCountsQuery
/// </summary>
public sealed class GetSubscriptionStatusCountsQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IQueryHandler<GetSubscriptionStatusCountsQuery, Dictionary<SubscriptionStatus, int>>
{
    public async Task<Dictionary<SubscriptionStatus, int>> Handle(
        GetSubscriptionStatusCountsQuery request,
        CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetCountByStatusAsync(cancellationToken).ConfigureAwait(false);
    }
}
