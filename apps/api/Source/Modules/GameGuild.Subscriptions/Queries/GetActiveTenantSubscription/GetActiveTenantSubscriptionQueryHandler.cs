using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

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
