namespace GameGuild.CQRS;

/// <summary>
/// Publisher that executes handlers without waiting for completion (fire and forget) with optimized O(n) enumeration
/// </summary>
public class NoWaitPublisher : INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to all handlers without waiting
    /// </summary>
    /// <param name="handlerExecutors">Handler executors</param>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task Publish(IEnumerable<NotificationHandlerExecutorBase> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handlerExecutors);

        // Optimize for fire-and-forget execution - O(n)
        foreach (var handler in handlerExecutors)
        {
            Task.Run(() => handler.ExecuteHandler(notification, cancellationToken), cancellationToken);
        }

        return Task.CompletedTask;
    }
}
