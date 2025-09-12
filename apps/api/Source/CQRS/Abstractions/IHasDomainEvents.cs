namespace GameGuild.CQRS;

/// <summary>
/// Base interface for entities with domain event support.
/// This provides a cleaner separation from the main IEntity interface.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to the entity's event collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Removes a specific domain event from the entity's event collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove</param>
    void RemoveDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Clears all domain events from the entity's event collection.
    /// </summary>
    void ClearDomainEvents();
}
