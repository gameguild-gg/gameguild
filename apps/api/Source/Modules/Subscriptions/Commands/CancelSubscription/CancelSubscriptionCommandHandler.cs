using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Abstractions;


namespace GameGuild.Modules.Subscriptions.Commands.CancelSubscription;

/// <summary> Command handler for cancelling a subscription </summary>
public class CancelSubscriptionCommandHandler : ICommandHandler<CancelSubscriptionCommand> {
  private readonly ISubscriptionRepository _subscriptionRepository;

  public CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) { _subscriptionRepository = subscriptionRepository; }

  public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken) {
    var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

    if (subscription == null) throw new ArgumentException($"Subscription with ID {request.SubscriptionId} not found");

    subscription.Cancel(request.Reason, request.Note, request.EffectiveDate);

    _subscriptionRepository.Update(subscription);
    await _subscriptionRepository.SaveChangesAsync(cancellationToken);

    return Unit.Value;
  }
}
