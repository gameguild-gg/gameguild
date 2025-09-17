using GameGuild.Modules.Notifications.Models;


namespace GameGuild.Modules.Notifications.Dtos;

/// <summary> DTO for creating a new notification </summary>
public class CreateNotificationDto {
  public Guid UserId { get; set; }

  public Guid? FromUserId { get; set; }

  public Guid? TenantId { get; set; }

  public NotificationType Type { get; set; }

  public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;

  public string Title { get; set; } = string.Empty;

  public string Message { get; set; } = string.Empty;

  public string? ActionUrl { get; set; }

  public string? ActionText { get; set; }

  public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary> DTO for notification response </summary>
public class NotificationDto {
  public Guid Id { get; set; }

  public NotificationType Type { get; set; }

  public NotificationPriority Priority { get; set; }

  public NotificationStatus Status { get; set; }

  public string Title { get; set; } = string.Empty;

  public string Message { get; set; } = string.Empty;

  public bool IsStarred { get; set; }

  public string? ActionUrl { get; set; }

  public string? ActionText { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? ReadAt { get; set; }

  public DateTime? ArchivedAt { get; set; }

  public Dictionary<string, object>? Metadata { get; set; }

  public NotificationUserDto? FromUser { get; set; }
}

/// <summary> DTO for notification user info </summary>
public class NotificationUserDto {
  public Guid Id { get; set; }

  public string Name { get; set; } = string.Empty;

  public string? Avatar { get; set; }
}

/// <summary> DTO for notification queries </summary>
public class NotificationQueryDto {
  public NotificationStatus? Status { get; set; }

  public NotificationType? Type { get; set; }

  public NotificationPriority? Priority { get; set; }

  public bool? IsStarred { get; set; }

  public int Skip { get; set; } = 0;

  public int Take { get; set; } = 20;
}

/// <summary> DTO for notification response with pagination </summary>
public class NotificationResponseDto {
  public List<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();

  public int UnreadCount { get; set; }

  public int TotalCount { get; set; }

  public bool HasMore { get; set; }
}

/// <summary> DTO for bulk notification actions </summary>
public class BulkNotificationActionDto {
  public List<Guid> NotificationIds { get; set; } = new List<Guid>();

  public string Action { get; set; } = string.Empty; // "read", "archive", "star", "unstar"
}

/// <summary> DTO for notification preferences </summary>
public class NotificationPreferencesDto {
  public bool EmailNotifications { get; set; } = true;

  public bool PushNotifications { get; set; } = true;

  public bool InAppNotifications { get; set; } = true;

  public bool SoundEnabled { get; set; } = true;

  public Dictionary<string, bool> TypePreferences { get; set; } = new Dictionary<string, bool>();
}
