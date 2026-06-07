namespace GameGuild.Commerce.Payments;

/// <summary>Revenue sources</summary>
public enum RevenueSource
{
    /// <summary>Subscription</summary>
    Subscription = 0,

    /// <summary>One-time payment</summary>
    OneTimePayment = 1,

    /// <summary>Add-on</summary>
    AddOn = 2,

    /// <summary>Service fee</summary>
    ServiceFee = 3,

    /// <summary>Transaction fee</summary>
    TransactionFee = 4,

    /// <summary>Setup fee</summary>
    SetupFee = 5,

    /// <summary>Usage fee</summary>
    UsageFee = 6,

    /// <summary>Other</summary>
    Other = 7
}
