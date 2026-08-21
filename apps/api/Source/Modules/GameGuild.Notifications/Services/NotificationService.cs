namespace GameGuild.Notifications.Services;

/// <summary>
/// Facade that delegates to focused sub-services for backward compatibility.
/// </summary>
public class NotificationService(
    INotificationDeliveryService deliveryService,
    INotificationPreferenceService preferenceService,
    INotificationTemplateService templateService) : INotificationService
{
    #region Notification CRUD

    public Task<Result<Notification>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => deliveryService.GetByIdAsync(id, cancellationToken);

    public Task<Result<IEnumerable<Notification>>> GetUserNotificationsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
        => deliveryService.GetUserNotificationsAsync(userId, skip, take, isRead, cancellationToken);

    public Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => deliveryService.GetUnreadCountAsync(userId, cancellationToken);

    public Task<Result<Notification>> SendAsync(
        Guid? recipientId,
        NotificationType type,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null,
        string? metadata = null,
        string? recipientEmail = null,
        CancellationToken cancellationToken = default)
        => deliveryService.SendAsync(recipientId, type, title, message, channel, tenantId, actionUrl, priority, referenceEntityId, referenceEntityType, metadata, recipientEmail, cancellationToken);

    public Task<Result<Notification>> SendFromTemplateAsync(
        Guid recipientId,
        string templateCode,
        Dictionary<string, string> placeholders,
        Guid? tenantId = null,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null,
        CancellationToken cancellationToken = default)
        => deliveryService.SendFromTemplateAsync(recipientId, templateCode, placeholders, tenantId, referenceEntityId, referenceEntityType, cancellationToken);

    public Task<Result<IEnumerable<Notification>>> SendBulkAsync(
        IEnumerable<Guid> recipientIds,
        NotificationType type,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default)
        => deliveryService.SendBulkAsync(recipientIds, type, title, message, channel, tenantId, actionUrl, priority, cancellationToken);

    public Task<Result<Notification>> ScheduleAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        DateTime scheduledAt,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default)
        => deliveryService.ScheduleAsync(recipientId, type, title, message, scheduledAt, channel, tenantId, actionUrl, priority, cancellationToken);

    #endregion

    #region Notification Status

    public Task<Result> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => deliveryService.MarkAsReadAsync(notificationId, cancellationToken);

    public Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => deliveryService.MarkAllAsReadAsync(userId, cancellationToken);

    public Task<Result> MarkAsUnreadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => deliveryService.MarkAsUnreadAsync(notificationId, cancellationToken);

    public Task<Result> DeleteAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => deliveryService.DeleteAsync(notificationId, cancellationToken);

    public Task<Result<int>> DeleteReadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => deliveryService.DeleteReadNotificationsAsync(userId, cancellationToken);

    #endregion

    #region User Preferences

    public Task<Result<NotificationPreference>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
        => preferenceService.GetPreferencesAsync(userId, cancellationToken);

    public Task<Result<NotificationPreference>> UpdatePreferencesAsync(
        Guid userId,
        bool? emailEnabled = null,
        bool? pushEnabled = null,
        bool? inAppEnabled = null,
        bool? smsEnabled = null,
        bool? marketingEnabled = null,
        bool? socialEnabled = null,
        bool? learningEnabled = null,
        bool? achievementsEnabled = null,
        CancellationToken cancellationToken = default)
        => preferenceService.UpdatePreferencesAsync(userId, emailEnabled, pushEnabled, inAppEnabled, smsEnabled, marketingEnabled, socialEnabled, learningEnabled, achievementsEnabled, cancellationToken);

    public Task<Result> SetQuietHoursAsync(
        Guid userId,
        TimeOnly? start,
        TimeOnly? end,
        string? timezone = null,
        CancellationToken cancellationToken = default)
        => preferenceService.SetQuietHoursAsync(userId, start, end, timezone, cancellationToken);

    #endregion

    #region Template Management

    public Task<Result<NotificationTemplate>> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken = default)
        => templateService.GetTemplateByCodeAsync(code, cancellationToken);

    public Task<Result<IEnumerable<NotificationTemplate>>> GetTemplatesAsync(
        string? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
        => templateService.GetTemplatesAsync(category, isActive, cancellationToken);

    public Task<Result<NotificationTemplate>> CreateTemplateAsync(
        string code,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string messageTemplate,
        string? description = null,
        string? actionUrlTemplate = null,
        string? category = null,
        CancellationToken cancellationToken = default)
        => templateService.CreateTemplateAsync(code, name, type, channel, titleTemplate, messageTemplate, description, actionUrlTemplate, category, cancellationToken);

    public Task<Result<NotificationTemplate>> UpdateTemplateAsync(
        Guid templateId,
        string? titleTemplate = null,
        string? messageTemplate = null,
        string? actionUrlTemplate = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
        => templateService.UpdateTemplateAsync(templateId, titleTemplate, messageTemplate, actionUrlTemplate, isActive, cancellationToken);

    #endregion
}
