using System.ComponentModel;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Entitlement subscription status for user product lifecycle management.
/// NOTE: This is separate from GameGuild.Commerce.Subscriptions.SubscriptionStatus which
/// tracks the Subscription entity state. This enum tracks the user's entitlement status
/// for subscription-based products.
/// </summary>
public enum EntitlementSubscriptionStatus
{
    /// <summary>Subscription entitlement is active and paid</summary>
    [Description("Active subscription")]
    Active = 0,

    /// <summary>Subscription entitlement is in trial period</summary>
    [Description("Trial period")]
    Trial = 1,

    /// <summary>Payment past due, in grace period</summary>
    [Description("Past due - grace period")]
    PastDue = 2,

    /// <summary>Subscription cancelled, still active until period end</summary>
    [Description("Cancelled - active until period end")]
    CancelledPending = 3,

    /// <summary>Subscription entitlement fully cancelled</summary>
    [Description("Cancelled")]
    Cancelled = 4,

    /// <summary>Subscription entitlement expired</summary>
    [Description("Expired")]
    Expired = 5,

    /// <summary>Subscription entitlement paused</summary>
    [Description("Paused")]
    Paused = 6,

    /// <summary>Subscription entitlement suspended for non-payment</summary>
    [Description("Suspended")]
    Suspended = 7
}
