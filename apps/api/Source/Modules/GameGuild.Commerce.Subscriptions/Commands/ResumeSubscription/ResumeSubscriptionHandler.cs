using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for ResumeSubscriptionCommand.
///     Note: Uses Reactivate() method since the entity doesn't have dedicated pause properties.
/// </summary>
public sealed class ResumeSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<ResumeSubscriptionCommand>
{
    public async Task<Unit> Handle(ResumeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        // Use Reactivate() which sets status back to Active and clears metadata
        subscription.Reactivate();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
