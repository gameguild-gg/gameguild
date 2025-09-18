using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Abstractions;


namespace GameGuild.Modules.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command handler for cancelling user subscriptions with proper state management
/// </summary>
/// <remarks>
/// This handler validates subscription existence, applies cancellation logic through
/// domain methods, and persists the state changes with appropriate domain events.
/// </remarks>
public class CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<CancelSubscriptionCommand> {
  private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;

  /// <summary>
  /// Handles the subscription cancellation command with validation and persistence
  /// </summary>
  /// <param name="request">The subscription cancellation request</param>
  /// <param name="cancellationToken">Cancellation token for async operations</param>
  /// <returns>Unit value indicating successful completion</returns>
  /// <exception cref="ArgumentException">Thrown when subscription is not found</exception>
  public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken) {
    // Retrieve the subscription to cancel
    var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
    if (subscription is null) {
      throw new ArgumentException($"Subscription with ID {request.SubscriptionId} not found");
    }

    // Apply cancellation through domain method (handles business rules and events)
    subscription.Cancel(request.Reason, request.Note, request.EffectiveDate);

    // Persist the cancellation state and commit changes
    _ = await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    _ = await _subscriptionRepository.SaveChangesAsync(cancellationToken);

    return Unit.Value;
  }
}
