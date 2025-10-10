using GameGuild.Core.Domain;

namespace GameGuild.Modules.Users.Entities;

/// <summary>
/// Represents a user's communication preferences including channel selection,
/// locale/timezone settings, and scheduling preferences (quiet hours, delivery windows).
/// Extends beyond simple notification toggles to include sophisticated delivery scheduling.
/// </summary>
public class CommunicationPreference : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the user who owns this preference.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the communication channel (Email, SMS, Push, InApp, WebPush).
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this channel is enabled for the user.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the preferred locale (e.g., "en-US", "fr-FR").
    /// Used for message localization for this channel.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the IANA timezone identifier (e.g., "America/New_York").
    /// Used for time-based scheduling and quiet hours enforcement.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Gets or sets the quiet hours start time (local time in user's timezone).
    /// Format: "HH:mm" (e.g., "22:00" for 10 PM).
    /// Messages will not be sent during quiet hours.
    /// </summary>
    public string? QuietHoursStart { get; set; }

    /// <summary>
    /// Gets or sets the quiet hours end time (local time in user's timezone).
    /// Format: "HH:mm" (e.g., "08:00" for 8 AM).
    /// </summary>
    public string? QuietHoursEnd { get; set; }

    /// <summary>
    /// Gets or sets the preferred delivery window start (local time).
    /// Format: "HH:mm". Messages will be queued until this time if sent before.
    /// </summary>
    public string? PreferredDeliveryStart { get; set; }

    /// <summary>
    /// Gets or sets the preferred delivery window end (local time).
    /// Format: "HH:mm". Messages sent after this time will be queued until next window.
    /// </summary>
    public string? PreferredDeliveryEnd { get; set; }

    /// <summary>
    /// Gets or sets the days of the week when delivery is allowed (comma-separated).
    /// Format: "Monday,Tuesday,Wednesday,Thursday,Friday" (empty = all days allowed).
    /// </summary>
    public string? AllowedDeliveryDays { get; set; }

    /// <summary>
    /// Gets or sets whether to respect quiet hours for urgent/critical messages.
    /// If false, urgent messages bypass quiet hours restrictions.
    /// </summary>
    public bool RespectQuietHoursForUrgent { get; set; }

    /// <summary>
    /// Gets or sets the frequency for digest/summary messages.
    /// Values: Realtime, Hourly, Daily, Weekly, Never
    /// </summary>
    public string? DigestFrequency { get; set; }

    /// <summary>
    /// Gets or sets the preferred time for digest delivery (local time).
    /// Format: "HH:mm" (e.g., "09:00" for 9 AM). Only used when DigestFrequency is set.
    /// </summary>
    public string? DigestDeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets additional channel-specific settings (JSON format).
    /// Can include channel-specific options like email format (HTML/plain), SMS max length, etc.
    /// </summary>
    public string? ChannelSettings { get; set; }

    /// <summary>
    /// Gets or sets the priority threshold for this channel.
    /// Only messages with priority >= this value will be sent.
    /// Values: 0 (all), 1 (low), 2 (normal), 3 (high), 4 (urgent).
    /// </summary>
    public int PriorityThreshold { get; set; }

    /// <summary>
    /// Gets or sets the date when these preferences were last reviewed/updated by the user.
    /// </summary>
    public DateTime? LastReviewedAt { get; set; }

    /// <summary>
    /// Enables this communication channel.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables this communication channel.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the quiet hours settings.
    /// </summary>
    public void SetQuietHours(string start, string end, bool respectForUrgent = false)
    {
        QuietHoursStart = start;
        QuietHoursEnd = end;
        RespectQuietHoursForUrgent = respectForUrgent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the preferred delivery window.
    /// </summary>
    public void SetDeliveryWindow(string start, string end)
    {
        PreferredDeliveryStart = start;
        PreferredDeliveryEnd = end;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the preferences as reviewed by the user.
    /// </summary>
    public void MarkReviewed()
    {
        LastReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a scheduled communication delivery (queued message awaiting optimal delivery time).
/// </summary>
public class ScheduledCommunication : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the user who will receive this communication.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the communication channel.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content (may be a template key or actual content).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message subject/title (for email, push notifications).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the message priority (0-4: All, Low, Normal, High, Urgent).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets when this message was originally queued.
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// Gets or sets the scheduled delivery time (UTC).
    /// </summary>
    public DateTime ScheduledDeliveryAt { get; set; }

    /// <summary>
    /// Gets or sets the actual delivery time (null if not yet delivered).
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the number of delivery attempts made.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the last error message (if delivery failed).
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets additional metadata (JSON format).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets whether this message is due for delivery.
    /// </summary>
    public bool IsDue => Status == DeliveryStatus.Scheduled && ScheduledDeliveryAt <= DateTime.UtcNow;

    /// <summary>
    /// Marks the message as successfully delivered.
    /// </summary>
    public void MarkDelivered()
    {
        Status = DeliveryStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed delivery attempt.
    /// </summary>
    public void RecordFailure(string error)
    {
        DeliveryAttempts++;
        LastError = error;
        Status = DeliveryAttempts >= 3 ? DeliveryStatus.Failed : DeliveryStatus.Retrying;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the scheduled delivery.
    /// </summary>
    public void Cancel()
    {
        Status = DeliveryStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Delivery status for scheduled communications.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Message is scheduled and awaiting delivery time.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Message was successfully delivered.
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Delivery failed and is being retried.
    /// </summary>
    Retrying = 2,

    /// <summary>
    /// Delivery failed after all retry attempts.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Message delivery was cancelled.
    /// </summary>
    Cancelled = 4
}
