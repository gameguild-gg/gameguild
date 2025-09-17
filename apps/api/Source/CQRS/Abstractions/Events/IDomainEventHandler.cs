namespace GameGuild.CQRS;

/// <summary> Defines a handler for domain events </summary>
/// <typeparam name="TDomainEvent"> The type of domain event being handled </typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent> where TDomainEvent : IDomainEvent { }
