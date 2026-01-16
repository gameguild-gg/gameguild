using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for UpdateSubscriptionCommand
/// </summary>
public sealed class UpdateSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateSubscriptionCommand>
{
    public async Task Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        // Full update - replace all fields
        subscription.PlanId = request.PlanId;
        subscription.BillingCycle = request.BillingCycle;
        subscription.Amount = request.Amount;
        subscription.AutoRenew = request.AutoRenew;
        subscription.ExternalSubscriptionId = request.ExternalSubscriptionId;
        subscription.ExternalCustomerId = request.ExternalCustomerId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
