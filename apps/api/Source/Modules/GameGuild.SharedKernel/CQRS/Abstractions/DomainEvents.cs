namespace GameGuild.CQRS;

/// <summary>
///     Marker interface for domain events that follow DDD principles.
///     Domain events represent something important that happened in the domain.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    ///     Unique identifier for the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    ///     When the event occurred
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    ///     Version of the event for compatibility
    /// </summary>
    int Version { get; }
}

/// <summary>
///     Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    ///     Initializes a new instance of the DomainEvent class
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = SystemClock.UtcNow;
        Version = 1;
    }

    /// <summary>
    ///     Unique identifier for the event
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    ///     When the event occurred
    /// </summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    ///     Version of the event for compatibility. Override via init in derived event records.
    /// </summary>
    public int Version { get; init; }
}

/// <summary>
///     Base interface for entities with domain event support.
///     This provides a cleaner separation from the main IEntity interface.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    ///     Gets the collection of domain events raised by this entity.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    ///     Adds a domain event to the entity's event collection.
    /// </summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    ///     Removes a specific domain event from the entity's event collection.
    /// </summary>
    void RemoveDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    ///     Clears all domain events from the entity's event collection.
    /// </summary>
    void ClearDomainEvents();
}
