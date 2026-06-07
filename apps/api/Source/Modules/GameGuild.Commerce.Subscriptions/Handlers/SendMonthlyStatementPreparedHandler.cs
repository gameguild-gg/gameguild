using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

public sealed class SendMonthlyStatementPreparedHandler(
    INotificationService notificationService,
    IMonthlyStatementMailSender mailSender,
    ILogger<SendMonthlyStatementPreparedHandler> logger) : INotificationHandler<MonthlyStatementPreparedNotification>
{
    public async Task Handle(MonthlyStatementPreparedNotification notification, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            statementMonth = notification.MonthLabel,
            fromDate = $"{notification.FromDate:yyyy-MM-dd}",
            toDate = $"{notification.ToDate:yyyy-MM-dd}",
            statementPageUrl = notification.StatementPagePath,
            statementPdfUrl = notification.StatementPdfPath,
            statementCsvUrl = notification.StatementCsvPath,
            notification.StatementPageAbsoluteUrl,
            notification.StatementPdfAbsoluteUrl,
            notification.StatementCsvAbsoluteUrl,
        });

        await notificationService.SendAsync(
            recipientId: notification.RecipientId,
            type: NotificationType.Billing,
            title: $"Your statement for {notification.MonthLabel} is ready",
            message:
                $"Your monthly statement for {notification.MonthLabel} is now available. " +
                $"The PDF and CSV copies are attached to this email, and the {notification.WorkspaceLabel} has the same statement available online.",
            channel: NotificationChannel.InApp,
            tenantId: notification.TenantId,
            actionUrl: notification.StatementPagePath,
            priority: GameGuild.Notifications.NotificationPriority.Normal,
            referenceEntityId: notification.SubscriptionId,
            referenceEntityType: nameof(Subscription),
            metadata: metadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var plainTextBody =
            $"Your monthly statement for {notification.MonthLabel} is attached as PDF and CSV.\n\n" +
            $"Period: {notification.FromDate:yyyy-MM-dd} to {notification.ToDate:yyyy-MM-dd}\n" +
            $"Net cash flow: {notification.Artifacts.Report.NetCashFlow:C2}\n" +
            $"Closing balance: {notification.Artifacts.Report.ClosingBalance:C2}\n\n" +
            $"Review the same statement online: {notification.StatementPageAbsoluteUrl}\n" +
            $"Related links: PDF {notification.StatementPdfAbsoluteUrl} | CSV {notification.StatementCsvAbsoluteUrl}";

        var htmlBody = $"""
            <p>Your monthly statement for <strong>{notification.MonthLabel}</strong> is attached as PDF and CSV.</p>
            <p>
                <strong>Period:</strong> {notification.FromDate:yyyy-MM-dd} to {notification.ToDate:yyyy-MM-dd}<br />
                <strong>Net cash flow:</strong> {notification.Artifacts.Report.NetCashFlow:C2}<br />
                <strong>Closing balance:</strong> {notification.Artifacts.Report.ClosingBalance:C2}
            </p>
            <p>
                Review the same statement online:
                <a href="{notification.StatementPageAbsoluteUrl}">{notification.StatementPageAbsoluteUrl}</a>
            </p>
            <p>
                Related links:
                <a href="{notification.StatementPdfAbsoluteUrl}">PDF</a>
                |
                <a href="{notification.StatementCsvAbsoluteUrl}">CSV</a>
            </p>
            """;

        await mailSender.SendAsync(
            new MonthlyStatementEmailMessage(
                notification.RecipientEmail,
                $"Your statement for {notification.MonthLabel} is ready",
                plainTextBody,
                htmlBody,
                notification.Artifacts.Attachments,
                notification.RecipientName),
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Monthly statement delivered for tenant {TenantId} subscription {SubscriptionId} to {RecipientEmail}",
            notification.TenantId,
            notification.SubscriptionId,
            notification.RecipientEmail);
    }
}
