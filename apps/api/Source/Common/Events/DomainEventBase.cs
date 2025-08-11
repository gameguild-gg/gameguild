namespace GameGuild.Common;

/// <summary>
/// Base class for domain events with common implementation
/// </summary>
public abstract class DomainEventBase : IDomainEvent {
  protected DomainEventBase(Guid aggregateId, string aggregateType) {
    EventId = Guid.NewGuid();
    OccurredAt = DateTime.UtcNow;
    Version = 1;
    AggregateId = aggregateId;
    AggregateType = aggregateType;
  }

  /// <inheritdoc />
  public Guid EventId { get; }

  /// <inheritdoc />
  public DateTime OccurredAt { get; }

  /// <inheritdoc />
  public int Version { get; }

  /// <inheritdoc />
  public Guid AggregateId { get; }

  /// <inheritdoc />
  public string AggregateType { get; }
}
