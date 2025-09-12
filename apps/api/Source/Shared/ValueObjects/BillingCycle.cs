namespace GameGuild;

/// <summary>
///     Billing cycle enumeration
/// </summary>
public enum BillingCycle
{
    /// <summary>
    ///     Monthly billing (every month)
    /// </summary>
    Monthly = 1,

    /// <summary>
    ///     Quarterly billing (every 3 months)
    /// </summary>
    Quarterly = 3,

    /// <summary>
    ///     Semi-annual billing (every 6 months)
    /// </summary>
    SemiAnnually = 6,

    /// <summary>
    ///     Annual billing (every 12 months)
    /// </summary>
    Annually = 12,

    /// <summary>
    ///     Biannual billing (every 24 months)
    /// </summary>
    Biannually = 24
}
