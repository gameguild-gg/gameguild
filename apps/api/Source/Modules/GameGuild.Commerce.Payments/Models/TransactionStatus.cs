namespace GameGuild.Commerce.Payments;

/// <summary>Transaction status</summary>
public enum TransactionStatus
{
    /// <summary>Pending processing</summary>
    Pending = 0,

    /// <summary>Processing</summary>
    Processing = 1,

    /// <summary>Completed successfully</summary>
    Completed = 2,

    /// <summary>Failed</summary>
    Failed = 3,

    /// <summary>Cancelled</summary>
    Cancelled = 4,

    /// <summary>Reversed</summary>
    Reversed = 5
}
