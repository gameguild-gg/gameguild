namespace GameGuild.CQRS;

/// <summary>
/// Typed notification handler executor
/// </summary>
/// <typeparam name="TNotification">Notification type</typeparam>
public class NotificationHandlerExecutorImpl<TNotification> : NotificationHandlerExecutor
    where TNotification : INotification
{
    private readonly INotificationHandler<TNotification> _handler;

    /// <summary>
    /// Initializes a new instance of the NotificationHandlerExecutorImpl class
    /// </summary>
    /// <param name="handler">Handler</param>
    public NotificationHandlerExecutorImpl(INotificationHandler<TNotification> handler) { _handler = handler; }

    /// <summary>
    /// Executes the handler
    /// </summary>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public override Task ExecuteHandler(INotification notification, CancellationToken cancellationToken) { return _handler.Handle((TNotification)notification, cancellationToken); }
}
