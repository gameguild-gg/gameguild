using MediatR;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query handler for getting active subscriptions by tenant
/// </summary>
public class GetActiveTenantSubscriptionQueryHandler : IQueryHandler<GetActiveTenantSubscriptionQuery, Subscription?>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetActiveTenantSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository) 
    { 
        _subscriptionRepository = subscriptionRepository; 
    }

    public async Task<Subscription?> Handle(GetActiveTenantSubscriptionQuery request, CancellationToken cancellationToken)
    {
        return await _subscriptionRepository.GetActiveTenantSubscriptionAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

