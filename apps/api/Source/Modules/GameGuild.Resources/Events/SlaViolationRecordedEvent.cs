using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Domain event raised when an SLA violation is recorded.
///     Consumers can use this for notifications, incident creation, and escalation.
/// </summary>
public record SlaViolationRecordedEvent(
    Guid ViolationId,
    Guid TenantId,
    Guid ResourceQuotaId,
    SlaViolationType ViolationType,
    SlaViolationSeverity Severity,
    long ExpectedValue,
    long ActualValue,
    decimal DeviationPercentage,
    bool RequiresEscalation,
    Guid? UserId,
    DateTime Timestamp) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = Timestamp;

    /// <inheritdoc />
    public int Version { get; init; } = 1;
}

/// <summary>
///     Domain event raised when an SLA violation is resolved.
/// </summary>
public record SlaViolationResolvedEvent(
    Guid ViolationId,
    Guid TenantId,
    Guid ResolvedByUserId,
    TimeSpan ResolutionDuration,
    string? MitigationActions,
    DateTime Timestamp) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = Timestamp;

    /// <inheritdoc />
    public int Version { get; init; } = 1;
}
