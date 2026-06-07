namespace GameGuild.Commerce.Payments;

/// <summary>Revenue event types</summary>
public enum RevenueEventType
{
    /// <summary>Payment received</summary>
    PaymentReceived = 0,

    /// <summary>Subscription started</summary>
    SubscriptionStarted = 1,

    /// <summary>Subscription renewed</summary>
    SubscriptionRenewed = 2,

    /// <summary>Subscription cancelled</summary>
    SubscriptionCancelled = 3,

    /// <summary>Refund processed</summary>
    RefundProcessed = 4,

    /// <summary>Chargeback</summary>
    Chargeback = 5,

    /// <summary>Fee charged</summary>
    FeeCharged = 6,

    /// <summary>Credit issued</summary>
    CreditIssued = 7,

    /// <summary>Adjustment</summary>
    Adjustment = 8,

    /// <summary>Other</summary>
    Other = 9
}
