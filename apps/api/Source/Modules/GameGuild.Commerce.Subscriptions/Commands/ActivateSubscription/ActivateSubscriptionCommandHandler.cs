namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for activating a subscription.
///     Uses base handler to reduce boilerplate for fetch/validate/save pattern.
/// </summary>
public class ActivateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
    : SubscriptionCommandHandlerBase<ActivateSubscriptionCommand>(subscriptionRepository)
{
    /// <inheritdoc />
    protected override Guid GetSubscriptionId(ActivateSubscriptionCommand request) =>
        request.SubscriptionId;

    /// <inheritdoc />
    protected override Task ExecuteAsync(
        Subscription subscription,
        ActivateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        subscription.Activate();
        return Task.CompletedTask;
    }
}
