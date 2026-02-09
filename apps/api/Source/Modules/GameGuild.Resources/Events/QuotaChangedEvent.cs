using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Domain event raised when a quota is modified (created, updated, or deleted)
/// </summary>
public sealed record QuotaChangedEvent(
    Guid TenantId,
    ResourceUsageType ResourceType,
    QuotaChangeType ChangeType,
    long? PreviousUsage,
    long CurrentUsage,
    long? SoftLimit,
    long? HardLimit,
    string? Source,
    Guid? ActorId,
    DateTimeOffset Timestamp) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = Timestamp;

    /// <inheritdoc />
    public int Version { get; init; } = 1;
}

/// <summary>
///     Type of quota change
/// </summary>
public enum QuotaChangeType
{
    /// <summary>Quota was created</summary>
    Created,

    /// <summary>Usage was incremented</summary>
    UsageIncremented,

    /// <summary>Usage was decremented</summary>
    UsageDecremented,

    /// <summary>Quota limits were updated</summary>
    LimitsUpdated,

    /// <summary>Quota was reset</summary>
    Reset,

    /// <summary>Quota was deleted</summary>
    Deleted
}
