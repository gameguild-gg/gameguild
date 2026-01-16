using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for ArchiveSubscriptionPlanCommand
/// </summary>
public sealed class ArchiveSubscriptionPlanHandler(IApplicationDbContext context)
    : ICommandHandler<ArchiveSubscriptionPlanCommand>
{
    public async Task Handle(ArchiveSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription plan {request.PlanId} not found");

        plan.IsArchived = true;
        plan.ArchivedAt = DateTime.UtcNow;
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
