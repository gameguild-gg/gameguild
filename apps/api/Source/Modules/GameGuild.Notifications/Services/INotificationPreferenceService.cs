namespace GameGuild.Notifications.Services;

/// <summary>
/// Service for managing user notification preferences and quiet hours
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Gets notification preferences for a user
    /// </summary>
    Task<Result<NotificationPreference>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates notification preferences for a user
    /// </summary>
    Task<Result<NotificationPreference>> UpdatePreferencesAsync(
        Guid userId,
        bool? emailEnabled = null,
        bool? pushEnabled = null,
        bool? inAppEnabled = null,
        bool? smsEnabled = null,
        bool? marketingEnabled = null,
        bool? socialEnabled = null,
        bool? learningEnabled = null,
        bool? achievementsEnabled = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets quiet hours for a user
    /// </summary>
    Task<Result> SetQuietHoursAsync(
        Guid userId,
        TimeOnly? start,
        TimeOnly? end,
        string? timezone = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a notification should be sent based on user preferences
    /// </summary>
    Task<bool> ShouldSendNotificationAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        NotificationPriority priority,
        CancellationToken cancellationToken = default);
}
