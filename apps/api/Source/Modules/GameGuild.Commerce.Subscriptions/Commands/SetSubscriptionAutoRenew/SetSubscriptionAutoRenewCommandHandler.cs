namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for setting subscription auto-renew.
///     Uses base handler to reduce boilerplate for fetch/validate/save pattern.
/// </summary>
public sealed class SetSubscriptionAutoRenewCommandHandler(ISubscriptionRepository subscriptionRepository)
    : SubscriptionCommandHandlerBase<SetSubscriptionAutoRenewCommand>(subscriptionRepository)
{
    /// <inheritdoc />
    protected override Guid GetSubscriptionId(SetSubscriptionAutoRenewCommand request) =>
        request.SubscriptionId;

    /// <inheritdoc />
    protected override Task ExecuteAsync(
        Subscription subscription,
        SetSubscriptionAutoRenewCommand request,
        CancellationToken cancellationToken)
    {
        subscription.SetAutoRenew(request.AutoRenew);
        return Task.CompletedTask;
    }
}
