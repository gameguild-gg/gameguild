namespace GameGuild.Modules.Users;

/// <summary>
/// Represents user preferences for notifications and communication
/// </summary>
[Table("user_preferences")]
public sealed class UserPreference : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Preferred language/locale (e.g., en-US, es-ES)
    /// </summary>
    [MaxLength(10)]
    public string? Locale { get; set; } = "en-US";

    /// <summary>
    /// Preferred timezone (IANA timezone identifier)
    /// </summary>
    [MaxLength(50)]
    public string? Timezone { get; set; } = "UTC";

    /// <summary>
    /// Email notification enabled
    /// </summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// SMS notification enabled
    /// </summary>
    public bool SmsNotificationsEnabled { get; set; } = false;

    /// <summary>
    /// Push notification enabled
    /// </summary>
    public bool PushNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// In-app notification enabled
    /// </summary>
    public bool InAppNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Web push notification enabled
    /// </summary>
    public bool WebPushNotificationsEnabled { get; set; } = false;

    /// <summary>
    /// Email digest frequency
    /// </summary>
    public DigestFrequency EmailDigestFrequency { get; set; } = DigestFrequency.Daily;

    /// <summary>
    /// Marketing emails enabled
    /// </summary>
    public bool MarketingEmailsEnabled { get; set; } = false;

    /// <summary>
    /// Product updates enabled
    /// </summary>
    public bool ProductUpdatesEnabled { get; set; } = true;

    /// <summary>
    /// Security alerts enabled (always on, cannot be disabled)
    /// </summary>
    public bool SecurityAlertsEnabled { get; set; } = true;

    /// <summary>
    /// Theme preference (light, dark, auto)
    /// </summary>
    [MaxLength(20)]
    public string Theme { get; set; } = "auto";

    /// <summary>
    /// Accessibility mode enabled
    /// </summary>
    public bool AccessibilityMode { get; set; } = false;
}

/// <summary>
/// Frequency for digest notifications
/// </summary>
public enum DigestFrequency
{
    Realtime = 0,
    Hourly = 1,
    Daily = 2,
    Weekly = 3,
    Never = 4
}

/// <summary>
/// Communication channels for notifications
/// </summary>
public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Push = 2,
    InApp = 3,
    WebPush = 4
}
