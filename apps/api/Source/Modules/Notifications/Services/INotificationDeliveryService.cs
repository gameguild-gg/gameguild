using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Service for delivering notifications across channels.
/// </summary>
public interface INotificationDeliveryService
{
    Task DeliverAsync(Notification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of notification delivery service.
/// </summary>
public sealed class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly ILogger<NotificationDeliveryService> _logger;
    private readonly IEmailNotificationProvider? _emailProvider;
    private readonly IPushNotificationProvider? _pushProvider;
    private readonly ISmsNotificationProvider? _smsProvider;

    public NotificationDeliveryService(
        ILogger<NotificationDeliveryService> logger,
        IEmailNotificationProvider? emailProvider = null,
        IPushNotificationProvider? pushProvider = null,
        ISmsNotificationProvider? smsProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailProvider = emailProvider;
        _pushProvider = pushProvider;
        _smsProvider = smsProvider;
    }

    public async Task DeliverAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var deliveryTasks = new List<Task>();

        // In-app is always delivered (stored in database)
        if (notification.Channel.HasFlag(NotificationChannel.InApp))
        {
            notification.IsDelivered = true;
            notification.DeliveredAt = DateTime.UtcNow;
        }

        // Email delivery
        if (notification.Channel.HasFlag(NotificationChannel.Email) && _emailProvider != null)
        {
            deliveryTasks.Add(DeliverEmailAsync(notification, cancellationToken));
        }

        // Push delivery
        if (notification.Channel.HasFlag(NotificationChannel.Push) && _pushProvider != null)
        {
            deliveryTasks.Add(DeliverPushAsync(notification, cancellationToken));
        }

        // SMS delivery
        if (notification.Channel.HasFlag(NotificationChannel.Sms) && _smsProvider != null)
        {
            deliveryTasks.Add(DeliverSmsAsync(notification, cancellationToken));
        }

        await Task.WhenAll(deliveryTasks);
    }

    private async Task DeliverEmailAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _emailProvider!.SendAsync(notification, cancellationToken);
            _logger.LogInformation("Email notification {NotificationId} delivered", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver email notification {NotificationId}", notification.Id);
        }
    }

    private async Task DeliverPushAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _pushProvider!.SendAsync(notification, cancellationToken);
            _logger.LogInformation("Push notification {NotificationId} delivered", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver push notification {NotificationId}", notification.Id);
        }
    }

    private async Task DeliverSmsAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _smsProvider!.SendAsync(notification, cancellationToken);
            _logger.LogInformation("SMS notification {NotificationId} delivered", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver SMS notification {NotificationId}", notification.Id);
        }
    }
}

/// <summary>
/// Email notification provider interface.
/// </summary>
public interface IEmailNotificationProvider
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Push notification provider interface.
/// </summary>
public interface IPushNotificationProvider
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// SMS notification provider interface.
/// </summary>
public interface ISmsNotificationProvider
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
