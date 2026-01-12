namespace GameGuild.Identity.Authentication;

/// <summary>
///     Risk level for session security and anomaly detection
/// </summary>
public enum RiskLevel 
{ 
    /// <summary>
    ///     Low risk level
    /// </summary>
    Low = 0,

    /// <summary>
    ///     Medium risk level
    /// </summary>
    Medium = 1,

    /// <summary>
    ///     High risk level
    /// </summary>
    High = 2,

    /// <summary>
    ///     Critical risk level requiring immediate attention
    /// </summary>
    Critical = 3
}
