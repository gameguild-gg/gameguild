namespace GameGuild.Notifications.Services;

/// <summary>
/// Service for sending, scheduling, querying, and managing notification lifecycle
/// </summary>
public interface INotificationDeliveryService
{
    /// <summary>
    /// Gets a notification by ID
    /// </summary>
    Task<Result<Notification>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a user with pagination
    /// </summary>
    Task<Result<IEnumerable<Notification>>> GetUserNotificationsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        bool? isRead = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notification count for a user
    /// </summary>
    Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and sends a notification
    /// </summary>
    Task<Result<Notification>> SendAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification from a template
    /// </summary>
    Task<Result<Notification>> SendFromTemplateAsync(
        Guid recipientId,
        string templateCode,
        Dictionary<string, string> placeholders,
        Guid? tenantId = null,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to multiple recipients
    /// </summary>
    Task<Result<IEnumerable<Notification>>> SendBulkAsync(
        IEnumerable<Guid> recipientIds,
        NotificationType type,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a notification for future delivery
    /// </summary>
    Task<Result<Notification>> ScheduleAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        DateTime scheduledAt,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task<Result> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as unread
    /// </summary>
    Task<Result> MarkAsUnreadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification
    /// </summary>
    Task<Result> DeleteAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all read notifications for a user
    /// </summary>
    Task<Result<int>> DeleteReadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
