namespace GameGuild.Commerce.Payments;

/// <summary>Revenue event status</summary>
public enum RevenueEventStatus
{
    /// <summary>Pending processing</summary>
    Pending = 0,

    /// <summary>Processed</summary>
    Processed = 1,

    /// <summary>Failed</summary>
    Failed = 2,

    /// <summary>Cancelled</summary>
    Cancelled = 3
}
