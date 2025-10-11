namespace GameGuild.Modules.Notifications;

/// <summary> DTO for notification user info </summary>
public class NotificationUserDto {
  public Guid Id { get; set; }

  public string Name { get; set; } = string.Empty;

  public string? Avatar { get; set; }
}