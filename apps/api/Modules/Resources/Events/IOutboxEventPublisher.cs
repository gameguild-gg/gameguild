namespace GameGuild.Modules.Resources.Events;

/// <summary>
/// Publisher for outbox pattern events
/// </summary>
public interface IOutboxEventPublisher
{
    /// <summary>
    /// Publishes an event to the outbox for reliable delivery
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
