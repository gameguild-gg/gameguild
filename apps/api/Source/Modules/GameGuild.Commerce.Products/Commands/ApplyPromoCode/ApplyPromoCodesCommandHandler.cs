using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for applying promo codes to an order. Computes the rule-driven discount via the
/// pricing engine and then records one usage row per applied code atomically. Usage
/// recording is idempotent on the supplied key and fails closed on usage limits: a code
/// whose usage cannot be recorded is dropped from the applied set and reported as rejected.
/// The final price decision is also appended to the durable pricing-decision ledger
/// (issue #400) whenever the command carries a stable correlation key (an explicit
/// idempotency key or order linkage), with the full per-code breakdown and the stacking
/// rule that governed resolution.
/// Redemption from untrusted (anonymous) contexts is CAPTCHA-gated (issue #418): when the
/// gate is enabled, a missing or unverifiable token rejects every code — fail closed.
/// Recorded redemptions feed the unusual-usage velocity alerting (issue #418).
/// </summary>
public sealed class ApplyPromoCodesCommandHandler(
    IPricingEngineService pricingEngine,
    IPromoCodeRepository promoCodeRepository,
    IPromoCodeUsageRecorder usageRecorder,
    IPromoCodeAuditLogger auditLogger,
    GameGuild.Commerce.IPricingDecisionLedgerWriter pricingLedgerWriter,
    ILogger<ApplyPromoCodesCommandHandler> logger,
    IPromoRedemptionCaptchaGate? captchaGate = null,
    IPromoUsageAnomalyEvaluator? anomalyEvaluator = null,
    IOptions<PromoAbuseDetectionOptions>? abuseOptions = null)
    : ICommandHandler<ApplyPromoCodesCommand, PromoCodeApplicationResult>
{
    public async Task<PromoCodeApplicationResult> Handle(
        ApplyPromoCodesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Issue #418 anti-farming: untrusted (anonymous) redemption contexts must pass the
        // CAPTCHA gate when it is enabled. Fail closed — a denied gate rejects every code
        // before any pricing or usage work happens.
        if (captchaGate is not null)
        {
            var gateDecision = captchaGate.Check(isTrustedContext: request.UserId.HasValue, request.CaptchaToken);
            if (!gateDecision.Allowed)
            {
                var rejected = request.PromoCodes
                    .Select(code => new RejectedPromoCode(code, gateDecision.Reason ?? "CAPTCHA verification failed"))
                    .ToList();
                return new PromoCodeApplicationResult(
                    request.OrderAmount,
                    request.OrderAmount,
                    0,
                    [],
                    rejected);
            }
        }

        var result = await pricingEngine.ApplyPromoCodesAsync(
            request.OrderAmount,
            request.PromoCodes,
            request.ProductId,
            request.UserId,
            request.PlanId,
            cancellationToken).ConfigureAwait(false);

        var usageRecorded = false;

        if (request.UserId.HasValue && result.AppliedCodes.Count > 0)
        {
            var baseKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"apply:{Guid.NewGuid():N}"
                : request.IdempotencyKey.Trim();

            var applied = new List<AppliedPromoCode>();
            var rejected = new List<RejectedPromoCode>(result.RejectedCodes);

            foreach (var entry in result.AppliedCodes)
            {
                var promoCode = await promoCodeRepository.GetByCodeAsync(entry.Code, cancellationToken)
                    .ConfigureAwait(false);

                if (promoCode is null)
                {
                    rejected.Add(new RejectedPromoCode(entry.Code, "Code not found"));
                    continue;
                }

                var perCodeKey = $"{baseKey}:code:{promoCode.Code}";
                var (status, usage) = await usageRecorder.RecordAsync(
                    promoCode,
                    request.UserId.Value,
                    entry.DiscountAmount,
                    promoCode.Currency,
                    perCodeKey,
                    request.OrderId,
                    null,
                    null,
                    cancellationToken).ConfigureAwait(false);

                switch (status)
                {
                    case PromoUsageRecordingStatus.Recorded:
                        applied.Add(entry);
                        usageRecorded = true;
                        await auditLogger.LogRedemptionAsync(
                            request.UserId.Value, promoCode.Code, entry.DiscountAmount, promoCode.Currency, usage?.Id, cancellationToken).ConfigureAwait(false);
                        await EvaluateRedemptionVelocityAsync(request.UserId.Value, promoCode.Code, cancellationToken).ConfigureAwait(false);
                        break;

                    case PromoUsageRecordingStatus.Replay:
                        // Idempotent replay: keep the code applied but never count it twice.
                        applied.Add(entry);
                        usageRecorded = true;
                        break;

                    case PromoUsageRecordingStatus.RejectedTotalLimit:
                        rejected.Add(new RejectedPromoCode(entry.Code, "Promo code has reached maximum uses"));
                        break;

                    case PromoUsageRecordingStatus.RejectedPerUserLimit:
                        rejected.Add(new RejectedPromoCode(entry.Code, "You have already used this promo code the maximum number of times"));
                        break;

                    default:
                        logger.LogWarning("Unhandled promo usage recording status {Status} for code {Code}", status, promoCode.Code);
                        rejected.Add(new RejectedPromoCode(entry.Code, "Usage could not be recorded"));
                        break;
                }
            }

            var totalDiscount = applied.Sum(a => a.DiscountAmount);
            result = result with
            {
                AppliedCodes = applied,
                RejectedCodes = rejected,
                TotalDiscount = totalDiscount,
                FinalAmount = Math.Max(0, request.OrderAmount - totalDiscount)
            };
        }

        await RecordPricingDecisionAsync(request, result, cancellationToken).ConfigureAwait(false);

        return result with { UsageRecorded = usageRecorded };
    }

    /// <summary>
    /// Unusual-usage velocity check (issue #418): counts the user's recorded redemptions
    /// inside the configured window (the current one included) and hands the count to the
    /// anomaly evaluator, which publishes a security event when the threshold is breached.
    /// </summary>
    private async Task EvaluateRedemptionVelocityAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        if (anomalyEvaluator is null)
        {
            return;
        }

        try
        {
            var windowMinutes = Math.Clamp(abuseOptions?.Value.RedemptionWindowMinutes ?? 10, 1, 1440);
            var windowStart = SystemClock.UtcNow.AddMinutes(-windowMinutes);
            var recentCount = await promoCodeRepository
                .CountRecentUsagesAsync(userId, windowStart, cancellationToken)
                .ConfigureAwait(false) + 1; // include the redemption that triggered the check

            await anomalyEvaluator
                .EvaluateRedemptionVelocityAsync(userId, code, recentCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: anomaly alerting must never change the redemption outcome.
            logger.LogWarning(exception, "Promo redemption velocity evaluation failed for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Appends the order-level promo pricing decision to the durable ledger (issue #400).
    /// Only decisions with a stable correlation key are recorded — an explicit idempotency
    /// key or an order linkage — so anonymous preview calls never write unattributable
    /// rows and every recorded row replays safely.
    /// </summary>
    private async Task RecordPricingDecisionAsync(
        ApplyPromoCodesCommand request,
        PromoCodeApplicationResult result,
        CancellationToken cancellationToken)
    {
        var ledgerKey = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"{request.IdempotencyKey.Trim()}:pricing"
            : request.OrderId.HasValue
                ? $"promo-apply:{request.OrderId.Value:N}:pricing"
                : null;

        if (ledgerKey is null)
        {
            return;
        }

        var currency = result.AppliedCodes.Count > 0
            ? (await promoCodeRepository.GetByCodeAsync(result.AppliedCodes[0].Code, cancellationToken).ConfigureAwait(false))?.Currency
            : null;

        var breakdown = GameGuild.Commerce.PricingDecisionBreakdown.Empty(currency ?? "USD", 1)
            with
        {
            BaseUnitPrice = request.OrderAmount,
            BaseAmount = request.OrderAmount,
            PromoApplications = result.AppliedCodes
                .Select((application, index) => new GameGuild.Commerce.PromoApplicationBreakdown(
                    application.Code,
                    null,
                    application.Type?.ToString(),
                    application.DiscountPercentage,
                    application.DiscountAmount,
                    index + 1))
                .ToList(),
            PromoDiscountTotal = result.TotalDiscount,
            FinalAmount = result.FinalAmount
        };

        var (status, _) = await pricingLedgerWriter
            .RecordAsync(
                new GameGuild.Commerce.PricingDecisionRecord(
                    DecisionType: GameGuild.Commerce.PricingDecisionType.OrderPromoApply,
                    TenantId: null,
                    ActorUserId: request.UserId,
                    ActorIpAddress: null,
                    Order: request.OrderId,
                    OrderLineItem: null,
                    PaymentId: null,
                    Subscription: null,
                    BillingCycleNumber: null,
                    Product: request.ProductId,
                    Breakdown: breakdown,
                    AppliedPricingRuleId: null,
                    AppliedStackingRuleName: result.StackingRuleName,
                    IdempotencyKey: ledgerKey),
                cancellationToken)
            .ConfigureAwait(false);

        if (status == GameGuild.Commerce.PricingDecisionRecordingStatus.Rejected)
        {
            logger.LogWarning(
                "Pricing decision ledger rejected the order promo application for order {OrderId}",
                request.OrderId);
        }
    }
}
