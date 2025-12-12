using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Query handler for getting subscription by ID
/// </summary>
public class GetSubscriptionByIdQueryHandler(ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetSubscriptionByIdQuery, Subscription?>
{
    public async Task<Subscription?> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false);
    }
}
