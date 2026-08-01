
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Data transfer object for SLO violation
/// </summary>
public class SloViolationDto
{
    /// <summary>
    ///     Violation identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     SLO identifier
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    ///     SLO name
    /// </summary>
    public string SloName { get; set; } = string.Empty;

    /// <summary>
    ///     Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     When violation started
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    ///     When violation ended (null if ongoing)
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    ///     Duration in minutes
    /// </summary>
    public double DurationMinutes { get; set; }

    /// <summary>
    ///     Actual value during violation
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    ///     Target value
    /// </summary>
    public double TargetValue { get; set; }

    /// <summary>
    ///     Severity level
    /// </summary>
    public ViolationSeverity Severity { get; set; }

    /// <summary>
    ///     Whether alert was triggered
    /// </summary>
    public bool AlertTriggered { get; set; }

    /// <summary>
    ///     When alert was sent
    /// </summary>
    public DateTimeOffset? AlertSentAt { get; set; }

    /// <summary>
    ///     Whether acknowledged
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    ///     Who acknowledged
    /// </summary>
    public Guid? AcknowledgedByUserId { get; set; }

    /// <summary>
    ///     When acknowledged
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }

    /// <summary>
    ///     Acknowledgment notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Whether this is an ongoing violation
    /// </summary>
    public bool IsOngoing { get => !EndedAt.HasValue; }
}
