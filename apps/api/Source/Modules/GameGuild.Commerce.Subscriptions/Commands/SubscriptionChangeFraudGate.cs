using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Shared fraud gate for subscription CHANGE commands (issue #409 remainder: score
///     subscription changes — upgrades, downgrades and reactivation-after-cancellation —
///     not just creations). Applies the same <c>IFraudDetectionService</c> evaluation the
///     creation path uses, through the change-kind entry point.
/// </summary>
/// <remarks>
///     <para>
///         Block refuses the change before anything is mutated
///         (<see cref="SubscriptionFraudBlockedException"/>, mapped to 422 by the shared
///         exception middleware). Review lets the change proceed but persists the bounded
///         evidence on the subscription's metadata when the slot is free, so reviewers see
///         it before the resulting charge clears the payment gate; an occupied metadata
///         slot is never clobbered — the warning log and the charge-time gate still surface
///         the case. Without a registered fraud detection service the gate is a no-op
///         (unit hosts, non-payments compositions); the payment gate still evaluates every
///         charge in that case.
///     </para>
///     <para>
///         The assessment context uses the subscription's authoritative amount as the
///         charge proxy and billing cycle 1 as the conservative tenure proxy — a change
///         right after signup must score like a new-customer charge, not a seasoned one.
///     </para>
/// </remarks>
public static class SubscriptionChangeFraudGate
{
    private const int MaxFraudMetadataLength = 2000;

    /// <summary>
    ///     Assesses a subscription change and enforces its outcome. Returns the assessment
    ///     (null when no service is registered) so callers can attach further evidence.
    /// </summary>
    /// <param name="fraudDetectionService">Optional fraud detection service.</param>
    /// <param name="logger">Caller logger for gate outcomes.</param>
    /// <param name="subscriptionRepository">Repository used to persist review evidence.</param>
    /// <param name="subscription">The subscription being changed.</param>
    /// <param name="changeKind">Kind of change being assessed.</param>
    /// <param name="amount">Charge-proxy amount of the change.</param>
    /// <param name="currency">Authoritative currency of the change.</param>
    /// <param name="country">Optional ISO 3166-1 alpha-2 billing country.</param>
    /// <param name="sourceIpAddress">Optional client IP of the change attempt.</param>
    /// <param name="deviceFingerprint">Optional device fingerprint of the change attempt.</param>
    /// <param name="payerEmail">Optional payer email of the change attempt.</param>
    /// <param name="cardBin">Optional 6-8 digit card BIN of the change attempt.</param>
    /// <param name="clientTimezone">Optional client-claimed IANA timezone.</param>
    /// <param name="reviewTicketCoordinator">Optional support-ticket coordinator for review cases (issue #409).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<GameGuild.Commerce.Payments.FraudAssessment?> AssessAsync(
        GameGuild.Commerce.Payments.IFraudDetectionService? fraudDetectionService,
        ILogger logger,
        ISubscriptionRepository subscriptionRepository,
        Subscription subscription,
        GameGuild.Commerce.Payments.SubscriptionFraudChangeKind changeKind,
        decimal amount,
        string currency,
        string? country,
        string? sourceIpAddress,
        string? deviceFingerprint,
        string? payerEmail,
        string? cardBin,
        string? clientTimezone,
        GameGuild.Commerce.IFraudReviewTicketCoordinator? reviewTicketCoordinator = null,
        CancellationToken cancellationToken = default)
    {
        if (fraudDetectionService is null)
        {
            return null;
        }

        var assessment = await fraudDetectionService
            .AssessSubscriptionChangeAsync(
                new GameGuild.Commerce.Payments.FraudSignalContext(
                    subscription.TenantId ?? Guid.Empty,
                    PaymentId: Guid.Empty,
                    SubscriptionId: subscription.Id,
                    ExternalCustomerId: null,
                    PaymentMethodId: null,
                    Amount: amount,
                    Currency: string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
                    BillingCycleNumber: 1,
                    Country: country,
                    SourceIpAddress: sourceIpAddress,
                    DeviceFingerprint: deviceFingerprint,
                    PayerEmail: payerEmail,
                    CardBin: cardBin,
                    ClientTimezone: clientTimezone),
                changeKind,
                cancellationToken)
            .ConfigureAwait(false);

        if (assessment.Decision == GameGuild.Commerce.Payments.FraudDecision.Block)
        {
            logger.LogWarning(
                "Subscription {SubscriptionId} {ChangeKind} refused by the fraud gate: score {Score}, reason {Reason}",
                subscription.Id,
                changeKind,
                assessment.Score,
                assessment.Reason);
            throw new SubscriptionFraudBlockedException(subscription.TenantId ?? Guid.Empty, subscription.PlanId, assessment.Score);
        }

        if (assessment.Decision == GameGuild.Commerce.Payments.FraudDecision.Review)
        {
            if (string.IsNullOrEmpty(subscription.Metadata))
            {
                var evidence = assessment.ToEvidenceJson();
                subscription.UpdateMetadata(evidence.Length <= MaxFraudMetadataLength
                    ? evidence
                    : evidence[..MaxFraudMetadataLength]);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                logger.LogWarning(
                    "Subscription {SubscriptionId} {ChangeKind} held for fraud review (score {Score}) but its metadata slot is occupied; evidence left to the charge-time gate: {Reason}",
                    subscription.Id,
                    changeKind,
                    assessment.Score,
                    assessment.Reason);
            }

            // Issue #409: review cases open an in-app support ticket through the commerce
            // coordinator, giving the case a durable surface in the support queue even when
            // the metadata slot is occupied. Best-effort — never fails the change.
            if (reviewTicketCoordinator is not null)
            {
                try
                {
                    await reviewTicketCoordinator
                        .OpenReviewTicketAsync(
                            new GameGuild.Commerce.FraudReviewTicketRequest(
                                subscription.TenantId ?? Guid.Empty,
                                subscription.Id,
                                nameof(Subscription),
                                $"subscription-{changeKind.ToString().ToLowerInvariant()}",
                                assessment.Score,
                                assessment.Reason,
                                amount,
                                string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
                                assessment.ToEvidenceJson()),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(
                        exception,
                        "Failed to open the fraud review ticket for subscription {SubscriptionId} {ChangeKind}",
                        subscription.Id,
                        changeKind);
                }
            }
        }

        return assessment;
    }

    /// <summary>Cycle amount of a plan for a subscription's billing cycle (the charge proxy).</summary>
    public static Money CycleAmountFor(Subscription subscription, SubscriptionPlan plan)
    {
        var monthly = plan.GetMonthlyPrice();
        return subscription.BillingCycle switch
        {
            BillingCycle.Annually => plan.GetAnnualPrice() ?? monthly * 12,
            BillingCycle.Quarterly => monthly * 3,
            BillingCycle.SemiAnnually => monthly * 6,
            BillingCycle.Biannually => monthly * 24,
            _ => monthly
        };
    }
}
