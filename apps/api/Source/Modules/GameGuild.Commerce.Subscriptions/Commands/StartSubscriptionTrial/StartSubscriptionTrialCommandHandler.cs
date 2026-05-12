using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for StartSubscriptionTrialCommand.
///     Delegates to the lifecycle service for trial state transitions.
/// </summary>
public sealed class StartSubscriptionTrialCommandHandler(ISubscriptionLifecycleService lifecycleService)
    : ICommandHandler<StartSubscriptionTrialCommand>
{
    public async Task<Unit> Handle(StartSubscriptionTrialCommand request, CancellationToken cancellationToken)
    {
        await lifecycleService.StartTrialAsync(request.SubscriptionId, request.TrialDays, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}