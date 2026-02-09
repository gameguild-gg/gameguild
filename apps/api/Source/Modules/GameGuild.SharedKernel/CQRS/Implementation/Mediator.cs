using GameGuild.CQRS.Publishers;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Public mediator facade that delegates to focused <see cref="MediatorSender" /> and <see cref="MediatorPublisher" /> implementations.
///     Implements <see cref="IMediator" /> (which extends <see cref="ISender" /> and <see cref="IPublisher" />).
/// </summary>
public class Mediator : IMediator
{
    private readonly MediatorSender _sender;
    private readonly MediatorPublisher _publisher;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Mediator" /> class.
    /// </summary>
    /// <param name="serviceFactory">Service factory</param>
    /// <param name="notificationPublisher">Notification publisher</param>
    public Mediator(ServiceFactory serviceFactory, INotificationPublisher? notificationPublisher = null)
    {
        var publisher = notificationPublisher ?? new ForeachAwaitPublisher();
        _sender = new MediatorSender(serviceFactory);
        _publisher = new MediatorPublisher(serviceFactory, publisher);
    }

    // ── ISender ────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => _sender.Send(request, cancellationToken);

    /// <inheritdoc />
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        => _sender.Send(request, cancellationToken);

    /// <inheritdoc />
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        => _sender.Send(request, cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStream<TResponse> request, CancellationToken cancellationToken = default)
        => _sender.CreateStream(request, cancellationToken);

    // ── IPublisher ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        => _publisher.Publish(notification, cancellationToken);

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => _publisher.Publish(notification, cancellationToken);
}
