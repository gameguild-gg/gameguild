namespace GameGuild.Modules.Notifications;

/// <summary>
/// User notification preferences.
/// </summary>
public sealed class NotificationPreference
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required NotificationType NotificationType { get; init; }
    public NotificationChannel EnabledChannels { get; set; } = NotificationChannel.All;
    public bool IsEnabled { get; set; } = true;
    public TimeSpan? QuietHoursStart { get; init; }
    public TimeSpan? QuietHoursEnd { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
