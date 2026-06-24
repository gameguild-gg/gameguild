namespace GameGuild.API.Integration;

public sealed class SubscriptionNotificationLinkOptions
{
    public string WorkspaceLabel { get; set; } = "GameGuild";

    public string BillingDashboardPath { get; set; } = "/dashboard/billing";

    public string StatementPageTemplate { get; set; } = "/dashboard/billing/statements?from={from}&to={to}";

    public string StatementPdfTemplate { get; set; } = "/dashboard/billing/statements/export.pdf?from={from}&to={to}";

    public string StatementCsvTemplate { get; set; } = "/dashboard/billing/statements/export.csv?from={from}&to={to}";
}
