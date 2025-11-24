using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command handler for cancelling a subscription
/// </summary>
public class CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<CancelSubscriptionCommand>
{
    public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription == null) { throw new InvalidOperationException("Subscription not found"); }

        subscription.Cancel(request.Reason, request.Note, request.EffectiveDate);

        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}
