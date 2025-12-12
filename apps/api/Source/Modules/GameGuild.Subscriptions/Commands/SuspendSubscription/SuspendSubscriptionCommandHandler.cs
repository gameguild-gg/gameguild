using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command handler for suspending a subscription
/// </summary>
public class SuspendSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<SuspendSubscriptionCommand>
{
    public async Task<Unit> Handle(SuspendSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription == null) { throw new InvalidOperationException("Subscription not found"); }

        subscription.Suspend(request.Reason);

        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}
