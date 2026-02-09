namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for recording subscription payment failures.
///     Uses base handler to reduce boilerplate for fetch/validate/save pattern.
/// </summary>
public sealed class RecordSubscriptionPaymentFailureCommandHandler(ISubscriptionRepository subscriptionRepository)
    : SubscriptionCommandHandlerBase<RecordSubscriptionPaymentFailureCommand>(subscriptionRepository)
{
    /// <inheritdoc />
    protected override Guid GetSubscriptionId(RecordSubscriptionPaymentFailureCommand request) =>
        request.SubscriptionId;

    /// <inheritdoc />
    protected override Task ExecuteAsync(
        Subscription subscription,
        RecordSubscriptionPaymentFailureCommand request,
        CancellationToken cancellationToken)
    {
        subscription.RecordPaymentFailure(request.Reason, request.FailureDate);
        return Task.CompletedTask;
    }
}
