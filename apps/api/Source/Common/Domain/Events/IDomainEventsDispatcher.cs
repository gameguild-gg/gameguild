using GameGuild.Common.Abstractions;


namespace GameGuild.Common.Domain.Events;

public interface IDomainEventsDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
