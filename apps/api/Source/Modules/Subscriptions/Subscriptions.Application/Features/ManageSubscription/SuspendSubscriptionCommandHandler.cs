using MediatR;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command handler for suspending a subscription
/// </summary>
public class SuspendSubscriptionCommandHandler : ICommandHandler<SuspendSubscriptionCommand>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SuspendSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) { _subscriptionRepository = subscriptionRepository; }

    public async Task<Unit> Handle(SuspendSubscriptionCommand request, CancellationToken cancellationToken)
    {
        Subscription? subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription not found");
        }

        subscription.Suspend(request.Reason);

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

