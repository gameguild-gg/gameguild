using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications.Services;

/// <summary>
/// Manages user notification preferences and quiet hours
/// </summary>
public class NotificationPreferenceService(
    IApplicationDbContext context) : INotificationPreferenceService
{
    public async Task<Result<NotificationPreference>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (preferences == null)
        {
            preferences = NotificationPreference.CreateDefault(userId);
            context.Set<NotificationPreference>().Add(preferences);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(preferences);
    }

    public async Task<Result<NotificationPreference>> UpdatePreferencesAsync(
        Guid userId,
        bool? emailEnabled = null,
        bool? pushEnabled = null,
        bool? inAppEnabled = null,
        bool? smsEnabled = null,
        bool? marketingEnabled = null,
        bool? socialEnabled = null,
        bool? learningEnabled = null,
        bool? achievementsEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var preferencesResult = await GetPreferencesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (!preferencesResult.IsSuccess)
        {
            return preferencesResult;
        }

        var preferences = preferencesResult.Value;

        preferences.UpdateChannelPreferences(
            emailEnabled ?? preferences.EmailEnabled,
            pushEnabled ?? preferences.PushEnabled,
            inAppEnabled ?? preferences.InAppEnabled,
            smsEnabled ?? preferences.SmsEnabled);

        preferences.UpdateCategoryPreferences(
            marketingEnabled ?? preferences.MarketingEnabled,
            socialEnabled ?? preferences.SocialEnabled,
            learningEnabled ?? preferences.LearningEnabled,
            achievementsEnabled ?? preferences.AchievementsEnabled);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(preferences);
    }

    public async Task<Result> SetQuietHoursAsync(
        Guid userId,
        TimeOnly? start,
        TimeOnly? end,
        string? timezone = null,
        CancellationToken cancellationToken = default)
    {
        var preferencesResult = await GetPreferencesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (!preferencesResult.IsSuccess)
        {
            return Result.Failure(preferencesResult.Error);
        }

        var preferences = preferencesResult.Value;

        if (start.HasValue && end.HasValue)
        {
            preferences.SetQuietHours(start, end, timezone);
        }
        else
        {
            preferences.ClearQuietHours();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<bool> ShouldSendNotificationAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        var preferences = await context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (preferences == null)
        {
            return true;
        }

        var channelEnabled = channel switch
        {
            NotificationChannel.Email => preferences.EmailEnabled,
            NotificationChannel.Push => preferences.PushEnabled,
            NotificationChannel.InApp => preferences.InAppEnabled,
            NotificationChannel.Sms => preferences.SmsEnabled,
            _ => true
        };

        if (!channelEnabled)
        {
            return false;
        }

        var categoryEnabled = type switch
        {
            NotificationType.Marketing => preferences.MarketingEnabled,
            NotificationType.SocialInteraction => preferences.SocialEnabled,
            NotificationType.CourseEnrollment or NotificationType.CourseCompletion or NotificationType.AssessmentReminder or NotificationType.AssessmentGraded => preferences.LearningEnabled,
            NotificationType.AchievementUnlocked or NotificationType.ProgressMilestone => preferences.AchievementsEnabled,
            _ => true
        };

        if (!categoryEnabled)
        {
            return false;
        }

        if (priority < preferences.QuietHoursBypassPriority && IsInQuietHours(preferences))
        {
            return false;
        }

        return true;
    }

    private static bool IsInQuietHours(NotificationPreference preferences)
    {
        if (!preferences.QuietHoursStart.HasValue || !preferences.QuietHoursEnd.HasValue)
        {
            return false;
        }

        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var start = preferences.QuietHoursStart.Value;
        var end = preferences.QuietHoursEnd.Value;

        if (start > end)
        {
            return now >= start || now <= end;
        }

        return now >= start && now <= end;
    }
}
