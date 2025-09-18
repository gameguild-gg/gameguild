namespace GameGuild.Modules.Notifications;

/// <summary> DTO for notification queries </summary>
public class NotificationQueryDto {
  public NotificationStatus? Status { get; set; }

  public NotificationType? Type { get; set; }

  public NotificationPriority? Priority { get; set; }

  public bool? IsStarred { get; set; }

  public int Skip { get; set; } = 0;

  public int Take { get; set; } = 20;
}