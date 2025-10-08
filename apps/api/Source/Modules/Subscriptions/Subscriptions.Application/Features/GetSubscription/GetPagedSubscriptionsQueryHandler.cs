using MediatR;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query handler for getting paginated subscriptions
/// </summary>
public class GetPagedSubscriptionsQueryHandler : IQueryHandler<GetPagedSubscriptionsQuery, IEnumerable<Subscription>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetPagedSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository) 
    { 
        _subscriptionRepository = subscriptionRepository; 
    }

    public async Task<IEnumerable<Subscription>> Handle(GetPagedSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await _subscriptionRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Status,
            request.TenantId,
            request.PlanId,
            cancellationToken).ConfigureAwait(false);
            
        return pagedResult.Items;
    }
}

