namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Represents the status of a Service Level Objective
/// </summary>
public enum SloStatus
{
    /// <summary>
    ///     SLO is operating within acceptable parameters
    /// </summary>
    Active = 0,

    /// <summary>
    ///     SLO has breached the target threshold
    /// </summary>
    Breached = 1,

    /// <summary>
    ///     SLO is approaching the error budget threshold
    /// </summary>
    AtRisk = 2,

    /// <summary>
    ///     SLO is manually disabled
    /// </summary>
    Disabled = 3,

    /// <summary>
    ///     SLO has violated the agreement (deprecated, use Breached)
    /// </summary>
    Violated = 4,

    /// <summary>
    ///     SLO is in warning state (deprecated, use AtRisk)
    /// </summary>
    Warning = 5,

    /// <summary>
    ///     SLO is not currently active
    /// </summary>
    Inactive = 6
}
