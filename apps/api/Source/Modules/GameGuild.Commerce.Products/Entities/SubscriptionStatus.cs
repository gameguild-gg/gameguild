using System.ComponentModel;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Subscription status for user product lifecycle management
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
