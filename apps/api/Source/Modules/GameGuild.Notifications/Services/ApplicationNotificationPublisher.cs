using System.Text.Json;
using GameGuild.Notifications.Services;

namespace GameGuild.Notifications;

/// <summary>
///     Adapter from the shared cross-module notification contract to the Notifications module.
/// </summary>
public sealed class ApplicationNotificationPublisher(INotificationService notificationService) : IApplicationNotificationPublisher
{
    public async Task<ApplicationNotificationPublishResult> PublishAsync(
        ApplicationNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        var result = await notificationService.SendAsync(
                message.RecipientId,
                ToNotificationType(message.Type),
                message.Title,
                message.Message,
                NotificationChannel.InApp,
                message.TenantId,
                message.ActionUrl,
                ToNotificationPriority(message.Priority),
                message.ReferenceEntityId,
                message.ReferenceEntityType,
                message.Metadata is null ? null : JsonSerializer.Serialize(message.Metadata),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? ApplicationNotificationPublishResult.Success(result.Value.Id)
            : ApplicationNotificationPublishResult.Failure(result.Error.Description);
    }

    private static NotificationType ToNotificationType(string type)
        => Enum.TryParse<NotificationType>(type, ignoreCase: true, out var parsed) ? parsed : NotificationType.Custom;

    private static NotificationPriority ToNotificationPriority(string priority)
        => Enum.TryParse<NotificationPriority>(priority, ignoreCase: true, out var parsed) ? parsed : NotificationPriority.Normal;
}
