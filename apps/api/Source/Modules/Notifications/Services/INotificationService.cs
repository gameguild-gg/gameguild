using GameGuild.Modules.Notifications.Dtos;


namespace GameGuild.Modules.Notifications.Services;

/// <summary>
/// Service interface for notification management
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Create a new notification
    /// </summary>
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for a user with filtering and pagination
    /// </summary>
    Task<NotificationResponseDto> GetNotificationsAsync(Guid userId, NotificationQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<NotificationDto?> GetNotificationByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive a notification
    /// </summary>
    Task<bool> ArchiveNotificationAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle star status of a notification
    /// </summary>
    Task<bool> ToggleStarAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a notification
    /// </summary>
    Task<bool> DeleteNotificationAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform bulk actions on notifications
    /// </summary>
    Task<int> BulkActionAsync(Guid userId, BulkNotificationActionDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    Task<List<NotificationDto>> CreateBulkNotificationsAsync(List<CreateNotificationDto> notifications, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old archived notifications
    /// </summary>
    Task<int> CleanupOldNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
