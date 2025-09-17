namespace GameGuild.CQRS;

/// <summary> Base notification handler executor </summary>
public abstract class NotificationHandlerExecutorBase {
  /// <summary> Executes the handler </summary>
  /// <param name="notification"> Notification </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  public abstract Task ExecuteHandler(INotification notification, CancellationToken cancellationToken);
}
