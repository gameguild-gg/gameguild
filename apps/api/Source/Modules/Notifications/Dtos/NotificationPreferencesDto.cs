namespace GameGuild.Modules.Notifications;

/// <summary> DTO for notification preferences </summary>
public class NotificationPreferencesDto {
  public bool EmailNotifications { get; set; } = true;

  public bool PushNotifications { get; set; } = true;

  public bool InAppNotifications { get; set; } = true;

  public bool SoundEnabled { get; set; } = true;

  public Dictionary<string, bool> TypePreferences { get; set; } = new Dictionary<string, bool>();
}