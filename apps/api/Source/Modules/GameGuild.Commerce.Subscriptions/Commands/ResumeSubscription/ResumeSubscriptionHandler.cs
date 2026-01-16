using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for ResumeSubscriptionCommand
/// </summary>
public sealed class ResumeSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<ResumeSubscriptionCommand>
{
    public async Task Handle(ResumeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        subscription.IsPaused = false;
        subscription.PausedAt = null;
        subscription.PauseReason = null;
        subscription.PauseUntil = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
