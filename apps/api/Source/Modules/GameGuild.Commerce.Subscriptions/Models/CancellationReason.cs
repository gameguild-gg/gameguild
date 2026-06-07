namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Cancellation reason enumeration
/// </summary>
public enum CancellationReason
{
    /// <summary>
    ///     User requested cancellation
    /// </summary>
    UserRequested,

    /// <summary>
    ///     Payment failed repeatedly
    /// </summary>
    PaymentFailed,

    /// <summary>
    ///     Plan was discontinued
    /// </summary>
    PlanDiscontinued,

    /// <summary>
    ///     Policy violation
    /// </summary>
    PolicyViolation,

    /// <summary>
    ///     Downgrade to free plan
    /// </summary>
    Downgrade,

    /// <summary>
    ///     Trial period ended without conversion
    /// </summary>
    TrialEnded,

    /// <summary>
    ///     Custom reason
    /// </summary>
    Custom,

    /// <summary>
    ///     Cancellation requested by external system (e.g., via webhook from payment provider)
    /// </summary>
    ExternalRequest
}
