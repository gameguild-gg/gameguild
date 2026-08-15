namespace GameGuild.API.Integration;

public sealed class SubscriptionNotificationLinkOptions
{
    public string WorkspaceLabel { get; set; } = string.Empty;

    public string BillingDashboardPath { get; set; } = string.Empty;

    public string StatementPageTemplate { get; set; } = string.Empty;

    public string StatementPdfTemplate { get; set; } = string.Empty;

    public string StatementCsvTemplate { get; set; } = string.Empty;
}
