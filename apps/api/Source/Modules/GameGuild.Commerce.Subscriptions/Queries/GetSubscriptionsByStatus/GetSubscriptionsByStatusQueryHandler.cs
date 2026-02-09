using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting subscriptions by status
/// </summary>
public sealed class GetSubscriptionsByStatusQueryHandler(ISubscriptionRepository subscriptionRepository) 
    : IQueryHandler<GetSubscriptionsByStatusQuery, IEnumerable<Subscription>>
{
    public async Task<IEnumerable<Subscription>> Handle(GetSubscriptionsByStatusQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetByStatusAsync(request.Status, cancellationToken).ConfigureAwait(false);
    }
}
