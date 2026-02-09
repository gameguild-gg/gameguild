using Microsoft.Extensions.Logging;

namespace GameGuild.CQRS.Publishers;

/// <summary>
///     Publisher that executes handlers without waiting for completion (fire and forget).
///     Exceptions are logged rather than silently swallowed.
/// </summary>
public sealed class NoWaitPublisher(ILogger<NoWaitPublisher> logger) : INotificationPublisher
{
    /// <summary>
    ///     Publishes a notification to all handlers without waiting.
    /// </summary>
    /// <param name="handlerExecutors">Handler executors</param>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handlerExecutors);

        foreach (var handler in handlerExecutors)
        {
            // Use CancellationToken.None: fire-and-forget handlers must not be cancelled
            // when the originating HTTP request completes — that defeats the purpose.
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler.ExecuteHandler(notification, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Fire-and-forget notification handler {HandlerType} failed for {NotificationType}",
                        handler.GetType().Name,
                        notification.GetType().Name);
                    throw;
                }
            }, CancellationToken.None);
        }

        return Task.CompletedTask;
    }
}
