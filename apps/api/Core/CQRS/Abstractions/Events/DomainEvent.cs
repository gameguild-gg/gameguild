namespace GameGuild.CQRS;

/// <summary> Base class for domain events - alias for DomainEventBase </summary>
public abstract class DomainEvent : DomainEventBase
{
    /// <summary> Initializes a new instance of the DomainEvent class </summary>
    protected DomainEvent(Guid aggregateId, string aggregateType) : base(aggregateId, aggregateType) { }
}
