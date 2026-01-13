using System.ComponentModel;

namespace GameGuild.Commerce.Orders;

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
