using MediatR;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query handler for getting subscription by ID
/// </summary>
public class GetSubscriptionByIdQueryHandler : IQueryHandler<GetSubscriptionByIdQuery, Subscription?>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetSubscriptionByIdQueryHandler(ISubscriptionRepository subscriptionRepository) 
    { 
        _subscriptionRepository = subscriptionRepository; 
    }

    public async Task<Subscription?> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken) 
    { 
        return await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false); 
    }
}

