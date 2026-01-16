using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for CloneSubscriptionPlanCommand
/// </summary>
public sealed class CloneSubscriptionPlanHandler(IApplicationDbContext context)
    : ICommandHandler<CloneSubscriptionPlanCommand, Guid>
{
    public async Task<Guid> Handle(CloneSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var sourcePlan = await context.Set<SubscriptionPlan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.SourcePlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription plan {request.SourcePlanId} not found");

        var newPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = request.NewName,
            Slug = request.NewSlug,
            Description = sourcePlan.Description,
            MonthlyPriceInCents = sourcePlan.MonthlyPriceInCents,
            AnnualPriceInCents = sourcePlan.AnnualPriceInCents,
            Currency = sourcePlan.Currency,
            MaxUsers = sourcePlan.MaxUsers,
            MaxStorageMb = sourcePlan.MaxStorageMb,
            MaxApiCallsPerMonth = sourcePlan.MaxApiCallsPerMonth,
            HasPrioritySupport = sourcePlan.HasPrioritySupport,
            HasAdvancedAnalytics = sourcePlan.HasAdvancedAnalytics,
            HasCustomBranding = sourcePlan.HasCustomBranding,
            Features = sourcePlan.Features,
            SortOrder = sourcePlan.SortOrder,
            IsActive = false, // Clone starts as inactive
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Set<SubscriptionPlan>().Add(newPlan);
        await context.SaveChangesAsync(cancellationToken);

        return newPlan.Id;
    }
}
