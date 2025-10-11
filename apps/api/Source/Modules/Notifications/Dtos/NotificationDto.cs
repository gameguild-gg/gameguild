namespace GameGuild.Modules.Notifications;

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