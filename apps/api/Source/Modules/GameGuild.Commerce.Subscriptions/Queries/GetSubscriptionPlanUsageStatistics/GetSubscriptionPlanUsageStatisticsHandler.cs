using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for <see cref="GetSubscriptionPlanUsageStatisticsQuery" /> (issue #1205 G-8):
///     aggregates the plan's subscription population into usage statistics. Returns
///     <see langword="null" /> when the plan does not exist.
/// </summary>
public sealed class GetSubscriptionPlanUsageStatisticsHandler(
    ISubscriptionPlanRepository planRepository,
    IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionPlanUsageStatisticsQuery, object>
{
    public async Task<object?> Handle(
        GetSubscriptionPlanUsageStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return null;
        }

        var now = SystemClock.UtcNow;

        var subscriptions = await context.Set<Subscription>()
            .AsNoTracking()
            .Where(subscription => subscription.PlanId == request.PlanId)
            .Select(subscription => new
            {
                subscription.Status,
                subscription.Amount,
                subscription.CreatedAt,
                subscription.EndDate,
                subscription.TrialEndDate,
                subscription.CancelledAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var active = subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Active);
        var cancelled = subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Cancelled);
        var total = subscriptions.Count;

        var totalRevenue = subscriptions
            .Where(subscription => subscription.Status != SubscriptionStatus.PendingActivation)
            .Sum(subscription => subscription.Amount.Amount);

        var averageMonthlyRevenue = total == 0
            ? 0m
            : Math.Round(totalRevenue / Math.Max(total, 1), 2, MidpointRounding.AwayFromZero);

        var terminated = subscriptions
            .Where(subscription => subscription.EndDate.HasValue)
            .ToList();
        var averageDurationDays = terminated.Count == 0
            ? 0d
            : terminated.Average(subscription =>
                Math.Max((subscription.EndDate!.Value - subscription.CreatedAt).TotalDays, 0));

        var trialing = subscriptions.Count(subscription => subscription.TrialEndDate.HasValue);
        var convertedFromTrial = subscriptions.Count(subscription =>
            subscription.TrialEndDate.HasValue &&
            subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Suspended);
        var trialConversionRate = trialing == 0
            ? (decimal?)null
            : Math.Round(convertedFromTrial * 100m / trialing, 2, MidpointRounding.AwayFromZero);

        // Churn over the trailing 30 days: cancellations ending in the window over the
        // population that existed at the window's start (fallback: all-time ratio).
        var churnWindowStart = now.AddDays(-30);
        var churnDenominator = subscriptions.Count(subscription => subscription.CreatedAt <= churnWindowStart);
        var churned = subscriptions.Count(subscription =>
            subscription.Status == SubscriptionStatus.Cancelled &&
            subscription.EndDate.HasValue &&
            subscription.EndDate.Value >= churnWindowStart);
        var churnRate = churnDenominator == 0
            ? Math.Round(cancelled * 100m / Math.Max(total, 1), 2, MidpointRounding.AwayFromZero)
            : Math.Round(churned * 100m / churnDenominator, 2, MidpointRounding.AwayFromZero);

        return new PlanUsageStatistics
        {
            PlanId = request.PlanId,
            ActiveSubscriptions = active,
            CancelledSubscriptions = cancelled,
            AverageMonthlyRevenue = averageMonthlyRevenue,
            TotalRevenue = totalRevenue,
            AverageSubscriptionDurationDays = Math.Round(averageDurationDays, 2, MidpointRounding.AwayFromZero),
            TrialConversionRate = trialConversionRate,
            ChurnRate = churnRate,
            CalculatedAt = now
        };
    }
}
