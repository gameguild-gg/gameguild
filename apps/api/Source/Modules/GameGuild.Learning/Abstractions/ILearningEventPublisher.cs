using GameGuild.CQRS;

namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Central event publisher for all learning-related domain events.
/// Provides a unified way to emit telemetry and cross-module events.
/// </summary>
public interface ILearningEventPublisher
{
    /// <summary>
    /// Publishes a learning domain event
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish</typeparam>
    /// <param name="domainEvent">The event instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent;

    /// <summary>
    /// Publishes multiple learning domain events
    /// </summary>
    /// <param name="domainEvents">The events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler interface for learning domain events
/// </summary>
/// <typeparam name="TEvent">The type of event to handle</typeparam>
public interface ILearningEventHandler<in TEvent> where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the learning domain event
    /// </summary>
    /// <param name="domainEvent">The event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
