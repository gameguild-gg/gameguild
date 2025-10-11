namespace GameGuild.Modules.Notifications;

/// <summary> DTO for notification response with pagination </summary>
public class NotificationResponseDto {
  public List<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();

  public int UnreadCount { get; set; }

  public int TotalCount { get; set; }

  public bool HasMore { get; set; }
}