using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Marker interface for domain events that follow DDD principles
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
    /// Version of the event schema for event evolution
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// The aggregate ID that generated this event
    /// </summary>
    Guid AggregateId { get; }
    
    /// <summary>
    /// Type of the aggregate that generated this event
    /// </summary>
    string AggregateType { get; }
}
