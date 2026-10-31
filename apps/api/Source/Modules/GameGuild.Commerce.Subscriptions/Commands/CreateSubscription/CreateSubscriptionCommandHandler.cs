using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for creating a new subscription. When promo codes are supplied
///     (issue #418) they are quoted through the unified promo engine via
///     <see cref="GameGuild.Commerce.ISubscriptionPromoCoordinator"/>: the subscription is
///     created on the discounted amount, trial-extension codes extend (or grant) the trial,
///     free-months codes skip billing periods, and one usage row per applied code is
///     recorded and linked to the created subscription. The creation attempt also runs the
///     fraud signal evaluation (issue #409: subscription-creation-time risk scoring): a
///     Block-level assessment refuses the creation outright; a Review-level assessment
///     creates the subscription flagged with its bounded evidence so reviewers see it
///     before the first charge clears the payment gate.
/// </summary>
public sealed class CreateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    ILogger<CreateSubscriptionCommandHandler> logger,
    GameGuild.Commerce.ISubscriptionPromoCoordinator? promoCoordinator = null,
    GameGuild.Commerce.Payments.IFraudDetectionService? fraudDetectionService = null) : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    private const int MaxFraudMetadataLength = 2000;

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Issue #416: fail closed — an enterprise plan still carrying the -1 custom-quote
        // sentinel cannot be checked out until a concrete price is configured.
        var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameGuild.EntityNotFoundException("SubscriptionPlan", request.PlanId);
        if (plan.RequiresCustomQuote())
        {
            throw new SubscriptionPlanCustomQuotePendingException(request.PlanId);
        }

        // Issue #409: subscription-creation-time risk scoring. The same signal evaluation
        // as the payment gate, evaluated BEFORE anything is created: Block refuses the
        // creation; Review creates the subscription flagged with its bounded evidence.
        var creationAssessment = await AssessCreationAsync(request, plan.Currency, cancellationToken).ConfigureAwait(false);
        if (creationAssessment is { Decision: GameGuild.Commerce.Payments.FraudDecision.Block })
        {
            logger.LogWarning(
                "Subscription creation for tenant {TenantId} plan {PlanId} refused by the fraud gate: score {Score}, reason {Reason}",
                request.TenantId,
                request.PlanId,
                creationAssessment.Score,
                creationAssessment.Reason);
            throw new SubscriptionFraudBlockedException(request.TenantId, request.PlanId, creationAssessment.Score);
        }

        // Issue #418: quote promo codes against the plan before creating the subscription.
        GameGuild.Commerce.SubscriptionPromoQuoteResult? promoQuote = null;
        if (request.PromoCodes is { Count: > 0 })
        {
            if (promoCoordinator is null)
            {
                throw new InvalidOperationException(
                    "Promo codes were supplied but no promo coordinator is registered for subscription creation.");
            }

            promoQuote = await promoCoordinator.QuoteForSubscriptionCreationAsync(
                new GameGuild.Commerce.SubscriptionPromoQuoteRequest(
                    request.PlanId,
                    request.Amount,
                    request.PromoCodes,
                    request.CreatedByUserId,
                    request.ProductId,
                    request.FulfilledOrderId),
                cancellationToken).ConfigureAwait(false);

            if (promoQuote.AppliedCodes.Count == 0)
            {
                // Fail closed: checkout must not silently drop a supplied promo. The client
                // previews codes through promo-codes:validate first; a total rejection here
                // means the code expired or was fully consumed between preview and create.
                var reasons = string.Join("; ", promoQuote.RejectedCodes.Select(r => $"{r.Code}: {r.Reason}"));
                throw new InvalidOperationException(
                    $"None of the supplied promo codes could be applied to plan {request.PlanId}. {reasons}");
            }
        }

        var effectiveAmount = promoQuote?.FinalAmount ?? request.Amount;

        var startDate = request.StartDate ?? SystemClock.UtcNow;
        var trialEndDate = request.TrialDays.HasValue ? startDate.AddDays(request.TrialDays.Value) : (DateTime?) null;

        var subscription = new Subscription(
            request.TenantId,
            request.PlanId,
            request.CreatedByUserId,
            request.BillingCycle,
            new Money(effectiveAmount),
            startDate,
            trialEndDate);

        // Issue #418: consume trial-extension and free-months grants from the applied codes.
        var promoTrialDays = promoQuote?.AppliedCodes.Sum(c => c.TrialExtensionDays ?? 0) ?? 0;
        if (promoTrialDays > 0)
        {
            subscription.ExtendTrial(promoTrialDays);
        }

        var promoFreeMonths = promoQuote?.AppliedCodes.Sum(c => c.FreeMonthsGranted ?? 0) ?? 0;
        if (promoFreeMonths > 0)
        {
            subscription.SkipBillingPeriods(promoFreeMonths);
        }

        // Associate with fulfilled order for economic audit trail (if provided)
        if (request.FulfilledOrderId.HasValue)
        {
            subscription.SetFulfilledOrderId(request.FulfilledOrderId.Value);
            logger.LogInformation(
                "Subscription {SubscriptionId} linked to fulfilled order {OrderId}",
                subscription.Id, request.FulfilledOrderId.Value);
        }
        else
        {
            // Log warning for subscriptions created without order linkage (legacy/migration scenarios)
            logger.LogWarning(
                "Subscription {SubscriptionId} created without FulfilledOrderId linkage. " +
                "This should only occur for legacy data migration or admin corrections.",
                subscription.Id);
        }

        await subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        // Issue #409: persist the creation-time fraud outcome on the subscription when the
        // assessment held it for review, so reviewers see the evidence before the first
        // charge clears the payment gate. Bounded to the metadata column's limit; the
        // evidence bundle itself never carries provider tokens.
        if (creationAssessment is { Decision: GameGuild.Commerce.Payments.FraudDecision.Review })
        {
            var evidence = creationAssessment.ToEvidenceJson();
            subscription.UpdateMetadata(evidence.Length <= MaxFraudMetadataLength
                ? evidence
                : evidence[..MaxFraudMetadataLength]);
            await subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

            logger.LogWarning(
                "Subscription {SubscriptionId} for tenant {TenantId} created flagged for fraud review: score {Score}, reason {Reason}",
                subscription.Id,
                request.TenantId,
                creationAssessment.Score,
                creationAssessment.Reason);
        }

        // Issue #418: persist one usage row per applied code, linked to the created
        // subscription (idempotent, fail-closed on limits inside the recorder).
        if (promoQuote is { AppliedCodes.Count: > 0 } && promoCoordinator is not null)
        {
            await promoCoordinator.RecordSubscriptionUsageAsync(
                new GameGuild.Commerce.SubscriptionPromoUsageRecordRequest(
                    subscription.Id,
                    request.CreatedByUserId,
                    promoQuote.AppliedCodes,
                    promoQuote.Currency,
                    request.FulfilledOrderId,
                    request.PromoCodeIdempotencyKey),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Subscription {SubscriptionId} created with {PromoCount} applied promo code(s); amount {OriginalAmount} → {FinalAmount} {Currency}",
                subscription.Id,
                promoQuote.AppliedCodes.Count,
                promoQuote.OriginalAmount,
                effectiveAmount,
                promoQuote.Currency);
        }

        return subscription.Id;
    }

    /// <summary>
    ///     Runs the creation-time fraud assessment (issue #409). Without a registered fraud
    ///     detection service the gate is a no-op (unit hosts, non-payments compositions);
    ///     the payment gate still evaluates every charge in that case. No payment row
    ///     exists yet, so the assessment context carries an empty payment id and the first
    ///     billing cycle as the customer-tenure proxy.
    /// </summary>
    private async Task<GameGuild.Commerce.Payments.FraudAssessment?> AssessCreationAsync(
        CreateSubscriptionCommand request,
        string planCurrency,
        CancellationToken cancellationToken)
    {
        if (fraudDetectionService is null)
        {
            return null;
        }

        return await fraudDetectionService
            .AssessSubscriptionCreationAsync(
                new GameGuild.Commerce.Payments.FraudSignalContext(
                    request.TenantId,
                    PaymentId: Guid.Empty,
                    SubscriptionId: Guid.Empty,
                    ExternalCustomerId: null,
                    PaymentMethodId: null,
                    Amount: request.Amount,
                    Currency: string.IsNullOrWhiteSpace(planCurrency) ? "USD" : planCurrency,
                    BillingCycleNumber: 1,
                    Country: request.FraudCountry,
                    SourceIpAddress: request.FraudSourceIpAddress,
                    DeviceFingerprint: request.FraudDeviceFingerprint,
                    PayerEmail: request.FraudPayerEmail,
                    CardBin: request.FraudCardBin,
                    ClientTimezone: request.FraudClientTimezone),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
