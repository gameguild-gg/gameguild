namespace GameGuild.CQRS;

/// <summary> Publisher that executes handlers sequentially using foreach and await with validation </summary>
public class ForeachAwaitPublisher : INotificationPublisher {
  /// <summary> Publishes a notification to all handlers sequentially </summary>
  /// <param name="handlerExecutors"> Handler executors </param>
  /// <param name="notification"> Notification </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  public async Task Publish(IEnumerable<NotificationHandlerExecutorBase> handlerExecutors, INotification notification, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(handlerExecutors);

    foreach (var handler in handlerExecutors) { await handler.ExecuteHandler(notification, cancellationToken).ConfigureAwait(false); }
  }
}
