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
