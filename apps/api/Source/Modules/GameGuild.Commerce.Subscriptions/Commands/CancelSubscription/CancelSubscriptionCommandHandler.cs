namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for cancelling a subscription.
///     Uses base handler to reduce boilerplate for fetch/validate/save pattern.
/// </summary>
public class CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
    : SubscriptionCommandHandlerBase<CancelSubscriptionCommand>(subscriptionRepository)
{
    /// <inheritdoc />
    protected override Guid GetSubscriptionId(CancelSubscriptionCommand request) =>
        request.SubscriptionId;

    /// <inheritdoc />
    protected override Task ExecuteAsync(
        Subscription subscription,
        CancelSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        subscription.Cancel(request.Reason, request.Note, request.EffectiveDate);
        return Task.CompletedTask;
    }
}
