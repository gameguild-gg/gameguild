using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command handler for setting subscription auto-renew
/// </summary>
public class SetSubscriptionAutoRenewCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<SetSubscriptionAutoRenewCommand>
{
    public async Task<Unit> Handle(SetSubscriptionAutoRenewCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false);

        if (subscription == null) { throw new InvalidOperationException("Subscription not found"); }

        subscription.SetAutoRenew(request.AutoRenew);

        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}
