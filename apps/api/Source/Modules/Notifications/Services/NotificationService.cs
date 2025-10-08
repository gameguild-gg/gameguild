using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Implementation of notification service.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationRepository _repository;
    private readonly INotificationTemplateService _templateService;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly INotificationDeliveryService _deliveryService;

    public NotificationService(
        ILogger<NotificationService> logger,
        INotificationRepository repository,
        INotificationTemplateService templateService,
        INotificationPreferenceService preferenceService,
        INotificationDeliveryService deliveryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
    }

    public async Task<Notification> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending notification to user {UserId}", request.UserId);

            // Check user preferences
            var preferences = await _preferenceService.GetPreferencesAsync(request.UserId, request.TenantId, cancellationToken);
            var enabledChannels = FilterChannelsByPreferences(request.Channels, preferences, request.Type);

            if (enabledChannels == NotificationChannel.None)
            {
                _logger.LogInformation("User {UserId} has disabled all channels for {Type}", request.UserId, request.Type);
                return null!; // User has disabled this notification type
            }

            // Create notification
            var notification = new Notification
            {
                UserId = request.UserId,
                TenantId = request.TenantId,
                Type = request.Type,
                Priority = request.Priority,
                Title = request.Title,
                Content = request.Content,
                Channel = enabledChannels,
                ActionUrl = request.ActionUrl,
                ImageUrl = request.ImageUrl,
                Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                ExpiresAt = request.ExpiresAt
            };

            // Save to repository
            await _repository.CreateAsync(notification, cancellationToken);

            // Deliver via enabled channels
            await _deliveryService.DeliverAsync(notification, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent successfully to user {UserId}", notification.Id, request.UserId);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<Notification> SendTemplatedNotificationAsync(SendTemplatedNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending templated notification {TemplateId} to user {UserId}", request.TemplateId, request.UserId);

            // Get template
            var template = await _templateService.GetTemplateAsync(request.TemplateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template {request.TemplateId} not found");
            }

            // Render template
            var rendered = await _templateService.RenderTemplateAsync(template, request.Variables, cancellationToken);

            // Create notification request
            var notificationRequest = new SendNotificationRequest
            {
                UserId = request.UserId,
                TenantId = request.TenantId,
                Type = template.Type,
                Priority = request.PriorityOverride ?? template.DefaultPriority,
                Title = rendered.Title,
                Content = rendered.Content,
                Channels = request.ChannelOverride ?? template.DefaultChannels,
                Data = request.Data
            };

            return await SendNotificationAsync(notificationRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated notification {TemplateId} to user {UserId}", request.TemplateId, request.UserId);
            throw;
        }
    }

    public async Task<List<Notification>> SendBulkNotificationsAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken = default)
    {
        var notifications = new List<Notification>();

        foreach (var request in requests)
        {
            try
            {
                var notification = await SendNotificationAsync(request, cancellationToken);
                if (notification != null)
                {
                    notifications.Add(notification);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to user {UserId}", request.UserId);
            }
        }

        return notifications;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, NotificationFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByUserIdAsync(userId, filter, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _repository.GetByUserIdAsync(
            userId,
            new NotificationFilter { IsRead = false },
            cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification, cancellationToken);
        }

        _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
    }

    public async Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(notificationId, cancellationToken);
        _logger.LogInformation("Notification {NotificationId} deleted", notificationId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _repository.GetByUserIdAsync(
            userId,
            new NotificationFilter { IsRead = false },
            cancellationToken);

        return unread.Count;
    }

    private NotificationChannel FilterChannelsByPreferences(
        NotificationChannel requestedChannels,
        List<NotificationPreference> preferences,
        NotificationType type)
    {
        var preference = preferences.FirstOrDefault(p => p.NotificationType == type);
        if (preference == null || !preference.IsEnabled)
        {
            return NotificationChannel.None;
        }

        // Check quiet hours
        if (IsInQuietHours(preference))
        {
            // During quiet hours, only allow urgent notifications
            return NotificationChannel.InApp; // Fallback to in-app only
        }

        return requestedChannels & preference.EnabledChannels;
    }

    private bool IsInQuietHours(NotificationPreference preference)
    {
        if (!preference.QuietHoursStart.HasValue || !preference.QuietHoursEnd.HasValue)
        {
            return false;
        }

        var now = DateTime.UtcNow.TimeOfDay;
        var start = preference.QuietHoursStart.Value;
        var end = preference.QuietHoursEnd.Value;

        if (start < end)
        {
            return now >= start && now <= end;
        }
        else
        {
            // Quiet hours span midnight
            return now >= start || now <= end;
        }
    }
}
