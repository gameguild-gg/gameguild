using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for UpgradeSubscriptionPlanCommand (issue #418 plan-upgrade promo application).
///     Delegates to the lifecycle service which handles proration and plan change logic.
///     When promo codes are supplied they are quoted through
///     <see cref="GameGuild.Commerce.ISubscriptionPromoCoordinator"/> against the new plan's
///     cycle price BEFORE the mutation: a total rejection fails the command before any state
///     changes. After a successful upgrade the quote's discount is applied to the prorated
///     charge (capped at the charge), one usage row per applied code is recorded against the
///     subscription, and the discounted amount is returned.
/// </summary>
public sealed class UpgradeSubscriptionPlanCommandHandler(
    ISubscriptionLifecycleService lifecycleService,
    ISubscriptionPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    ILogger<UpgradeSubscriptionPlanCommandHandler>? logger = null,
    GameGuild.Commerce.ISubscriptionPromoCoordinator? promoCoordinator = null)
    : ICommandHandler<UpgradeSubscriptionPlanCommand, SubscriptionUpgradeResult>
{
    public async Task<SubscriptionUpgradeResult> Handle(UpgradeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        // Issue #418: quote promo codes against the new plan's cycle price before mutating
        // anything, so an unusable code fails the command with no partial state.
        GameGuild.Commerce.SubscriptionPromoQuoteResult? promoQuote = null;
        if (request.PromoCodes is { Count: > 0 })
        {
            if (promoCoordinator is null)
            {
                throw new InvalidOperationException(
                    "Promo codes were supplied but no promo coordinator is registered for plan upgrades.");
            }

            var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false)
                ?? throw new GameGuild.EntityNotFoundException("Subscription", request.SubscriptionId);
            var newPlan = await planRepository.GetByIdAsync(request.NewPlanId, cancellationToken).ConfigureAwait(false)
                ?? throw new GameGuild.EntityNotFoundException("SubscriptionPlan", request.NewPlanId);
            // Quote basis: the new plan's price for the subscription's billing cycle —
            // the same amount the lifecycle service charges on (mirrors its static mapping).
            var monthly = newPlan.GetMonthlyPrice();
            var cycleAmount = subscription.BillingCycle switch
            {
                BillingCycle.Annually => newPlan.GetAnnualPrice() ?? monthly * 12,
                BillingCycle.Quarterly => monthly * 3,
                BillingCycle.SemiAnnually => monthly * 6,
                BillingCycle.Biannually => monthly * 24,
                _ => monthly
            };

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
                // Fail closed before mutation, mirroring subscription creation.
                var reasons = string.Join("; ", promoQuote.RejectedCodes.Select(r => $"{r.Code}: {r.Reason}"));
                throw new InvalidOperationException(
                    $"None of the supplied promo codes could be applied to plan {request.NewPlanId}. {reasons}");
            }
        }

        var result = await lifecycleService.UpgradePlanAsync(request.SubscriptionId, request.NewPlanId, request.EffectiveDate, cancellationToken).ConfigureAwait(false);
        if (!result.Success || promoCoordinator is null || promoQuote is null)
        {
            return result;
        }

        // Apply the quoted discount to the prorated charge (capped), record usage rows
        // linked to the subscription, and return the discounted amounts.
        var charge = result.ProratedAmount?.Amount ?? 0m;
        var currency = result.ProratedAmount?.Currency ?? result.UpdatedSubscription?.Amount.Currency ?? "USD";

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
                    request.PromoCodeIdempotencyKey ?? $"plan-upgrade:{request.SubscriptionId:N}:{request.NewPlanId:N}"),
                cancellationToken).ConfigureAwait(false);

            var totalDiscount = applied.Sum(c => c.DiscountAmount);
            logger?.LogInformation(
                "Subscription {SubscriptionId} upgraded to plan {NewPlanId} with {PromoCount} promo code(s); prorated charge {OriginalCharge} → {DiscountedCharge} {Currency}",
                request.SubscriptionId,
                request.NewPlanId,
                applied.Count,
                charge,
                Math.Max(0m, charge - totalDiscount),
                currency);

            return result.WithPromo(
                new Money(Math.Max(0m, charge - totalDiscount), currency),
                totalDiscount,
                applied,
                promoQuote.RejectedCodes.Where(r => applied.All(a => !string.Equals(a.Code, r.Code, StringComparison.OrdinalIgnoreCase))).ToList());
        }

        return result;
    }
}
