using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query handler for getting subscriptions by tenant
/// </summary>
public class GetTenantSubscriptionsQueryHandler : IQueryHandler<GetTenantSubscriptionsQuery, IEnumerable<Subscription>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetTenantSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository) 
    { 
        _subscriptionRepository = subscriptionRepository; 
    }

    public async Task<IEnumerable<Subscription>> Handle(GetTenantSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        return await _subscriptionRepository.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

