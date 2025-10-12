namespace GameGuild.Modules.Notifications;
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
