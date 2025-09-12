namespace GameGuild.CQRS;

public interface IDomainEventsDispatcher {
  Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
