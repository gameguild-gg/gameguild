using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Domain event raised when a quota limit is exceeded (for analytics and alerting)
/// </summary>
public record QuotaExceededEvent(
    Guid TenantId,
    ResourceUsageType ResourceType,
    long CurrentUsage,
    long RequestedAmount,
    long HardLimit,
    string? Source,
    Guid? ActorId,
    DateTime Timestamp) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = Timestamp;

    /// <inheritdoc />
    public int Version { get; init; } = 1;
}
