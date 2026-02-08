using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for DeleteSubscriptionCommand
/// </summary>
public sealed class DeleteSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteSubscriptionCommand>
{
    public async Task<Unit> Handle(DeleteSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        context.Set<Subscription>().Remove(subscription);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
