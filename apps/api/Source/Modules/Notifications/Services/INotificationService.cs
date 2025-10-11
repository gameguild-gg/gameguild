namespace GameGuild.Modules.Notifications;

/// <summary>
/// Service for managing user notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a user.
    /// </summary>
    Task<Notification> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a templated notification.
    /// </summary>
    Task<Notification> SendTemplatedNotificationAsync(SendTemplatedNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends bulk notifications to multiple users.
    /// </summary>
    Task<List<Notification>> SendBulkNotificationsAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a user.
    /// </summary>
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, NotificationFilter? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks notification as read.
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all user notifications as read.
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification.
    /// </summary>
    Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notification count.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to send a notification.
/// </summary>
public sealed class SendNotificationRequest
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required NotificationType Type { get; init; }
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;
    public required string Title { get; init; }
    public required string Content { get; init; }
    public NotificationChannel Channels { get; init; } = NotificationChannel.InApp;
    public string? ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, object>? Data { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Request to send a templated notification.
/// </summary>
public sealed class SendTemplatedNotificationRequest
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required string TemplateId { get; init; }
    public required Dictionary<string, string> Variables { get; init; }
    public NotificationChannel? ChannelOverride { get; init; }
    public NotificationPriority? PriorityOverride { get; init; }
    public Dictionary<string, object>? Data { get; init; }
}

/// <summary>
/// Notification filter.
/// </summary>
public sealed class NotificationFilter
{
    public bool? IsRead { get; init; }
    public NotificationType? Type { get; init; }
    public NotificationPriority? Priority { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
