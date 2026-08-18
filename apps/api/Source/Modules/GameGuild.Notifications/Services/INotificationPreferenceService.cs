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
    /// Decides how a notification should be routed based on user preferences.
    /// Evaluation order: transactional bypass (EmailVerification/PasswordReset/MagicLink/TenantInvite or Urgent priority),
    /// digest routing, per-type mute, channel toggle, category toggle, quiet hours (hold for Email, drop for InApp).
    /// </summary>
    Task<NotificationDeliveryDecision> DecideDeliveryAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        NotificationPriority priority,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy boolean gate — true only when the decision is Send. Kept temporarily for the email dispatcher lane; removed with the dispatcher rework.
    /// </summary>
    [Obsolete("Use DecideDeliveryAsync instead. Returns true only for the Send decision.")]
    Task<bool> ShouldSendNotificationAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        NotificationPriority priority,
        CancellationToken cancellationToken = default);
}
