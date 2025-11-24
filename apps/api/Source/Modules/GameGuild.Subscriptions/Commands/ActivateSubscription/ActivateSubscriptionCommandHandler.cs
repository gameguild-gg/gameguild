using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command handler for activating a subscription
/// </summary>
public class ActivateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<ActivateSubscriptionCommand>
{
    public async Task<Unit> Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription == null) { throw new InvalidOperationException("Subscription not found"); }

        subscription.Activate();

        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}
