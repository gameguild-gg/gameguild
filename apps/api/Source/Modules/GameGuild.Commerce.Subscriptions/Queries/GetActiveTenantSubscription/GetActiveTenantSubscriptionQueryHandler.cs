using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting active subscriptions by tenant
/// </summary>
public class GetActiveTenantSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetActiveTenantSubscriptionQuery, Subscription?>
{
    public async Task<Subscription?> Handle(GetActiveTenantSubscriptionQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionRepository.GetActiveTenantSubscriptionAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}
