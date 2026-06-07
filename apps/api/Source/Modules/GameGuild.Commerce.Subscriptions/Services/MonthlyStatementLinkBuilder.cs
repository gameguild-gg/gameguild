using Microsoft.Extensions.Configuration;

namespace GameGuild.Commerce.Subscriptions;

public sealed record MonthlyStatementLinks(
    string WorkspaceLabel,
    string BillingDashboardPath,
    string StatementPagePath,
    string StatementPdfPath,
    string StatementCsvPath);

public interface IMonthlyStatementLinkBuilder
{
    MonthlyStatementLinks Build(DateOnly fromDate, DateOnly toDate);

    string GetBillingDashboardPath();
}

public sealed class MonthlyStatementLinkBuilder : IMonthlyStatementLinkBuilder
{
    private const string DefaultWorkspaceLabel = "GameGuild workspace";
    private readonly string _billingDashboardPath;
    private readonly string _workspaceLabel;

    public MonthlyStatementLinkBuilder(IConfiguration configuration)
    {
        _workspaceLabel = configuration["StatementEmails:WorkspaceLabel"] ?? DefaultWorkspaceLabel;
        _billingDashboardPath = NormalizePath(configuration["StatementEmails:BillingDashboardPath"], "/billing");
    }

    public MonthlyStatementLinks Build(DateOnly fromDate, DateOnly toDate)
    {
        var period = $"{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}";
        var statementPath = $"{_billingDashboardPath.TrimEnd('/')}/statements/{period}";

        return new MonthlyStatementLinks(
            _workspaceLabel,
            _billingDashboardPath,
            statementPath,
            $"{statementPath}.pdf",
            $"{statementPath}.csv");
    }

    public string GetBillingDashboardPath() => _billingDashboardPath;

    private static string NormalizePath(string? configuredPath, string fallback)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath.Trim();
        return path.StartsWith("/", StringComparison.Ordinal) ? path : $"/{path}";
    }
}
