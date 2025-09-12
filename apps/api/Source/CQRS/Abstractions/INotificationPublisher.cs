namespace GameGuild.CQRS;

/// <summary>
/// Notification publisher interface
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to all handlers
    /// </summary>
    /// <param name="handlerExecutors">Handler executors</param>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken);
}
