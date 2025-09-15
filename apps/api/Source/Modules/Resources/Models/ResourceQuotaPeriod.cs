namespace GameGuild.Modules.Resources.Models;

/// <summary>
/// Period types for resource quota resets
/// </summary>
public enum ResourceQuotaPeriod
{
    /// <summary>
    /// Quota resets daily
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Quota resets weekly
    /// </summary>
    Weekly = 7,

    /// <summary>
    /// Quota resets monthly
    /// </summary>
    Monthly = 30,

    /// <summary>
    /// Quota resets quarterly
    /// </summary>
    Quarterly = 90,

    /// <summary>
    /// Quota resets yearly
    /// </summary>
    Yearly = 365,

    /// <summary>
    /// Quota never resets (lifetime limit)
    /// </summary>
    Never = 0
}
