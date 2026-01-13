using System.ComponentModel;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created but not yet paid</summary>
    [Description("Order pending payment")]
    Pending = 0,

    /// <summary>Payment processing</summary>
    [Description("Payment is being processed")]
    Processing = 1,

    /// <summary>Payment completed, order fulfilled</summary>
    [Description("Order completed successfully")]
    Completed = 2,

    /// <summary>Payment failed</summary>
    [Description("Payment failed")]
    Failed = 3,

    /// <summary>Order cancelled before payment</summary>
    [Description("Order cancelled")]
    Cancelled = 4,

    /// <summary>Full refund issued</summary>
    [Description("Order fully refunded")]
    Refunded = 5,

    /// <summary>Partial refund issued</summary>
    [Description("Order partially refunded")]
    PartiallyRefunded = 6,

    /// <summary>Disputed by customer</summary>
    [Description("Order disputed")]
    Disputed = 7
}

/// <summary>
/// Subscription status for lifecycle management
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Subscription is active and paid</summary>
    [Description("Active subscription")]
    Active = 0,

    /// <summary>Subscription is in trial period</summary>
    [Description("Trial period")]
    Trial = 1,

    /// <summary>Payment past due, in grace period</summary>
    [Description("Past due - grace period")]
    PastDue = 2,

    /// <summary>Subscription cancelled, still active until period end</summary>
    [Description("Cancelled - active until period end")]
    CancelledPending = 3,

    /// <summary>Subscription fully cancelled</summary>
    [Description("Cancelled")]
    Cancelled = 4,

    /// <summary>Subscription expired</summary>
    [Description("Expired")]
    Expired = 5,

    /// <summary>Subscription paused</summary>
    [Description("Paused")]
    Paused = 6,

    /// <summary>Subscription suspended for non-payment</summary>
    [Description("Suspended")]
    Suspended = 7
}
