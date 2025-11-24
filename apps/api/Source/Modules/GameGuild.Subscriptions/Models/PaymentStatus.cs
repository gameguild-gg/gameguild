namespace GameGuild.Subscriptions.Models;

/// <summary>
///     Payment status enumeration
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    ///     Payment is pending processing
    /// </summary>
    Pending,

    /// <summary>
    ///     Payment was successful
    /// </summary>
    Succeeded,

    /// <summary>
    ///     Payment failed
    /// </summary>
    Failed,

    /// <summary>
    ///     Payment was cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    ///     Payment requires additional action (3DS, etc.)
    /// </summary>
    RequiresAction,

    /// <summary>
    ///     Payment is being processed
    /// </summary>
    Processing,

    /// <summary>
    ///     Payment was refunded
    /// </summary>
    Refunded,

    /// <summary>
    ///     Payment was partially refunded
    /// </summary>
    PartiallyRefunded
}
