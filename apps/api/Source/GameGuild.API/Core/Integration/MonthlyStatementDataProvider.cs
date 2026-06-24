using System.Globalization;
using GameGuild.Commerce.Subscriptions;

namespace GameGuild.API.Integration;

public sealed class MonthlyStatementDataProvider(
    ISubscriptionRepository subscriptionRepository) : IMonthlyStatementDataProvider
{
    public async Task<MonthlyStatementBuildContext> BuildAsync(
        Guid tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var subscriptions = (await subscriptionRepository
                .GetByTenantIdAsync(tenantId, cancellationToken)
                .ConfigureAwait(false))
            .ToList();

        var periodStartUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var periodEndExclusiveUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var periodSubscriptions = subscriptions
            .Where(subscription => subscription.CreatedAt >= periodStartUtc && subscription.CreatedAt < periodEndExclusiveUtc)
            .OrderByDescending(subscription => subscription.CreatedAt)
            .ToList();

        var activeSubscriptions = subscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active)
            .ToList();

        var activeRevenue = RoundCurrency(activeSubscriptions.Sum(subscription => subscription.Amount.Amount));
        var periodRevenue = RoundCurrency(periodSubscriptions.Sum(subscription => subscription.Amount.Amount));
        var categories = BuildCategorySummaries(subscriptions);
        var transactions = BuildTransactionSummaries(periodSubscriptions);

        var sourceData = new MonthlyStatementSourceData(
            tenantId,
            generatedAtUtc,
            fromDate,
            toDate,
            subscriptions.Count,
            activeSubscriptions.Count,
            transactions.Count,
            0m,
            activeRevenue,
            activeRevenue,
            activeRevenue,
            categories,
            [
                new StatementPeriodSummary(
                    fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    fromDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    0m,
                    periodRevenue,
                    periodRevenue,
                    activeRevenue,
                    periodSubscriptions.Count),
            ],
            transactions,
            [],
            [],
            null);

        var documentOptions = new MonthlyStatementDocumentOptions(
            $"gameguild-statement-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}",
            "GameGuild Subscription Statement",
            MonthlyStatementDocumentProfile.Detailed);

        return new MonthlyStatementBuildContext(sourceData, documentOptions);
    }

    private static IReadOnlyList<StatementCategorySummary> BuildCategorySummaries(IReadOnlyList<Subscription> subscriptions)
    {
        var grouped = subscriptions
            .GroupBy(subscription => subscription.Status)
            .Select(group => new
            {
                Status = group.Key.ToString(),
                Count = group.Count(),
                Total = RoundCurrency(group.Sum(subscription => subscription.Amount.Amount)),
            })
            .OrderByDescending(group => group.Total)
            .ToList();

        var total = grouped.Sum(group => group.Total);

        return grouped
            .Select(group => new StatementCategorySummary(
                group.Status,
                0m,
                group.Total,
                group.Total,
                group.Count,
                total == 0m ? 0m : Math.Round((group.Total / total) * 100m, 2)))
            .ToList();
    }

    private static IReadOnlyList<StatementTransactionSummary> BuildTransactionSummaries(IReadOnlyList<Subscription> subscriptions)
        => subscriptions
            .Select(subscription => new StatementTransactionSummary(
                subscription.Id,
                subscription.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                subscription.Plan?.Slug ?? "subscription",
                "Subscription",
                subscription.Status.ToString(),
                subscription.Plan?.Name ?? "Subscription",
                RoundCurrency(subscription.Amount.Amount),
                subscription.Status.ToString(),
                subscription.ExternalCustomerId,
                subscription.CreatedAt))
            .ToList();

    private static decimal RoundCurrency(decimal value) => Math.Round(value, 2);
}
