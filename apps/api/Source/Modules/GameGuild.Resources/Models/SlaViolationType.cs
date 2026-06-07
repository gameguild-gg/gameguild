namespace GameGuild.Resources;

/// <summary>
///     SLA violation types
/// </summary>
public enum SlaViolationType
{
    /// <summary>No violation</summary>
    None = 0,

    /// <summary>Resource quota exceeded</summary>
    QuotaExceeded = 1,

    /// <summary>Response time exceeded threshold</summary>
    ResponseTimeExceeded = 2,

    /// <summary>Availability dropped below threshold</summary>
    AvailabilityBreach = 3,

    /// <summary>Performance degradation detected</summary>
    PerformanceDegradation = 4,

    /// <summary>Throttling policy activated</summary>
    ThrottlingActivated = 5,

    /// <summary>Resource became unavailable</summary>
    ResourceUnavailable = 6,

    /// <summary>Other violation type</summary>
    Other = 99
}
