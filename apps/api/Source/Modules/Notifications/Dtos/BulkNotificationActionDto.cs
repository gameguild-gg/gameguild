namespace GameGuild.Modules.Notifications;

/// <summary> DTO for bulk notification actions </summary>
public class BulkNotificationActionDto {
  public List<Guid> NotificationIds { get; set; } = new List<Guid>();

  public string Action { get; set; } = string.Empty; // "read", "archive", "star", "unstar"
}