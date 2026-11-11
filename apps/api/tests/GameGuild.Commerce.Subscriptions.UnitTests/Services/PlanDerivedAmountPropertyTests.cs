using FsCheck;
using FsCheck.Xunit;
using GameGuild.Commerce;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

/// <summary>
///     Property-based invariants of the plan-derived amounts (post-#1266 / #1205 G-13):
///     the plan's interval price is the authoritative base — the client-supplied amount
///     NEVER influences the charged amount (it is advisory only, logged and discarded);
///     proration derives from plan interval prices and dates only, with the credit never
///     exceeding the old plan price and the charge never exceeding the new plan price at
///     the money boundary; promo discounts reduce what the customer pays without ever
///     changing the plan's own cents.
/// </summary>
public sealed class PlanDerivedAmountPropertyTests
{
    private static readonly BillingCalculator Calculator = new();

    private static decimal RoundToMinor(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    // ---------- Proration: the plan interval prices and dates are the only inputs ----------

    /// <summary>
    ///     For every pair of plan prices and every effective date inside the period, the
    ///     proration credit never exceeds the old plan's interval price and the charge for
    ///     the new plan never exceeds the new plan's interval price once rounded to the
    ///     currency's minor units (sum of parts never exceeds the plan-derived base).
    /// </summary>
    [Property(MaxTest = 400)]
    public Property Proration_CreditAndCharge_NeverExceedTheirPlanPrices(
        int oldCents,
        int newCents,
        byte periodLengthDays,
        byte effectiveOffsetDays)
    {
        var oldAmount = new Money(Math.Max(0, oldCents % 10_000_000) / 100m);
        var newAmount = new Money(Math.Max(0, newCents % 10_000_000) / 100m);
        var totalDays = 1 + (periodLengthDays % 365);
        var offset = effectiveOffsetDays % totalDays;
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddDays(totalDays);
        var effectiveDate = periodStart.AddDays(offset);

        var proration = Calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        var creditAtMoney = RoundToMinor(proration.CreditForUnused);
        var chargeAtMoney = RoundToMinor(proration.ChargeForNew);

        return (creditAtMoney <= oldAmount.Amount)
            .ToProperty()
            .Label($"credit {creditAtMoney} exceeds the old plan price {oldAmount.Amount}")
            .And((chargeAtMoney <= newAmount.Amount)
                .ToProperty()
                .Label($"charge {chargeAtMoney} exceeds the new plan price {newAmount.Amount}"))
            .And((proration.CreditForUnused >= 0 && proration.ChargeForNew >= 0)
                .ToProperty()
                .Label("credit and charge must be non-negative"));
    }

    /// <summary>
    ///     The net adjustment is exactly the charge minus the credit, and at the period
    ///     boundaries the degenerate cases hold: an effective date at the period end zeroes
    ///     everything (nothing left to credit or charge).
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Proration_NetIsChargeMinusCredit_AndVanishesAtPeriodEnd(
        int oldCents,
        int newCents,
        byte periodLengthDays)
    {
        var oldAmount = new Money(Math.Max(0, oldCents % 10_000_000) / 100m);
        var newAmount = new Money(Math.Max(0, newCents % 10_000_000) / 100m);
        var totalDays = 1 + (periodLengthDays % 365);
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddDays(totalDays);

        var atEnd = Calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, periodEnd);
        // Any strictly interior day exercises the net-adjustment identity (charge − credit
        // by construction); the day before the period end avoids integer midpoint division.
        var inside = Calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, periodEnd.AddDays(-1));

