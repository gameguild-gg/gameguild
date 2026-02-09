namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for suspending a subscription.
///     Uses base handler to reduce boilerplate for fetch/validate/save pattern.
/// </summary>
public sealed class SuspendSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
    : SubscriptionCommandHandlerBase<SuspendSubscriptionCommand>(subscriptionRepository)
{
    /// <inheritdoc />
    protected override Guid GetSubscriptionId(SuspendSubscriptionCommand request) =>
        request.SubscriptionId;

    /// <inheritdoc />
    protected override Task ExecuteAsync(
        Subscription subscription,
        SuspendSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        subscription.Suspend(request.Reason);
        return Task.CompletedTask;
    }
}
