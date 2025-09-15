namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
/// Reasons for subscription cancellation
/// </summary>
public enum CancellationReason
{
    /// <summary>
    /// User requested cancellation
    /// </summary>
    UserRequested = 1,

    /// <summary>
    /// Payment failure
    /// </summary>
    PaymentFailure = 2,

    /// <summary>
    /// Trial period ended without conversion
    /// </summary>
    TrialEnded = 3,

    /// <summary>
    /// Administrative cancellation
    /// </summary>
    Administrative = 4,

    /// <summary>
    /// Account suspension
    /// </summary>
    AccountSuspension = 5,

    /// <summary>
    /// Upgrade to different plan
    /// </summary>
    PlanUpgrade = 6,

    /// <summary>
    /// Downgrade to different plan
    /// </summary>
    PlanDowngrade = 7,

    /// <summary>
    /// Terms of service violation
    /// </summary>
    TermsViolation = 8,

    /// <summary>
    /// Fraud detection
    /// </summary>
    Fraud = 9,

    /// <summary>
    /// Other reason
    /// </summary>
    Other = 99
}