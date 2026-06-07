namespace GameGuild.Commerce.Payments;

/// <summary>Dispute status</summary>
public enum DisputeStatus
{
    /// <summary>Submitted</summary>
    Submitted = 0,

    /// <summary>Under review</summary>
    UnderReview = 1,

    /// <summary>Pending customer response</summary>
    PendingCustomerResponse = 2,

    /// <summary>Pending merchant response</summary>
    PendingMerchantResponse = 3,

    /// <summary>Resolved</summary>
    Resolved = 4,

    /// <summary>Won by customer</summary>
    Won = 5,

    /// <summary>Lost by customer</summary>
    Lost = 6,

    /// <summary>Cancelled</summary>
    Cancelled = 7
}
