namespace GameGuild.Commerce.Payments;

/// <summary>
///     Evaluates the aggregate fraud assessment for a payment attempt by aggregating all
///     registered <see cref="IFraudSignalProvider"/> contributions against the
///     tenant-resolved <see cref="FraudAssessmentOptions"/> thresholds (issue #409).
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    ///     Assesses a payment attempt and returns the Allow / Review / Block decision with
    ///     its score, risk level, reason and bounded signal evidence.
    /// </summary>
    /// <param name="context">The payment attempt being assessed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FraudAssessment> AssessPaymentAsync(FraudSignalContext context, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Applies the same fraud signal evaluation at subscription creation time (issue
    ///     #409: creation-time risk scoring, not just payment). The context describes the
    ///     creation attempt — <see cref="FraudSignalContext.PaymentId"/> is empty because no
    ///     payment row exists yet, and the billing cycle is the first.
    /// </summary>
    /// <param name="context">The subscription creation attempt being assessed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FraudAssessment> AssessSubscriptionCreationAsync(FraudSignalContext context, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Applies the same fraud signal evaluation to a subscription CHANGE (issue #409:
    ///     score subscription creations and changes). Covers plan upgrades/downgrades and
    ///     reactivation-after-cancellation (the resubscribe path) — the attempts where the
    ///     charged amount or entitlement changes without a fresh checkout. The context
    ///     describes the change attempt; <see cref="FraudSignalContext.PaymentId"/> is empty
    ///     because the change is assessed before any charge is raised.
    /// </summary>
    /// <param name="context">The subscription change attempt being assessed.</param>
    /// <param name="changeKind">Which kind of change is being assessed (upgrade/downgrade/reactivation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FraudAssessment> AssessSubscriptionChangeAsync(
        FraudSignalContext context,
        SubscriptionFraudChangeKind changeKind,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Re-runs the payment fraud evaluation WITHOUT the synchronous latency budget
    ///     (issue #409: asynchronous scoring for background checks). Used by the background
    ///     re-scoring worker to complete the providers that were deferred when the
    ///     synchronous pass hit its budget. The caller is responsible for reconciling the
    ///     outcome with the already-persisted decision.
    /// </summary>
    /// <param name="context">The payment attempt being re-assessed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FraudAssessment> AssessPaymentForBackgroundAsync(FraudSignalContext context, CancellationToken cancellationToken = default);
}
