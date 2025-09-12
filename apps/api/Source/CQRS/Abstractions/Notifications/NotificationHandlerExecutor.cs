namespace GameGuild.CQRS;

/// <summary>
/// Typed notification handler executor
/// </summary>
/// <typeparam name="TNotification">Notification type</typeparam>
public class NotificationHandlerExecutor<TNotification> : NotificationHandlerExecutorBase where TNotification : INotification {
  private readonly INotificationHandler<TNotification> _handler;

  /// <summary>
  /// Initializes a new instance of the NotificationHandlerExecutor class
  /// </summary>
  /// <param name="handler">Handler</param>
  public NotificationHandlerExecutor(INotificationHandler<TNotification> handler) { _handler = handler; }

  /// <summary>
  /// Executes the handler
  /// </summary>
  /// <param name="notification">Notification</param>
  /// <param name="cancellationToken">Cancellation token</param>
  public override Task ExecuteHandler(INotification notification, CancellationToken cancellationToken) { return _handler.Handle((TNotification) notification, cancellationToken); }
}
