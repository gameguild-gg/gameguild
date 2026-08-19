using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

public sealed class SendMonthlyStatementPreparedHandler(
    INotificationService notificationService,
    ILogger<SendMonthlyStatementPreparedHandler> logger) : INotificationHandler<MonthlyStatementPreparedNotification>
{
    public async Task Handle(MonthlyStatementPreparedNotification notification, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            statementMonth = notification.MonthLabel,
            fromDate = $"{notification.FromDate:yyyy-MM-dd}",
            toDate = $"{notification.ToDate:yyyy-MM-dd}",
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
            priority: GameGuild.Notifications.NotificationPriority.Normal,
            referenceEntityId: notification.SubscriptionId,
            referenceEntityType: nameof(Subscription),
            metadata: metadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Monthly statement InApp notification created for tenant {TenantId} subscription {SubscriptionId}",
            notification.TenantId,
            notification.SubscriptionId);
    }
}
