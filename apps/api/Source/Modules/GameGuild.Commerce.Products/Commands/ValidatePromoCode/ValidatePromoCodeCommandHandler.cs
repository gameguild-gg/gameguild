using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for validating a promo code. Every attempt (successful or not) is persisted as a
/// <see cref="PromoCodeValidationAttempt"/> so campaign performance can compute a
/// validations-to-applications conversion rate (issue #418). Attempt persistence is
/// best effort: a tracking write failure is logged and never fails the validation itself.
/// Attempts also feed the unusual-usage velocity alerting (issue #418): when the subject's
/// attempt count inside the configured window crosses the burst threshold, an anomaly
/// security event is published through the central pipeline.
/// </summary>
public sealed class ValidatePromoCodeCommandHandler(
    IPricingEngineService pricingEngine,
    IPromoCodeRepository promoCodeRepository,
    ILogger<ValidatePromoCodeCommandHandler> logger,
    IPromoUsageAnomalyEvaluator? anomalyEvaluator = null,
    IOptions<PromoAbuseDetectionOptions>? abuseOptions = null)
    : ICommandHandler<ValidatePromoCodeCommand, PromoCodeValidationResult>
{
    public async Task<PromoCodeValidationResult> Handle(
        ValidatePromoCodeCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await pricingEngine.ValidatePromoCodeAsync(
            request.Code,
            request.OrderAmount,
            request.ProductId,
            request.UserId,
            request.PlanId,
            cancellationToken).ConfigureAwait(false);

        await RecordAttemptAsync(request, result, cancellationToken).ConfigureAwait(false);
        await EvaluateVelocityAsync(request, result, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Persists the conversion-tracking denominator. The outcome carries the resolved
    /// promo code id, so unknown code strings remain attributable only by their raw text —
    /// code-farming patterns stay observable without inflating any campaign's numbers.
    /// </summary>
    private async Task RecordAttemptAsync(
        ValidatePromoCodeCommand request,
        PromoCodeValidationResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await promoCodeRepository.RecordValidationAttemptAsync(
                request.Code,
                result.PromoCodeId,
                request.UserId,
                result.Outcome ?? (result.IsValid
                    ? PromoCodeValidationOutcome.Valid
                    : PromoCodeValidationOutcome.Invalid),
                result.ErrorMessage,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to persist promo validation attempt for code {Code}; the validation result is unaffected",
                request.Code);
        }
    }

    /// <summary>
    /// Unusual-usage velocity check (issue #418): counts the subject's attempts inside the
    /// configured window (the current one included) and hands the count to the anomaly
    /// evaluator. Anonymous subjects are keyed by the normalized code string, exposing
    /// scripted probing of a single code.
    /// </summary>
    private async Task EvaluateVelocityAsync(
        ValidatePromoCodeCommand request,
        PromoCodeValidationResult result,
        CancellationToken cancellationToken)
    {
        if (anomalyEvaluator is null)
        {
            return;
        }

        try
        {
            var windowMinutes = Math.Clamp(abuseOptions?.Value.ValidationWindowMinutes ?? 10, 1, 1440);
            var windowStart = SystemClock.UtcNow.AddMinutes(-windowMinutes);
            var normalized = PromoCodeNormalizer.Normalize(request.Code) ?? request.Code.Trim().ToUpperInvariant();

            var recentCount = await promoCodeRepository
                .CountRecentValidationAttemptsAsync(request.UserId, normalized, windowStart, cancellationToken)
                .ConfigureAwait(false) + 1; // include the attempt that triggered the check

            await anomalyEvaluator
                .EvaluateValidationVelocityAsync(normalized, request.UserId, recentCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: anomaly alerting must never change the validation outcome.
            logger.LogWarning(
                exception,
                "Promo validation velocity evaluation failed for code {Code}",
                request.Code);
        }
    }
}
