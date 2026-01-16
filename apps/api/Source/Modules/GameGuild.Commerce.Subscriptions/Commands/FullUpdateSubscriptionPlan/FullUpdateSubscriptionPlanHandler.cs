using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for FullUpdateSubscriptionPlanCommand
/// </summary>
public sealed class FullUpdateSubscriptionPlanHandler(IApplicationDbContext context)
    : ICommandHandler<FullUpdateSubscriptionPlanCommand>
{
    public async Task<Unit> Handle(FullUpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription plan {request.PlanId} not found");

        // Full update - replace all fields
        plan.Name = request.Name;
        plan.Slug = request.Slug;
        plan.Description = request.Description;
        plan.MonthlyPriceInCents = request.MonthlyPriceInCents;
        plan.AnnualPriceInCents = request.AnnualPriceInCents;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxStorageMb = request.MaxStorageMb;
        plan.MaxApiCallsPerMonth = request.MaxApiCallsPerMonth;
        plan.HasPrioritySupport = request.HasPrioritySupport ?? false;
        plan.HasAdvancedAnalytics = request.HasAdvancedAnalytics ?? false;
        plan.HasCustomBranding = request.HasCustomBranding ?? false;
        plan.Features = request.Features;
        plan.SortOrder = request.SortOrder ?? 0;
        plan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
