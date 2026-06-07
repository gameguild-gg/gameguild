namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Subscription status enumeration
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    ///     Subscription is pending activation
    /// </summary>
    PendingActivation,

    /// <summary>
    ///     Subscription is active and current
    /// </summary>
    Active,

    /// <summary>
    ///     Subscription is in trial period
    /// </summary>
    Trialing,

    /// <summary>
    ///     Payment is past due
    /// </summary>
    PastDue,

    /// <summary>
    ///     Subscription is temporarily suspended
    /// </summary>
    Suspended,

    /// <summary>
    ///     Subscription has been cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    ///     Subscription has expired
    /// </summary>
    Expired
}
