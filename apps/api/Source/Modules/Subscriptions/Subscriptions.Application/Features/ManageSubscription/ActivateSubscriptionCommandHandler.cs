using MediatR;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command handler for activating a subscription
/// </summary>
public class ActivateSubscriptionCommandHandler : ICommandHandler<ActivateSubscriptionCommand>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public ActivateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) { _subscriptionRepository = subscriptionRepository; }

    public async Task<Unit> Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        Subscription? subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription not found");
        }

        subscription.Activate();

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

