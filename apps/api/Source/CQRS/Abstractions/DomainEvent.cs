namespace GameGuild.CQRS;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the DomainEvent class
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        Version = 1;
    }

    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Version of the event for compatibility
    /// </summary>
    public int Version { get; }
}
