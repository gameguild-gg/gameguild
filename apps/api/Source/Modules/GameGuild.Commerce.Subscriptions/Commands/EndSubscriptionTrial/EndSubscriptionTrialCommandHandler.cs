using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for EndSubscriptionTrialCommand.
///     Delegates to the lifecycle service for trial completion logic.
/// </summary>
public sealed class EndSubscriptionTrialCommandHandler(ISubscriptionLifecycleService lifecycleService)
    : ICommandHandler<EndSubscriptionTrialCommand>
{
    public async Task<Unit> Handle(EndSubscriptionTrialCommand request, CancellationToken cancellationToken)
    {
        await lifecycleService.EndTrialAsync(request.SubscriptionId, request.ConvertToPaid, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}