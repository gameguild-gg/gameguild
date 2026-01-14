namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents an entry in a user's activity timeline.
/// </summary>
public class ActivityTimelineEntry
{
    /// <summary>
    ///     Unique identifier for the entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     When the activity occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Type of activity (Login, Logout, SessionCreated, SessionTerminated, DeviceTrusted, etc.)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    ///     Human-readable description of the activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     IP address associated with the activity.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Device fingerprint if available.
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    ///     Location information if available.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    ///     Whether this activity was flagged as suspicious.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    ///     Risk level associated with this activity.
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    /// <summary>
    ///     Related session ID if applicable.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    ///     Additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
