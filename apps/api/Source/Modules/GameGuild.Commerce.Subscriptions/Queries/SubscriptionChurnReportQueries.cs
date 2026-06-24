using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SubscriptionChurnReportDto(
    Guid? TenantId,
    DateTime StartDate,
    DateTime EndDate,
    int TotalSubscriptions,
    int ActiveSubscriptions,
    int CancelledInPeriod,
    decimal ChurnRate,
    decimal RetentionRate,
    decimal MonthlyRecurringRevenue,
    DateTime GeneratedAt,
    IReadOnlyDictionary<string, int> StatusBreakdown);

public sealed record GetSubscriptionChurnReportQuery(
    Guid? TenantId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IQuery<SubscriptionChurnReportDto>;

public sealed class GetSubscriptionChurnReportQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionChurnReportQuery, SubscriptionChurnReportDto>
{
    public async Task<SubscriptionChurnReportDto> Handle(GetSubscriptionChurnReportQuery request, CancellationToken cancellationToken)
    {
        var end = request.EndDate ?? SystemClock.UtcNow;
        var start = request.StartDate ?? end.AddDays(-30);

        if (start > end)
        {
            throw new ArgumentException("StartDate must be before or equal to EndDate.", nameof(request));
        }

        var query = context.Set<Subscription>().AsNoTracking();

        if (request.TenantId.HasValue)
        {
            var tenantId = request.TenantId.Value;
            query = query.Where(subscription => subscription.TenantId.HasValue && subscription.TenantId.Value == tenantId);
        }

        var subscriptions = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var activeStatuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Trialing };

        var activeSubscriptions = subscriptions.Count(subscription => activeStatuses.Contains(subscription.Status));
        var cancelledInPeriod = subscriptions.Count(subscription =>
            subscription.CancelledAt.HasValue &&
            subscription.CancelledAt.Value >= start &&
            subscription.CancelledAt.Value <= end);
        var activeAtPeriodStart = subscriptions.Count(subscription =>
            subscription.StartDate <= start &&
            (!subscription.CancelledAt.HasValue || subscription.CancelledAt.Value > start));
        var denominator = Math.Max(activeAtPeriodStart, activeSubscriptions + cancelledInPeriod);
        var churnRate = denominator == 0 ? 0m : Math.Round(cancelledInPeriod * 100m / denominator, 2, MidpointRounding.AwayFromZero);
        var retentionRate = denominator == 0 ? 100m : Math.Max(0m, Math.Round(100m - churnRate, 2, MidpointRounding.AwayFromZero));
        var mrr = subscriptions
            .Where(subscription => activeStatuses.Contains(subscription.Status))
            .Sum(subscription => subscription.Amount.Amount);
        var statusBreakdown = subscriptions
            .GroupBy(subscription => subscription.Status.ToString())
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new SubscriptionChurnReportDto(
            request.TenantId,
            start,
            end,
            subscriptions.Count,
            activeSubscriptions,
            cancelledInPeriod,
            churnRate,
            retentionRate,
            mrr,
            SystemClock.UtcNow,
            statusBreakdown);
    }
}
