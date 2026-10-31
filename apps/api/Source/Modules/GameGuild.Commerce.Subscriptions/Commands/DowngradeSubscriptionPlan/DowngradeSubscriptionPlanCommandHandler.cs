using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for DowngradeSubscriptionPlanCommand (issue #418 plan-change promo application).
///     Delegates to the lifecycle service which handles the plan change and effective date
///     logic. When promo codes are supplied and the change carries a charge, the codes are
///     quoted against the target plan before the mutation (total rejection fails the
///     command before any state change) and the discount is applied to the charge after
///     the change succeeds. Credit-only downgrades return the codes rejected — there is
///     no charge to discount.
/// </summary>
/// <remarks>
///     Issue #409 remainder: the downgrade attempt runs the fraud signal evaluation through
///     <see cref="SubscriptionChangeFraudGate"/> BEFORE anything is mutated — Block refuses
///     the downgrade (422), Review persists the bounded evidence on the subscription.
/// </remarks>
public sealed class DowngradeSubscriptionPlanCommandHandler(
    ISubscriptionLifecycleService lifecycleService,
    ISubscriptionPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    ILogger<DowngradeSubscriptionPlanCommandHandler>? logger = null,
    GameGuild.Commerce.ISubscriptionPromoCoordinator? promoCoordinator = null,
    GameGuild.Commerce.Payments.IFraudDetectionService? fraudDetectionService = null,
    GameGuild.Commerce.IFraudReviewTicketCoordinator? reviewTicketCoordinator = null)
    : ICommandHandler<DowngradeSubscriptionPlanCommand, SubscriptionDowngradeResult>
{
    public async Task<SubscriptionDowngradeResult> Handle(DowngradeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameGuild.EntityNotFoundException("Subscription", request.SubscriptionId);
        var newPlan = await planRepository.GetByIdAsync(request.NewPlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameGuild.EntityNotFoundException("SubscriptionPlan", request.NewPlanId);

        // Issue #409: subscription-change-time risk scoring, before any mutation.
        await SubscriptionChangeFraudGate.AssessAsync(
            fraudDetectionService,
            logger ?? NullLogger<DowngradeSubscriptionPlanCommandHandler>.Instance,
            subscriptionRepository,
            subscription,
            GameGuild.Commerce.Payments.SubscriptionFraudChangeKind.Downgrade,
            SubscriptionChangeFraudGate.CycleAmountFor(subscription, newPlan).Amount,
            newPlan.Currency,
            request.FraudCountry,
            request.FraudSourceIpAddress,
            request.FraudDeviceFingerprint,
            request.FraudPayerEmail,
            request.FraudCardBin,
            request.FraudClientTimezone,
            reviewTicketCoordinator,
            cancellationToken).ConfigureAwait(false);

        // Issue #418: quote promo codes against the target plan before mutating anything.
        GameGuild.Commerce.SubscriptionPromoQuoteResult? promoQuote = null;
        if (request.PromoCodes is { Count: > 0 })
        {
            if (promoCoordinator is null)
            {
                throw new InvalidOperationException(
                    "Promo codes were supplied but no promo coordinator is registered for plan downgrades.");
            }

            var cycleAmount = SubscriptionChangeFraudGate.CycleAmountFor(subscription, newPlan);

            promoQuote = await promoCoordinator.QuoteForPlanChangeAsync(
                new GameGuild.Commerce.SubscriptionPlanChangePromoQuoteRequest(
                    request.SubscriptionId,
                    request.NewPlanId,
                    cycleAmount.Amount,
                    request.PromoCodes,
                    subscription.CreatedByUserId),
                cancellationToken).ConfigureAwait(false);

            if (promoQuote.AppliedCodes.Count == 0)
            {
                var reasons = string.Join("; ", promoQuote.RejectedCodes.Select(r => $"{r.Code}: {r.Reason}"));
                throw new InvalidOperationException(
                    $"None of the supplied promo codes could be applied to plan {request.NewPlanId}. {reasons}");
            }
        }

        var result = await lifecycleService.DowngradePlanAsync(request.SubscriptionId, request.NewPlanId, request.EffectiveDate, cancellationToken).ConfigureAwait(false);
        if (!result.Success || promoCoordinator is null || promoQuote is null)
        {
            return result;
        }

        // Credit-only downgrades have no charge to discount: report the codes as rejected.
        var creditIssued = result.CreditIssued?.Amount ?? 0m;
        if (creditIssued > 0m)
        {
            return result.WithPromo(
                0m,
                [],
                promoQuote.AppliedCodes
                    .Select(code => new GameGuild.Commerce.SubscriptionPromoRejectedCode(code.Code, "Downgrade issued credit; there is no charge to discount"))
                    .ToList());
        }

        // The downgrade charged for the new plan's remaining period: apply the quoted
        // discount (capped at the charge), record usage rows, return the discounted totals.
        var charge = result.UpdatedSubscription?.Amount.Amount ?? 0m;
        var currency = result.UpdatedSubscription?.Amount.Currency ?? "USD";

        var applied = new List<GameGuild.Commerce.SubscriptionPromoCodeApplication>();
        var remaining = Math.Max(0m, charge);
        foreach (var code in promoQuote.AppliedCodes)
        {
            if (remaining <= 0m)
            {
                break;
            }

            var discount = Math.Min(code.DiscountAmount, remaining);
            applied.Add(code with { DiscountAmount = discount });
            remaining -= discount;
        }

        if (applied.Count > 0)
        {
            await promoCoordinator.RecordSubscriptionUsageAsync(
                new GameGuild.Commerce.SubscriptionPromoUsageRecordRequest(
                    request.SubscriptionId,
                    result.UpdatedSubscription!.CreatedByUserId,
                    applied,
                    currency,
                    null,
                    request.PromoCodeIdempotencyKey ?? $"plan-downgrade:{request.SubscriptionId:N}:{request.NewPlanId:N}"),
                cancellationToken).ConfigureAwait(false);

            var totalDiscount = applied.Sum(c => c.DiscountAmount);
            logger?.LogInformation(
                "Subscription {SubscriptionId} downgraded to plan {NewPlanId} with {PromoCount} promo code(s); charge {OriginalCharge} → {DiscountedCharge} {Currency}",
                request.SubscriptionId,
                request.NewPlanId,
                applied.Count,
                charge,
                Math.Max(0m, charge - totalDiscount),
                currency);

            return result.WithPromo(
                totalDiscount,
                applied,
                promoQuote.RejectedCodes.Where(r => applied.All(a => !string.Equals(a.Code, r.Code, StringComparison.OrdinalIgnoreCase))).ToList());
        }

        return result;
    }
}
