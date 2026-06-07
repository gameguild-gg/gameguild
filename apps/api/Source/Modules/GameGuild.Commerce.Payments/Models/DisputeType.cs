namespace GameGuild.Commerce.Payments;

/// <summary>Dispute types</summary>
public enum DisputeType
{
    /// <summary>Fraudulent transaction</summary>
    Fraudulent = 0,

    /// <summary>Product not received</summary>
    ProductNotReceived = 1,

    /// <summary>Product not as described</summary>
    ProductNotAsDescribed = 2,

    /// <summary>Duplicate charge</summary>
    Duplicate = 3,

    /// <summary>Incorrect amount</summary>
    IncorrectAmount = 4,

    /// <summary>Service not provided</summary>
    ServiceNotProvided = 5,

    /// <summary>Credit not processed</summary>
    CreditNotProcessed = 6,

    /// <summary>Other</summary>
    Other = 7
}
