namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Represents the severity level of an SLO violation
/// </summary>
public enum ViolationSeverity
{
    /// <summary>
    ///     Minor breach with minimal impact
    /// </summary>
    Low = 0,

    /// <summary>
    ///     Moderate breach requiring attention
    /// </summary>
    Medium = 1,

    /// <summary>
    ///     Significant breach requiring immediate action
    /// </summary>
    High = 2,

    /// <summary>
    ///     Critical breach requiring urgent escalation
    /// </summary>
    Critical = 3
}
