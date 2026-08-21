using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications;

/// <summary>
/// Represents a user's notification preferences
/// </summary>
[Table("NotificationPreferences")]
[Index(nameof(UserId), IsUnique = true)]
public class NotificationPreference : EntityBase
{
    /// <summary>
    /// ID of the user these preferences belong to
    /// </summary>
    [Required]
    public Guid UserId { get; private set; }

    /// <summary>
    /// Whether to receive email notifications
    /// </summary>
    public bool EmailEnabled { get; private set; } = true;

    /// <summary>
    /// Whether to receive push notifications
    /// </summary>
    public bool PushEnabled { get; private set; } = true;

    /// <summary>
    /// Whether to receive in-app notifications
    /// </summary>
    public bool InAppEnabled { get; private set; } = true;

    /// <summary>
    /// Whether to receive SMS notifications
    /// </summary>
    public bool SmsEnabled { get; private set; } = false;

    /// <summary>
    /// Whether to receive marketing/promotional notifications
    /// </summary>
    public bool MarketingEnabled { get; private set; } = true;

    /// <summary>
    /// Whether to receive social interaction notifications
    /// </summary>
    public bool SocialEnabled { get; private set; } = true;

    /// <summary>
    /// Whether to receive course/learning notifications
    /// </summary>
    public bool LearningEnabled { get; private set; } = true;

    /// <summary>
    /// Whether to receive achievement notifications
    /// </summary>
    public bool AchievementsEnabled { get; private set; } = true;

    /// <summary>
    /// Quiet hours start time (notifications are held until end time)
    /// </summary>
    public TimeOnly? QuietHoursStart { get; private set; }

    /// <summary>
    /// Quiet hours end time
    /// </summary>
    public TimeOnly? QuietHoursEnd { get; private set; }

    /// <summary>
    /// User's timezone for quiet hours calculation
    /// </summary>
    [MaxLength(50)]
    public string? Timezone { get; private set; }

    /// <summary>
    /// Email digest frequency (null = immediate delivery)
    /// </summary>
    public DigestFrequency? EmailDigestFrequency { get; private set; }

    /// <summary>
    /// Minimum priority level to bypass quiet hours
    /// </summary>
    public NotificationPriority QuietHoursBypassPriority { get; private set; } = NotificationPriority.Urgent;

    /// <summary>
    /// JSON array of notification types to mute
    /// </summary>
    [MaxLength(500)]
    public string? MutedTypes { get; private set; }

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private NotificationPreference() { }

    /// <summary>
    /// Creates default notification preferences for a user
    /// </summary>
    public static NotificationPreference CreateDefault(Guid userId)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailEnabled = true,
            PushEnabled = true,
            InAppEnabled = true,
            SmsEnabled = false,
            MarketingEnabled = true,
            SocialEnabled = true,
            LearningEnabled = true,
            AchievementsEnabled = true,
            QuietHoursBypassPriority = NotificationPriority.Urgent
        };
    }

    /// <summary>
    /// Updates channel preferences
    /// </summary>
    public void UpdateChannelPreferences(
        bool emailEnabled,
        bool pushEnabled,
        bool inAppEnabled,
        bool smsEnabled)
    {
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        InAppEnabled = inAppEnabled;
        SmsEnabled = smsEnabled;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Updates category preferences
    /// </summary>
    public void UpdateCategoryPreferences(
        bool marketingEnabled,
        bool socialEnabled,
        bool learningEnabled,
        bool achievementsEnabled)
    {
        MarketingEnabled = marketingEnabled;
        SocialEnabled = socialEnabled;
        LearningEnabled = learningEnabled;
        AchievementsEnabled = achievementsEnabled;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Sets quiet hours
    /// </summary>
    public void SetQuietHours(
        TimeOnly? start,
        TimeOnly? end,
        string? timezone,
        NotificationPriority bypassPriority = NotificationPriority.Urgent)
    {
        QuietHoursStart = start;
        QuietHoursEnd = end;
        Timezone = timezone;
        QuietHoursBypassPriority = bypassPriority;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Clears quiet hours
    /// </summary>
    public void ClearQuietHours()
    {
        QuietHoursStart = null;
        QuietHoursEnd = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Sets email digest frequency
    /// </summary>
    public void SetEmailDigestFrequency(DigestFrequency? frequency)
    {
        EmailDigestFrequency = frequency;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Mutes specific notification types
    /// </summary>
    public void SetMutedTypes(string? mutedTypesJson)
    {
        MutedTypes = mutedTypesJson;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Parses <see cref="MutedTypes"/> (JSON array of notification type names, case-insensitive).
    /// Malformed or missing JSON yields an empty set.
    /// </summary>
    public IReadOnlySet<string> GetMutedTypeNames()
    {
        if (string.IsNullOrWhiteSpace(MutedTypes))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var names = JsonSerializer.Deserialize<string[]>(MutedTypes);
            return names == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Adds a notification type name to the mute list
    /// </summary>
    public void MuteType(string typeName)
    {
        var names = new HashSet<string>(GetMutedTypeNames(), StringComparer.OrdinalIgnoreCase) { typeName };
        SetMutedTypes(JsonSerializer.Serialize(names));
    }

    /// <summary>
    /// Removes a notification type name from the mute list
    /// </summary>
    public void UnmuteType(string typeName)
    {
        var names = new HashSet<string>(GetMutedTypeNames(), StringComparer.OrdinalIgnoreCase);
        if (names.Remove(typeName))
        {
            SetMutedTypes(JsonSerializer.Serialize(names));
        }
    }
}

/// <summary>
/// Frequency for email digest delivery
/// </summary>
public enum DigestFrequency
{
    /// <summary>Daily digest at end of day</summary>
    Daily = 0,

    /// <summary>Weekly digest</summary>
    Weekly = 1,

    /// <summary>Bi-weekly digest</summary>
    BiWeekly = 2
}
