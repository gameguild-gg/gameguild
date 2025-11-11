namespace GameGuild.Authentication.Entities;

/// <summary>
///     Types of conditions that can trigger policy enforcement
/// </summary>
public enum PolicyConditionType
{
    /// <summary>
    ///     Time-based conditions (time of day, day of week)
    /// </summary>
    Time = 1,

    /// <summary>
    ///     Environment-based conditions (production, staging, dev)
    /// </summary>
    Environment = 2,

    /// <summary>
    ///     Location-based conditions (country, region, IP range)
    /// </summary>
    Location = 3,

    /// <summary>
    ///     Device-based conditions (mobile, desktop, compliance status)
    /// </summary>
    Device = 4,

    /// <summary>
    ///     Risk-based conditions (risk score, anomaly detection)
    /// </summary>
    Risk = 5,

    /// <summary>
    ///     Composite conditions (multiple condition types combined)
    /// </summary>
    Composite = 6,

    /// <summary>
    ///     Custom conditions defined by implementation
    /// </summary>
    Custom = 99
}
