using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command handler for setting subscription auto-renew
/// </summary>
public class SetSubscriptionAutoRenewCommandHandler : ICommandHandler<SetSubscriptionAutoRenewCommand>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SetSubscriptionAutoRenewCommandHandler(ISubscriptionRepository subscriptionRepository) { _subscriptionRepository = subscriptionRepository; }

    public async Task<Unit> Handle(SetSubscriptionAutoRenewCommand request, CancellationToken cancellationToken)
    {
        Subscription? subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription not found");
        }

        subscription.SetAutoRenew(request.AutoRenew);

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

