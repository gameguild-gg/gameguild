using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting subscription by ID
/// </summary>
public sealed class GetSubscriptionByIdQueryHandler(ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetSubscriptionByIdQuery, Subscription?>
{
    public async Task<Subscription?> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false);
    }
}
