namespace GameGuild.CQRS;

/// <summary>
///     Defines a handler for a request with a response
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     Handles a request
    /// </summary>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
///     Defines a handler for a request without a response (Command pattern)
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit> { }

/// <summary>
///     Defines a handler for a command with a response.
///     Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse> where TCommand : ICommand<TResponse> { }

/// <summary>
///     Defines a handler for a command without a response.
///     Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand> where TCommand : ICommand { }

/// <summary>
///     Defines a handler for a query.
///     Queries represent read-only operations that return data without modifying state.
/// </summary>
/// <typeparam name="TQuery">The type of query being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse> { }

/// <summary>
///     Defines a handler for a notification
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    ///     Handles a notification
    /// </summary>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

/// <summary>
///     Defines a handler for domain events
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event being handled</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent> where TDomainEvent : IDomainEvent { }

/// <summary>
///     Defines a handler for a streamable request
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStream<TResponse>
{
    /// <summary>
    ///     Handles a streaming request
    /// </summary>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
