namespace GameGuild.Resources;

/// <summary>
///     Defines the period for resource quota resets
/// </summary>
public enum ResourceQuotaPeriod
{
    /// <summary>
    ///     Quota resets daily
    /// </summary>
    Daily = 1,

    /// <summary>
    ///     Quota resets weekly
    /// </summary>
    Weekly = 2,

    /// <summary>
    ///     Quota resets monthly
    /// </summary>
    Monthly = 3,

    /// <summary>
    ///     Quota resets quarterly
    /// </summary>
    Quarterly = 4,

    /// <summary>
    ///     Quota resets yearly
    /// </summary>
    Yearly = 5,

    /// <summary>
    ///     No automatic reset (unlimited period)
    /// </summary>
    Unlimited = 6
}
