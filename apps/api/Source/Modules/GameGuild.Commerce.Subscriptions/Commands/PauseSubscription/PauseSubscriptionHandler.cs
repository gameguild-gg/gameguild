using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for PauseSubscriptionCommand.
///     Note: Uses Suspend() method since the entity doesn't have dedicated pause properties.
///     Pause reason is stored in metadata.
/// </summary>
public sealed class PauseSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<PauseSubscriptionCommand>
{
    public async Task<Unit> Handle(PauseSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        // Use Suspend() which sets status to Suspended and stores reason in metadata
        subscription.Suspend(request.Reason);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
