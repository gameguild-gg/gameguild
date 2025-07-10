namespace GameGuild.Common;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventPublisher {
  /// <summary>
  /// Publishes a domain event asynchronously
  /// </summary>
  /// <typeparam name="T">The type of domain event</typeparam>
  /// <param name="domainEvent">The domain event to publish</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>A task representing the asynchronous operation</returns>
  Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent;
}
