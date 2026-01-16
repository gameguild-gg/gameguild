using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for PauseSubscriptionCommand
/// </summary>
public sealed class PauseSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<PauseSubscriptionCommand>
{
    public async Task Handle(PauseSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        subscription.IsPaused = true;
        subscription.PausedAt = DateTime.UtcNow;
        subscription.PauseReason = request.Reason;
        subscription.PauseUntil = request.PauseUntil;
        subscription.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
