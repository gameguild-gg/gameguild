using GameGuild.Database;
using GameGuild.Modules.Notifications.Dtos;
using GameGuild.Modules.Notifications.Models;


namespace GameGuild.Modules.Notifications.Services;

/// <summary> Service implementation for notification management </summary>
public class NotificationService : INotificationService {
  private readonly ApplicationDbContext _context;

  private readonly ILogger<NotificationService> _logger;

  public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger) {
    _context = context;
    _logger = logger;
  }

  public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default) {
    try {
      // Check user preferences
      var preferences = await GetOrCreatePreferencesAsync(dto.UserId, cancellationToken);

      if (!preferences.InAppNotifications || !preferences.IsTypeEnabled(dto.Type)) {
        _logger.LogDebug("Notification creation skipped due to user preferences for user {UserId}", dto.UserId);

        throw new InvalidOperationException("User has disabled this type of notification");
      }

      var notification = new Notification {
        UserId = dto.UserId, FromUserId = dto.FromUserId, TenantId = dto.TenantId, Type = dto.Type, Priority = dto.Priority, Title = dto.Title, Message = dto.Message, ActionUrl = dto.ActionUrl, ActionText = dto.ActionText,
      };

      if (dto.Metadata != null) { notification.SetMetadata(dto.Metadata); }

      _context.Notifications.Add(notification);
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Created notification {NotificationId} for user {UserId}", notification.Id, dto.UserId);

      return await MapToDto(notification, cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error creating notification for user {UserId}", dto.UserId);

      throw;
    }
  }

  public async Task<NotificationResponseDto> GetNotificationsAsync(Guid userId, NotificationQueryDto query, CancellationToken cancellationToken = default) {
    try {
      var queryable = _context.Notifications.Where(n => n.UserId == userId);

      // Apply filters
      if (query.Status.HasValue) { queryable = queryable.Where(n => n.Status == query.Status.Value); }

      if (query.Type.HasValue) { queryable = queryable.Where(n => n.Type == query.Type.Value); }

      if (query.Priority.HasValue) { queryable = queryable.Where(n => n.Priority == query.Priority.Value); }

      if (query.IsStarred.HasValue) { queryable = queryable.Where(n => n.IsStarred == query.IsStarred.Value); }

      // Get total count
      var totalCount = await queryable.CountAsync(cancellationToken);

      // Get unread count
      var unreadCount = await _context.Notifications.Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread).CountAsync(cancellationToken);

      // Apply pagination and ordering
      var notifications = await queryable.OrderByDescending(n => n.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);

      var notificationDtos = new List<NotificationDto>();

      foreach (var notification in notifications) { notificationDtos.Add(await MapToDto(notification, cancellationToken)); }

      return new NotificationResponseDto { Notifications = notificationDtos, UnreadCount = unreadCount, TotalCount = totalCount, HasMore = query.Skip + query.Take < totalCount };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);

      throw;
    }
  }

  public async Task<NotificationDto?> GetNotificationByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) {
    var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

    return notification == null ? null : await MapToDto(notification, cancellationToken);
  }

  public async Task<bool> MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) {
    try {
      var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

      if (notification == null) return false;

      notification.MarkAsRead();
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogDebug("Marked notification {NotificationId} as read for user {UserId}", id, userId);

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", id, userId);

      return false;
    }
  }

  public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) {
    try {
      var notifications = await _context.Notifications.Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread).ToListAsync(cancellationToken);

      foreach (var notification in notifications) { notification.MarkAsRead(); }

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", notifications.Count, userId);

      return notifications.Count;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);

      throw;
    }
  }

  public async Task<bool> ArchiveNotificationAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) {
    try {
      var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

      if (notification == null) return false;

      notification.MarkAsArchived();
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogDebug("Archived notification {NotificationId} for user {UserId}", id, userId);

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error archiving notification {NotificationId} for user {UserId}", id, userId);

      return false;
    }
  }

  public async Task<bool> ToggleStarAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) {
    try {
      var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

      if (notification == null) return false;

      notification.ToggleStar();
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogDebug("Toggled star for notification {NotificationId} for user {UserId}", id, userId);

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error toggling star for notification {NotificationId} for user {UserId}", id, userId);

      return false;
    }
  }

  public async Task<bool> DeleteNotificationAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) {
    try {
      var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

      if (notification == null) return false;

      _context.Notifications.Remove(notification);
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogDebug("Deleted notification {NotificationId} for user {UserId}", id, userId);

      return true;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", id, userId);

      return false;
    }
  }

  public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) {
    return await _context.Notifications.Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread).CountAsync(cancellationToken);
  }

  public async Task<int> BulkActionAsync(Guid userId, BulkNotificationActionDto dto, CancellationToken cancellationToken = default) {
    try {
      var notifications = await _context.Notifications.Where(n => n.UserId == userId && dto.NotificationIds.Contains(n.Id)).ToListAsync(cancellationToken);

      foreach (var notification in notifications) {
        switch (dto.Action.ToLowerInvariant()) {
          case "read" : notification.MarkAsRead(); break;
          case "archive" : notification.MarkAsArchived(); break;

          case "star" :
            if (!notification.IsStarred) notification.ToggleStar();

            break;

          case "unstar" :
            if (notification.IsStarred) notification.ToggleStar();

            break;
        }
      }

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Performed bulk action {Action} on {Count} notifications for user {UserId}", dto.Action, notifications.Count, userId);

      return notifications.Count;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error performing bulk action {Action} for user {UserId}", dto.Action, userId);

      throw;
    }
  }

  public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default) {
    var preferences = await GetOrCreatePreferencesAsync(userId, cancellationToken);

    return new NotificationPreferencesDto {
      EmailNotifications = preferences.EmailNotifications,
      PushNotifications = preferences.PushNotifications,
      InAppNotifications = preferences.InAppNotifications,
      SoundEnabled = preferences.SoundEnabled,
      TypePreferences = new Dictionary<string, bool> {
        [nameof(NotificationType.Comment)] = preferences.CommentNotifications,
        [nameof(NotificationType.Follow)] = preferences.FollowNotifications,
        [nameof(NotificationType.Invite)] = preferences.InviteNotifications,
        [nameof(NotificationType.Reminder)] = preferences.ReminderNotifications,
        [nameof(NotificationType.Task)] = preferences.TaskNotifications,
        [nameof(NotificationType.Mention)] = preferences.MentionNotifications,
        [nameof(NotificationType.System)] = preferences.SystemNotifications,
        [nameof(NotificationType.Course)] = preferences.CourseNotifications,
        [nameof(NotificationType.Achievement)] = preferences.AchievementNotifications,
        [nameof(NotificationType.Social)] = preferences.SocialNotifications,
        [nameof(NotificationType.Promotion)] = preferences.PromotionNotifications,
      },
    };
  }

  public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto dto, CancellationToken cancellationToken = default) {
    try {
      var preferences = await GetOrCreatePreferencesAsync(userId, cancellationToken);

      preferences.EmailNotifications = dto.EmailNotifications;
      preferences.PushNotifications = dto.PushNotifications;
      preferences.InAppNotifications = dto.InAppNotifications;
      preferences.SoundEnabled = dto.SoundEnabled;

      // Update type preferences
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Comment), out var comment)) preferences.CommentNotifications = comment;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Follow), out var follow)) preferences.FollowNotifications = follow;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Invite), out var invite)) preferences.InviteNotifications = invite;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Reminder), out var reminder)) preferences.ReminderNotifications = reminder;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Task), out var task)) preferences.TaskNotifications = task;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Mention), out var mention)) preferences.MentionNotifications = mention;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.System), out var system)) preferences.SystemNotifications = system;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Course), out var course)) preferences.CourseNotifications = course;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Achievement), out var achievement)) preferences.AchievementNotifications = achievement;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Social), out var social)) preferences.SocialNotifications = social;
      if (dto.TypePreferences.TryGetValue(nameof(NotificationType.Promotion), out var promotion)) preferences.PromotionNotifications = promotion;

      preferences.UpdatedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

      return await GetPreferencesAsync(userId, cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating notification preferences for user {UserId}", userId);

      throw;
    }
  }

  public async Task<List<NotificationDto>> CreateBulkNotificationsAsync(List<CreateNotificationDto> notifications, CancellationToken cancellationToken = default) {
    try {
      var results = new List<NotificationDto>();

      foreach (var dto in notifications) {
        try {
          var result = await CreateNotificationAsync(dto, cancellationToken);
          results.Add(result);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to create notification for user {UserId}", dto.UserId); }
      }

      _logger.LogInformation("Created {Count} notifications out of {Total} requested", results.Count, notifications.Count);

      return results;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error creating bulk notifications");

      throw;
    }
  }

  public async Task<int> CleanupOldNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default) {
    try {
      var oldNotifications = await _context.Notifications.Where(n => n.Status == NotificationStatus.Archived && n.CreatedAt < olderThan).ToListAsync(cancellationToken);

      if (oldNotifications.Count > 0) {
        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} old notifications older than {Date}", oldNotifications.Count, olderThan);
      }

      return oldNotifications.Count;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error cleaning up old notifications");

      throw;
    }
  }

  private async Task<NotificationPreferences> GetOrCreatePreferencesAsync(Guid userId, CancellationToken cancellationToken) {
    var preferences = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    if (preferences == null) {
      preferences = new NotificationPreferences { UserId = userId };
      _context.NotificationPreferences.Add(preferences);
      await _context.SaveChangesAsync(cancellationToken);
    }

    return preferences;
  }

  private async Task<NotificationDto> MapToDto(Notification notification, CancellationToken cancellationToken) {
    NotificationUserDto? fromUser = null;

    if (notification.FromUserId.HasValue) {
      var user = await _context.Users.Where(u => u.Id == notification.FromUserId.Value)
                               .Select(u => new { u.Id, u.Username, Avatar = (string?) null }) // Adjust based on your User entity
                               .FirstOrDefaultAsync(cancellationToken);

      if (user != null) { fromUser = new NotificationUserDto { Id = user.Id, Name = user.Username, Avatar = user.Avatar }; }
    }

    return new NotificationDto {
      Id = notification.Id,
      Type = notification.Type,
      Priority = notification.Priority,
      Status = notification.Status,
      Title = notification.Title,
      Message = notification.Message,
      IsStarred = notification.IsStarred,
      ActionUrl = notification.ActionUrl,
      ActionText = notification.ActionText,
      CreatedAt = notification.CreatedAt,
      ReadAt = notification.ReadAt,
      ArchivedAt = notification.ArchivedAt,
      Metadata = notification.GetMetadata(),
      FromUser = fromUser,
    };
  }
}
