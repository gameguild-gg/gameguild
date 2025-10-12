namespace GameGuild.Modules.SlaMonitoring.Entities;

/// <summary>
/// Represents a violation of a Service Level Objective.
/// </summary>
public class SloViolation : EntityBase
{
    /// <summary>
    /// Gets or sets the SLO that was violated.
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the SLO.
    /// </summary>
    public ServiceLevelObjective? ServiceLevelObjective { get; set; }

    /// <summary>
    /// Gets or sets when the violation started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the violation ended (null if still ongoing).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the actual value when the violation occurred.
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Gets or sets the target value that was not met.
    /// </summary>
    public double TargetValue { get; set; }

    /// <summary>
    /// Gets or sets the severity of the violation.
    /// </summary>
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Medium;

    /// <summary>
    /// Gets or sets whether an alert was triggered for this violation.
    /// </summary>
    public bool AlertTriggered { get; set; }

    /// <summary>
    /// Gets or sets when the alert was sent.
    /// </summary>
    public DateTime? AlertSentAt { get; set; }

    /// <summary>
    /// Gets or sets whether this violation has been acknowledged.
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// Gets or sets who acknowledged the violation.
    /// </summary>
    public Guid? AcknowledgedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the violation was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the violation.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Calculates the duration of the violation.
    /// </summary>
    public TimeSpan GetDuration()
    {
        var endTime = EndedAt ?? DateTime.UtcNow;
        return endTime - StartedAt;
    }

    /// <summary>
    /// Marks the violation as resolved.
    /// </summary>
    public void Resolve()
    {
        EndedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Acknowledges the violation.
    /// </summary>
    public void Acknowledge(Guid userId, string? notes = null)
    {
        IsAcknowledged = true;
        AcknowledgedByUserId = userId;
        AcknowledgedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes;
        }
    }
}

/// <summary>
/// Represents the severity of an SLO violation.
/// </summary>
public enum ViolationSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
