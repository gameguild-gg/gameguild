using System.ComponentModel;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Order status enumeration with explicit economic states.
/// Follows monotonic FSM - no backward economic transitions allowed.
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
    Disputed = 7,

    /// <summary>Payment succeeded, awaiting entitlement fulfillment</summary>
    [Description("Order paid, pending fulfillment")]
    Paid = 8,

    /// <summary>All entitlements granted, order complete</summary>
    [Description("Order fulfilled - all entitlements granted")]
    Fulfilled = 9
}

/// <summary>
/// Order type classification - determines fulfillment logic and validation rules.
/// </summary>
public enum OrderType
{
    /// <summary>One-time product purchase (no recurring billing)</summary>
    [Description("One-time purchase")]
    OneTimePurchase = 0,

    /// <summary>New subscription creation</summary>
    [Description("New subscription")]
    Subscribe = 1,

    /// <summary>Subscription upgrade to higher tier</summary>
    [Description("Subscription upgrade")]
    Upgrade = 2,

    /// <summary>Subscription downgrade to lower tier</summary>
    [Description("Subscription downgrade")]
    Downgrade = 3,

    /// <summary>Add-on purchase for existing subscription</summary>
    [Description("Add-on purchase")]
    AddOn = 4,

    /// <summary>Subscription renewal (automated or manual)</summary>
    [Description("Subscription renewal")]
    Renewal = 5
}
