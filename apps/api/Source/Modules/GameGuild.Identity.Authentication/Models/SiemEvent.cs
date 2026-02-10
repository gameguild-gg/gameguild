namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents a security event to be sent to a SIEM system.
/// </summary>
public class SiemEvent
{
    /// <summary>
    ///     Unique identifier for the event.
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = SystemClock.UtcNow;

    /// <summary>
    ///     Type of security event.
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    ///     Severity level of the event.
    /// </summary>
    public SiemSeverity Severity { get; set; }

    /// <summary>
    ///     Source system or component that generated the event.
    /// </summary>
    public string Source { get; set; } = "GameGuild.Authentication";

    /// <summary>
    ///     User ID associated with the event, if applicable.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     IP address associated with the event.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Description of the event.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    ///     Additional metadata about the event.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    ///     Risk score associated with the event (0-100).
    /// </summary>
    public int? RiskScore { get; set; }

    /// <summary>
    ///     Tenant ID for multi-tenant scenarios.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Correlation ID for tracking related events.
    /// </summary>
    public Guid? CorrelationId { get; set; }
}

/// <summary>
///     SIEM event severity levels.
/// </summary>
public enum SiemSeverity
{
    /// <summary>
    ///     Informational event.
    /// </summary>
    Info = 0,

    /// <summary>
    ///     Low severity event.
    /// </summary>
    Low = 1,

    /// <summary>
    ///     Medium severity event.
    /// </summary>
    Medium = 2,

    /// <summary>
    ///     High severity event requiring attention.
    /// </summary>
    High = 3,

    /// <summary>
    ///     Critical severity event requiring immediate action.
    /// </summary>
    Critical = 4
}
