namespace GameGuild.CQRS;

/// <summary>
/// Marker interface for domain events that follow DDD principles.
/// Domain events represent something important that happened in the domain.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Version of the event for compatibility
    /// </summary>
    int Version { get; }
}
