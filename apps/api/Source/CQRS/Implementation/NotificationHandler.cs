namespace GameGuild.CQRS;

/// <summary>
/// Notification handler for all notifications.
/// </summary>
/// <typeparam name="TNotification">The notification type</typeparam>
public abstract class NotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification asynchronously
    /// </summary>
    /// <param name="notification">Notification instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task INotificationHandler<TNotification>.Handle(TNotification notification, CancellationToken cancellationToken)
    {
        Handle(notification);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Override in a derived class for the handler logic
    /// </summary>
    /// <param name="notification">Notification instance</param>
    protected abstract void Handle(TNotification notification);
}
