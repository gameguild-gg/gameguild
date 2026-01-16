using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for ArchiveSubscriptionPlanCommand.
///     Uses Deactivate() method since the entity doesn't have dedicated archive properties.
/// </summary>
public sealed class ArchiveSubscriptionPlanHandler(IApplicationDbContext context)
    : ICommandHandler<ArchiveSubscriptionPlanCommand>
{
    public async Task<Unit> Handle(ArchiveSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription plan {request.PlanId} not found");

        // Use Deactivate() which sets IsActive = false and raises PlanDiscontinuedEvent
        plan.Deactivate();

        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
