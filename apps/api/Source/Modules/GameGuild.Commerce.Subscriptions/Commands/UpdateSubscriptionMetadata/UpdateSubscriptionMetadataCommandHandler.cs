namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for updating subscription metadata.
///     Uses base handler to reduce boilerplate for fetch/validate/save pattern.
/// </summary>
public sealed class UpdateSubscriptionMetadataCommandHandler(ISubscriptionRepository subscriptionRepository)
    : SubscriptionCommandHandlerBase<UpdateSubscriptionMetadataCommand>(subscriptionRepository)
{
    /// <inheritdoc />
    protected override Guid GetSubscriptionId(UpdateSubscriptionMetadataCommand request) =>
        request.SubscriptionId;

    /// <inheritdoc />
    protected override Task ExecuteAsync(
        Subscription subscription,
        UpdateSubscriptionMetadataCommand request,
        CancellationToken cancellationToken)
    {
        subscription.UpdateMetadata(request.Metadata);
        return Task.CompletedTask;
    }
}
