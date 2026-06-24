using System.Globalization;
using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Integration;

public sealed class MonthlyStatementLinkBuilder(IOptions<SubscriptionNotificationLinkOptions> options) : IMonthlyStatementLinkBuilder
{
    public MonthlyStatementLinks Build(DateOnly fromDate, DateOnly toDate)
    {
        var currentOptions = options.Value;

        return new MonthlyStatementLinks(
            currentOptions.WorkspaceLabel,
            currentOptions.BillingDashboardPath,
            ExpandTemplate(currentOptions.StatementPageTemplate, fromDate, toDate),
            ExpandTemplate(currentOptions.StatementPdfTemplate, fromDate, toDate),
            ExpandTemplate(currentOptions.StatementCsvTemplate, fromDate, toDate));
    }

    public string GetBillingDashboardPath() => options.Value.BillingDashboardPath;

    private static string ExpandTemplate(string template, DateOnly fromDate, DateOnly toDate)
        => template
            .Replace("{from}", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{to}", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
