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
}
