using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for ReactivateSubscriptionCommand.
///     Delegates to the lifecycle service for subscription reactivation.
/// </summary>
public sealed class ReactivateSubscriptionCommandHandler(ISubscriptionLifecycleService lifecycleService)
    : ICommandHandler<ReactivateSubscriptionCommand>
{
    public async Task<Unit> Handle(ReactivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await lifecycleService.ReactivateAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}