        return (atEnd.CreditForUnused == 0 && atEnd.ChargeForNew == 0 && atEnd.NetAdjustment == 0)
            .ToProperty()
            .Label("an effective date at the period end must zero the proration")
            .And(inside.NetAdjustment.Equals(inside.ChargeForNew - inside.CreditForUnused)
                .ToProperty()
                .Label("net adjustment must equal charge minus credit"));
    }

    /// <summary>
    ///     The proration math never sees a client amount: the same (planOld, planNew, dates)
    ///     input always produces the same proration — determinism the idempotency of quotes
    ///     relies on.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Proration_IsDeterministicForIdenticalPlanInputs(
        int oldCents,
        int newCents,
        byte periodLengthDays,
        byte effectiveOffsetDays)
    {
        var oldAmount = new Money(Math.Max(0, oldCents % 10_000_000) / 100m);
        var newAmount = new Money(Math.Max(0, newCents % 10_000_000) / 100m);
        var totalDays = 1 + (periodLengthDays % 365);
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddDays(totalDays);
        var effectiveDate = periodStart.AddDays(effectiveOffsetDays % totalDays);

        var first = Calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);
        var second = Calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        return first.Equals(second)
            .ToProperty()
            .Label("identical plan inputs produced different prorations");
    }

    // ---------- Derived interval prices: multiplication rules and bounds ----------

    /// <summary>
    ///     Derived fallback prices follow the plan's monthly cents: monthly is the plan
    ///     price itself, quarterly x3, semiannually x6, biannually x24; the weekly price is
    ///     the truncating quarter (documented historical behavior — four weekly payments
    ///     never exceed the monthly price); an explicit annual price point always wins; a
    ///     derived annual price never exceeds monthly x 12 and a 0% discount keeps the
    ///     historical monthly x 12 exactly.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property DerivedIntervalPrices_FollowThePlanCents_WithBoundedAnnualDiscount(
        int monthlyCents,
        int explicitAnnualCents,
        byte discountPercent)
    {
        var monthly = Math.Abs((long)monthlyCents % 100_000_000L);
        var plan = new SubscriptionPlan("Pro", "pro", monthly, "USD")
        {
            Type = SubscriptionPlanType.Paid,
            AnnualDiscountPercent = discountPercent % 101m
        };

        var monthlyDerived = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.Monthly);
        var quarterly = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.Quarterly);
        var semiAnnual = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.SemiAnnually);
        var biannual = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.Biannually);
        var weekly = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.Weekly);
        var derivedAnnual = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.Annually);

        plan.AnnualPriceInCents = Math.Abs((long)explicitAnnualCents % 100_000_000L);
        var explicitAnnual = SubscriptionIntervalPricingService.DeriveFallbackPriceInCents(plan, BillingCycle.Annually);

        var undiscountedAnnual = SubscriptionPlanPricingRules.DeriveAnnualPriceInCents(monthly, null, 0m);

        return monthlyDerived.Equals(monthly)
            .ToProperty()
            .Label("monthly derived price must be the plan's monthly cents")
            .And(quarterly.Equals(monthly * 3).ToProperty().Label("quarterly must be monthly x 3"))
            .And(semiAnnual.Equals(monthly * 6).ToProperty().Label("semiannual must be monthly x 6"))
            .And(biannual.Equals(monthly * 24).ToProperty().Label("biannual must be monthly x 24"))
            .And((weekly * 4 <= monthly && monthly < (weekly + 1) * 4)
                .ToProperty()
                .Label($"weekly {weekly} from monthly {monthly} must be the truncating quarter"))
            .And(explicitAnnual.Equals(plan.AnnualPriceInCents.Value)
                .ToProperty()
                .Label("an explicit annual price point must win verbatim"))
            .And((derivedAnnual >= 0 && derivedAnnual <= monthly * 12)
                .ToProperty()
                .Label($"derived annual {derivedAnnual} escaped [0, monthly x 12] = [0, {monthly * 12}]"))
            .And(undiscountedAnnual.Equals(monthly * 12)
                .ToProperty()
                .Label("a 0% annual discount must keep the historical monthly x 12"));
    }

    // ---------- Checkout: the client-supplied amount NEVER influences the charged amount ----------

    /// <summary>
    ///     Post-#1266 price integrity: for every client-supplied amount (0, fractional,
    ///     huge — anything a direct API caller could try), the created subscription is
    ///     charged the plan's authoritative interval price. The client value is advisory
    ///     only: never persisted, never minted into the charge.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property CreateSubscription_IgnoresEveryClientSuppliedAmount(int clientAmountCentsNumerator, uint clientAmountScale)
    {
        var planCents = 1 + Math.Abs(clientAmountCentsNumerator % 199_999); // never the -1 custom-quote sentinel
        var plan = new SubscriptionPlan("Pro", "pro", planCents, "USD") { Type = SubscriptionPlanType.Paid };
        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? captured = null;
        var repository = new Mock<ISubscriptionRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        var handler = new CreateSubscriptionCommandHandler(
            repository.Object,
            planRepository.Object,
            Mock.Of<ILogger<CreateSubscriptionCommandHandler>>());

        // Any non-negative decimal a caller might send, including sub-cent and huge values.
        var clientAmount = Math.Abs((decimal)clientAmountCentsNumerator % 1_000_000_000m) / 100m
            + (clientAmountScale % 1000) / 100_000m;

        handler.Handle(
            new CreateSubscriptionCommand(
                TenantId: Guid.NewGuid(),
                PlanId: Guid.NewGuid(),
                CreatedByUserId: Guid.NewGuid(),
                BillingCycle: BillingCycle.Monthly,
                Amount: clientAmount),
            CancellationToken.None)
            .GetAwaiter().GetResult();

        return captured.ShouldNotBeNull()
            .Label("the subscription was never persisted")
            .And(captured!.Amount.Amount.Equals(planCents / 100m)
                .ToProperty()
                .Label($"client amount {clientAmount} influenced the charge: got {captured.Amount.Amount}, expected the plan price {planCents / 100m}"))
            .And(plan.MonthlyPriceInCents.Equals(planCents)
                .ToProperty()
                .Label("the plan's own cents must never change at checkout"));
    }

    /// <summary>
    ///     An explicitly configured interval price point (resolved through
    ///     <see cref="ISubscriptionIntervalPricingService"/>) is authoritative for every
    ///     cycle — the client amount cannot override it either.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property CreateSubscription_UsesConfiguredIntervalPrice_WhateverTheClientSends(int clientCents, byte cycleChoice)
    {
        var plan = new SubscriptionPlan("Pro", "pro", 1999, "USD") { Type = SubscriptionPlanType.Paid };
        var configuredPrice = Math.Abs((long)clientCents % 100_000) / 100m + 0.01m;
        var cycles = new[] { BillingCycle.Monthly, BillingCycle.Quarterly, BillingCycle.SemiAnnually, BillingCycle.Annually };
        var cycle = cycles[cycleChoice % cycles.Length];

        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? captured = null;
        var repository = new Mock<ISubscriptionRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        var intervalPricing = new Mock<ISubscriptionIntervalPricingService>();
        intervalPricing
            .Setup(service => service.ResolvePriceForCycleAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<BillingCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(configuredPrice, "USD"));

        var handler = new CreateSubscriptionCommandHandler(
            repository.Object,
            planRepository.Object,
            Mock.Of<ILogger<CreateSubscriptionCommandHandler>>(),
            intervalPricingService: intervalPricing.Object);

        var clientAmount = Math.Abs((decimal)clientCents % 10_000_000m) / 100m;

        handler.Handle(
            new CreateSubscriptionCommand(
                TenantId: Guid.NewGuid(),
                PlanId: Guid.NewGuid(),
                CreatedByUserId: Guid.NewGuid(),
                BillingCycle: cycle,
                Amount: clientAmount),
            CancellationToken.None)
            .GetAwaiter().GetResult();

        return captured.ShouldNotBeNull()
            .Label("the subscription was never persisted")
            .And(captured!.Amount.Amount.Equals(configuredPrice)
                .ToProperty()
                .Label($"client amount {clientAmount} overrode the configured interval price {configuredPrice}"));
    }

    /// <summary>
    ///     Promo discounts reduce what the customer pays but never mint or change the plan's
    ///     cents: the quote is requested against the PLAN's authoritative price (never the
    ///     client's amount), the subscription is created on the quote's final amount, which
    ///     never exceeds the plan price, and the plan's own price is untouched afterwards.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property CreateSubscription_PromoReducesThePayment_NeverThePlanCents(
        int clientCents,
        int discountCents)
    {
        var planCents = 5000; // $50.00 plan
        var plan = new SubscriptionPlan("Pro", "pro", planCents, "USD") { Type = SubscriptionPlanType.Paid };
        var planPrice = planCents / 100m;
        var discount = Math.Clamp(Math.Abs((decimal)discountCents % 100_000m) / 100m, 0m, planPrice);

        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? captured = null;
        var repository = new Mock<ISubscriptionRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        SubscriptionPromoQuoteRequest? quoteRequest = null;
        var promo = new Mock<ISubscriptionPromoCoordinator>();
        promo
            .Setup(coordinator => coordinator.QuoteForSubscriptionCreationAsync(It.IsAny<SubscriptionPromoQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPromoQuoteRequest, CancellationToken>((request, _) => quoteRequest = request)
            .ReturnsAsync((SubscriptionPromoQuoteRequest request, CancellationToken _) =>
                new SubscriptionPromoQuoteResult(
                    request.Amount,
                    request.Amount - discount,
                    discount,
                    [new SubscriptionPromoCodeApplication(Guid.NewGuid(), "SAVE", discount, "AmountOff")],
                    [],
                    "USD"));
        promo
            .Setup(coordinator => coordinator.RecordSubscriptionUsageAsync(It.IsAny<SubscriptionPromoUsageRecordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new CreateSubscriptionCommandHandler(
            repository.Object,
            planRepository.Object,
            Mock.Of<ILogger<CreateSubscriptionCommandHandler>>(),
            promoCoordinator: promo.Object);

        var clientAmount = Math.Abs((decimal)clientCents % 100_000m) / 100m;

        handler.Handle(
            new CreateSubscriptionCommand(
                TenantId: Guid.NewGuid(),
                PlanId: Guid.NewGuid(),
                CreatedByUserId: Guid.NewGuid(),
                BillingCycle: BillingCycle.Monthly,
                Amount: clientAmount,
                PromoCodes: ["SAVE"]),
            CancellationToken.None)
            .GetAwaiter().GetResult();

        var expectedFinal = planPrice - discount;

        return quoteRequest.ShouldNotBeNull()
            .Label("the promo coordinator was never asked for a quote")
            .And(quoteRequest!.Amount.Equals(planPrice)
                .ToProperty()
                .Label($"the promo was quoted against {quoteRequest.Amount} instead of the plan price {planPrice} (client amount {clientAmount} leaked in)"))
            .And(captured.ShouldNotBeNull()
                .Label("the subscription was never persisted")
            .And(captured!.Amount.Amount.Equals(expectedFinal)
                .ToProperty()
                .Label($"subscription charged {captured.Amount.Amount}, expected the discounted {expectedFinal}"))
            .And((captured.Amount.Amount <= planPrice)
                .ToProperty()
                .Label("a promo discount must never increase the charge above the plan price"))
            .And(plan.MonthlyPriceInCents.Equals(planCents)
                .ToProperty()
                .Label("the plan's own cents must never change from a promo quote")));
    }
}

internal static class PropertyTestExtensions
{
    /// <summary>Null-check as an FsCheck property so object-shaped assertions compose with .And.</summary>
    public static Property ShouldNotBeNull<T>(this T? value) =>
        (value is not null).ToProperty();
}
