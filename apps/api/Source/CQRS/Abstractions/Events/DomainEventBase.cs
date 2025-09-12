namespace GameGuild.CQRS;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEventBase : IDomainEvent {
  /// <summary>
  /// Initializes a new instance of the DomainEventBase class
  /// </summary>
  protected DomainEventBase(Guid aggregateId, string aggregateType) {
    EventId = Guid.NewGuid();
    OccurredAt = DateTime.UtcNow;
    Version = 1;
    AggregateId = aggregateId;
    AggregateType = aggregateType;
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

  /// <inheritdoc />
  public Guid AggregateId { get; }

  /// <inheritdoc />
  public string AggregateType { get; }
}
