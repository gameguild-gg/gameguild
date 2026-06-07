namespace GameGuild.Commerce.Payments;

/// <summary>Dispute resolution types</summary>
public enum DisputeResolution
{
    /// <summary>Won by customer</summary>
    Won = 0,

    /// <summary>Lost by customer</summary>
    Lost = 1,

    /// <summary>Partial refund</summary>
    PartialRefund = 2,

    /// <summary>Merchant credit</summary>
    MerchantCredit = 3,

    /// <summary>Replacement</summary>
    Replacement = 4,

    /// <summary>Mutual agreement</summary>
    MutualAgreement = 5
}
