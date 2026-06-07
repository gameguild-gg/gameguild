using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed class MonthlyStatementPreparedNotification : INotification
{
    public required Guid SubscriptionId { get; init; }

    public required Guid TenantId { get; init; }

    public required Guid RecipientId { get; init; }

    public required string RecipientEmail { get; init; }

    public string? RecipientName { get; init; }

    public required string WorkspaceLabel { get; init; }

    public required string MonthLabel { get; init; }

    public required DateOnly FromDate { get; init; }

    public required DateOnly ToDate { get; init; }

    public required string StatementPagePath { get; init; }

    public required string StatementPdfPath { get; init; }

    public required string StatementCsvPath { get; init; }

    public required string StatementPageAbsoluteUrl { get; init; }

    public required string StatementPdfAbsoluteUrl { get; init; }

    public required string StatementCsvAbsoluteUrl { get; init; }

    public required MonthlyStatementArtifacts Artifacts { get; init; }
}
