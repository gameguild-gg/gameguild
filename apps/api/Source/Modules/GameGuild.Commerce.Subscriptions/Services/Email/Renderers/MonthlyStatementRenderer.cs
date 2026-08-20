using System.Globalization;
using System.Text.Json;
using GameGuild.Email;
using GameGuild.Notifications;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Commerce.Subscriptions.Services.Email.Renderers;

/// <summary>
/// Metadata contract carried on the MonthlyStatement notification row. The dispatcher writes this JSON
/// into <see cref="Notification.Metadata"/>; the renderer reads it back at send time.
/// </summary>
public sealed record MonthlyStatementMetadata(
    Guid TenantId,
    Guid SubscriptionId,
    Guid UserId,
    DateOnly FromDate,
    DateOnly ToDate,
    string WorkspaceLabel,
    string MonthLabel,
    string RecipientEmail,
    string? RecipientName);

/// <summary>
/// Renders the monthly statement email at send time. The statement artifacts (PDF/CSV) are REGENERATED
/// from the ledger via <see cref="IMonthlyStatementAttachmentBuilder"/> rather than stored on the row, so
/// the numbers reflect the ledger at send time. Drift vs the online snapshot taken when the row was queued
/// is accepted (REGEN-IS-CANONICAL). A builder failure surfaces as a render exception, which the email
/// dispatcher routes through its normal retry/backoff/deadletter path.
/// </summary>
/// <remarks>
/// Lives in Commerce.Subscriptions (not the Notifications module) because it depends on the statement
/// artifact/link builders defined here; the Notifications module cannot reference this module (circular).
/// Registered as <see cref="IEmailRenderer"/> in the Subscriptions module DI.
/// </remarks>
public sealed class MonthlyStatementRenderer(
    IMonthlyStatementAttachmentBuilder attachmentBuilder,
    IMonthlyStatementLinkBuilder linkBuilder,
    IConfiguration configuration,
    IEmailFooterService footerService) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions MetadataOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.MonthlyStatement;

    public async Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var metadata = ParseMetadata(notification.Metadata);
        var artifacts = await attachmentBuilder
            .BuildAsync(metadata.TenantId, metadata.FromDate, metadata.ToDate, cancellationToken)
            .ConfigureAwait(false);

        var links = linkBuilder.Build(metadata.FromDate, metadata.ToDate);
        var consoleBaseUrl = ResolveConsoleBaseUrl();
        var statementPageAbsoluteUrl = BuildAbsoluteUrl(consoleBaseUrl, links.StatementPagePath);
        var statementPdfAbsoluteUrl = BuildAbsoluteUrl(consoleBaseUrl, links.StatementPdfPath);
        var statementCsvAbsoluteUrl = BuildAbsoluteUrl(consoleBaseUrl, links.StatementCsvPath);

        var subject = $"Your statement for {metadata.MonthLabel} is ready";

        var plainTextBody =
            $"Your monthly statement for {metadata.MonthLabel} is attached as PDF and CSV.\n\n" +
            $"Period: {metadata.FromDate:yyyy-MM-dd} to {metadata.ToDate:yyyy-MM-dd}\n" +
            $"Net cash flow: {artifacts.Report.NetCashFlow:C2}\n" +
            $"Closing balance: {artifacts.Report.ClosingBalance:C2}\n\n" +
            $"Review the same statement online: {statementPageAbsoluteUrl}\n" +
            $"Related links: PDF {statementPdfAbsoluteUrl} | CSV {statementCsvAbsoluteUrl}";

        var htmlBody = $"""
            <p>Your monthly statement for <strong>{metadata.MonthLabel}</strong> is attached as PDF and CSV.</p>
            <p>
                <strong>Period:</strong> {metadata.FromDate:yyyy-MM-dd} to {metadata.ToDate:yyyy-MM-dd}<br />
                <strong>Net cash flow:</strong> {artifacts.Report.NetCashFlow:C2}<br />
                <strong>Closing balance:</strong> {artifacts.Report.ClosingBalance:C2}
            </p>
            <p>
                Review the same statement online:
                <a href="{statementPageAbsoluteUrl}">{statementPageAbsoluteUrl}</a>
            </p>
            <p>
                Related links:
                <a href="{statementPdfAbsoluteUrl}">PDF</a>
                |
                <a href="{statementCsvAbsoluteUrl}">CSV</a>
            </p>
            """;

        var (plain, html) = MergeFooter(plainTextBody, htmlBody, footerService.Build(notification));

        var attachments = artifacts.Attachments
            .Select(a => new EmailAttachment(a.FileName, a.ContentType, a.Content))
            .ToList();

        return new EmailMessage(
            metadata.RecipientEmail,
            subject,
            plain,
            html,
            metadata.RecipientName,
            attachments);
    }

    private static MonthlyStatementMetadata ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            throw new InvalidOperationException("Monthly statement notification is missing metadata.");
        }

        try
        {
            return JsonSerializer.Deserialize<MonthlyStatementMetadata>(metadataJson, MetadataOptions)
                ?? throw new InvalidOperationException("Monthly statement metadata could not be deserialized.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Monthly statement metadata is malformed.", ex);
        }
    }

    private string ResolveConsoleBaseUrl()
    {
        var configured = configuration["StatementEmails:ConsoleBaseUrl"]
            ?? configuration["NEXTAUTH_URL"]
            ?? configuration["NEXT_PUBLIC_URL"]
            ?? "http://localhost:3000";

        return configured.Trim().TrimEnd('/');
    }

    private static string BuildAbsoluteUrl(string baseUrl, string relativePath)
        => new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/", UriKind.Absolute), relativePath.TrimStart('/')).ToString();
}
