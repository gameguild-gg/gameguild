namespace GameGuild.CQRS;

/// <summary> Async notification handler for all notifications. </summary>
/// <typeparam name="TNotification"> The notification type </typeparam>
public abstract class AsyncNotificationHandler<TNotification> : INotificationHandler<TNotification> where TNotification : INotification {
  /// <summary> Handles the notification </summary>
  /// <param name="notification"> Notification instance </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Task representing the async operation </returns>
  Task INotificationHandler<TNotification>.Handle(TNotification notification, CancellationToken cancellationToken) { return Handle(notification, cancellationToken); }

  /// <summary> Override in a derived class for the handler logic </summary>
  /// <param name="notification"> Notification instance </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Task representing the async operation </returns>
  protected abstract Task Handle(TNotification notification, CancellationToken cancellationToken);
}
