using System.Text.RegularExpressions;
using GameGuild.Abstractions;
using GameGuild.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Notifications.Services;

/// <summary>
/// Service implementation for managing notifications
/// </summary>
public partial class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Notification CRUD

    public async Task<Result<Notification>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null)
        {
            return Result.Failure<Notification>(Error.NotFound("Notification.NotFound", $"Notification with ID {id} not found"));
        }

        return Result.Success(notification);
    }

    public async Task<Result<IEnumerable<Notification>>> GetUserNotificationsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Notification>()
            .Where(n => n.RecipientId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Notification>>(notifications);
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await _context.Set<Notification>()
            .CountAsync(n => n.RecipientId == userId && !n.IsRead, cancellationToken);

        return Result.Success(count);
    }

    public async Task<Result<Notification>> SendAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // Check user preferences
        var shouldSend = await ShouldSendNotificationAsync(recipientId, type, channel, priority, cancellationToken);
        if (!shouldSend)
        {
            _logger.LogDebug("Notification skipped due to user preferences. UserId: {UserId}, Type: {Type}", recipientId, type);
            return Result.Failure<Notification>(Error.Failure("Notification.Skipped", "Notification skipped due to user preferences"));
        }

        var notification = Notification.Create(
            recipientId,
            type,
            channel,
            title,
            message,
            tenantId,
            actionUrl,
            null,
            priority,
            null,
            referenceEntityId,
            referenceEntityType,
            metadata);

        _context.Set<Notification>().Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Mark as sent for in-app notifications (immediate delivery)
        if (channel == NotificationChannel.InApp)
        {
            notification.MarkAsSent();
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Notification sent. Id: {NotificationId}, Recipient: {RecipientId}, Type: {Type}",
            notification.Id, recipientId, type);

        return Result.Success(notification);
    }

    public async Task<Result<Notification>> SendFromTemplateAsync(
        Guid recipientId,
        string templateCode,
        Dictionary<string, string> placeholders,
        Guid? tenantId = null,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, cancellationToken);

        if (template == null)
        {
            return Result.Failure<Notification>(Error.NotFound("Template.NotFound", $"Active template with code '{templateCode}' not found"));
        }

        var title = ReplacePlaceholders(template.TitleTemplate, placeholders);
        var message = ReplacePlaceholders(template.MessageTemplate, placeholders);
        var actionUrl = template.ActionUrlTemplate != null 
            ? ReplacePlaceholders(template.ActionUrlTemplate, placeholders)
            : null;

        return await SendAsync(
            recipientId,
            template.Type,
            title,
            message,
            template.Channel,
            tenantId,
            actionUrl,
            template.DefaultPriority,
            referenceEntityId,
            referenceEntityType,
            null,
            cancellationToken);
    }

    public async Task<Result<IEnumerable<Notification>>> SendBulkAsync(
        IEnumerable<Guid> recipientIds,
        NotificationType type,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        var notifications = new List<Notification>();

        foreach (var recipientId in recipientIds)
        {
            var shouldSend = await ShouldSendNotificationAsync(recipientId, type, channel, priority, cancellationToken);
            if (!shouldSend) continue;

            var notification = Notification.Create(
                recipientId,
                type,
                channel,
                title,
                message,
                tenantId,
                actionUrl,
                null,
                priority);

            notifications.Add(notification);
        }

        if (notifications.Count > 0)
        {
            _context.Set<Notification>().AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);

            if (channel == NotificationChannel.InApp)
            {
                foreach (var notification in notifications)
                {
                    notification.MarkAsSent();
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation("Bulk notification sent. Count: {Count}, Type: {Type}", notifications.Count, type);

        return Result.Success<IEnumerable<Notification>>(notifications);
    }

    public async Task<Result<Notification>> ScheduleAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        DateTime scheduledAt,
        NotificationChannel channel = NotificationChannel.InApp,
        Guid? tenantId = null,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        if (scheduledAt <= DateTime.UtcNow)
        {
            return Result.Failure<Notification>(Error.Validation("Notification.InvalidSchedule", "Scheduled time must be in the future"));
        }

        var notification = Notification.Create(
            recipientId,
            type,
            channel,
            title,
            message,
            tenantId,
            actionUrl,
            null,
            priority,
            scheduledAt);

        _context.Set<Notification>().Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification scheduled. Id: {NotificationId}, ScheduledAt: {ScheduledAt}",
            notification.Id, scheduledAt);

        return Result.Success(notification);
    }

    #endregion

    #region Notification Status

    public async Task<Result> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure(Error.NotFound("Notification.NotFound", $"Notification with ID {notificationId} not found"));
        }

        notification.MarkAsRead();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _context.Set<Notification>()
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unreadNotifications.Count, userId);

        return Result.Success();
    }

    public async Task<Result> MarkAsUnreadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure(Error.NotFound("Notification.NotFound", $"Notification with ID {notificationId} not found"));
        }

        notification.MarkAsUnread();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure(Error.NotFound("Notification.NotFound", $"Notification with ID {notificationId} not found"));
        }

        notification.Delete();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<int>> DeleteReadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var readNotifications = await _context.Set<Notification>()
            .Where(n => n.RecipientId == userId && n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in readNotifications)
        {
            notification.Delete();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(readNotifications.Count);
    }

    #endregion

    #region User Preferences

    public async Task<Result<NotificationPreference>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preferences == null)
        {
            // Create default preferences if none exist
            preferences = NotificationPreference.CreateDefault(userId);
            _context.Set<NotificationPreference>().Add(preferences);
            await _context.SaveChangesAsync(cancellationToken);
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
        var preferencesResult = await GetPreferencesAsync(userId, cancellationToken);
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

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(preferences);
    }

    public async Task<Result> SetQuietHoursAsync(
        Guid userId,
        TimeOnly? start,
        TimeOnly? end,
        string? timezone = null,
        CancellationToken cancellationToken = default)
    {
        var preferencesResult = await GetPreferencesAsync(userId, cancellationToken);
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

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    #endregion

    #region Template Management

    public async Task<Result<NotificationTemplate>> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var template = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        if (template == null)
        {
            return Result.Failure<NotificationTemplate>(Error.NotFound("Template.NotFound", $"Template with code '{code}' not found"));
        }

        return Result.Success(template);
    }

    public async Task<Result<IEnumerable<NotificationTemplate>>> GetTemplatesAsync(
        string? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<NotificationTemplate>().AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<NotificationTemplate>>(templates);
    }

    public async Task<Result<NotificationTemplate>> CreateTemplateAsync(
        string code,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string messageTemplate,
        string? description = null,
        string? actionUrlTemplate = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate code
        var existingTemplate = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        if (existingTemplate != null)
        {
            return Result.Failure<NotificationTemplate>(Error.Conflict("Template.DuplicateCode", $"Template with code '{code}' already exists"));
        }

        var template = NotificationTemplate.Create(
            code,
            name,
            type,
            channel,
            titleTemplate,
            messageTemplate,
            description,
            actionUrlTemplate,
            null,
            NotificationPriority.Normal,
            null,
            category);

        _context.Set<NotificationTemplate>().Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification template created. Id: {TemplateId}, Code: {Code}", template.Id, code);

        return Result.Success(template);
    }

    public async Task<Result<NotificationTemplate>> UpdateTemplateAsync(
        Guid templateId,
        string? titleTemplate = null,
        string? messageTemplate = null,
        string? actionUrlTemplate = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result.Failure<NotificationTemplate>(Error.NotFound("Template.NotFound", $"Template with ID {templateId} not found"));
        }

        if (titleTemplate != null || messageTemplate != null || actionUrlTemplate != null)
        {
            template.UpdateContent(
                titleTemplate ?? template.TitleTemplate,
                messageTemplate ?? template.MessageTemplate,
                actionUrlTemplate ?? template.ActionUrlTemplate,
                template.DefaultIconUrl);
        }

        if (isActive.HasValue)
        {
            if (isActive.Value)
            {
                template.Activate();
            }
            else
            {
                template.Deactivate();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(template);
    }

    #endregion

    #region Private Helpers

    private async Task<bool> ShouldSendNotificationAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        NotificationPriority priority,
        CancellationToken cancellationToken)
    {
        var preferences = await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preferences == null)
        {
            return true; // Default to sending if no preferences exist
        }

        // Check channel preferences
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

        // Check category preferences
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

        // Check quiet hours (high priority can bypass)
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

        // Handle overnight quiet hours (e.g., 22:00 to 07:00)
        if (start > end)
        {
            return now >= start || now <= end;
        }

        return now >= start && now <= end;
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var placeholder in placeholders)
        {
            result = result.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }
        return result;
    }

    #endregion
}
