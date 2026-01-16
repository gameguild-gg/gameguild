using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for PatchSubscriptionCommand
/// </summary>
public sealed class PatchSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<PatchSubscriptionCommand>
{
    public async Task Handle(PatchSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        // Apply only provided fields
        if (request.BillingCycle.HasValue)
            subscription.BillingCycle = request.BillingCycle.Value;

        if (request.AutoRenew.HasValue)
            subscription.AutoRenew = request.AutoRenew.Value;

        if (request.ExternalSubscriptionId != null)
            subscription.ExternalSubscriptionId = request.ExternalSubscriptionId;

        if (request.ExternalCustomerId != null)
            subscription.ExternalCustomerId = request.ExternalCustomerId;

        if (request.Metadata != null)
            subscription.Metadata = request.Metadata;

        subscription.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}
