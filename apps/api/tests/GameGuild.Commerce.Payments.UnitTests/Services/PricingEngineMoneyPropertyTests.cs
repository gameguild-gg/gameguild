using FsCheck;
using FsCheck.Xunit;
using GameGuild.Commerce;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Property-based money invariants of the unified pricing engine (issue #417, post-#1266):
///     the plan-resolved price is the authoritative base — promo discounts reduce what the
///     customer pays but never change the plan-derived parts (base, add-ons) and never
///     exceed what they were applied to; the sum of parts always equals the subtotal;
///     proration flows through as an exact rounded adjustment on top of the discounted base.
/// </summary>
public sealed class PricingEngineMoneyPropertyTests
{
    private static PricingEngine CreateEngine(
        Mock<IPlanPricingResolver> planResolver,
        Mock<IPromoDiscountCalculator> promoCalculator,
        Mock<IPlanProrationCalculator>? prorationCalculator = null) => new(
        planResolver.Object,
        (prorationCalculator ?? new Mock<IPlanProrationCalculator>()).Object,
        promoCalculator.Object,
        Mock.Of<ITaxCalculationService>(),
        Mock.Of<IWalletService>(),
        new CurrencyConversionService(new ManualExchangeRateProvider([])),
        Mock.Of<IPricingDecisionLedgerWriter>(),
        new MemoryCache(new MemoryCacheOptions()));

    private static void SetupPlanPrice(Mock<IPlanPricingResolver> planResolver, decimal monthly) =>
        planResolver
            .Setup(resolver => resolver.GetPlanPriceAsync(It.IsAny<Guid>(), It.IsAny<BillingCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(monthly, "USD"));

    private static readonly DateTime FixedEffectiveDate = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     Sum of parts == charged base: for any plan price and any set of independently
    ///     priced add-ons, the subtotal is exactly base + add-ons, the reported line items
    ///     sum to the same value, and the engine's own self-validation raises nothing.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property PartsSum_AlwaysEqualsTheSubtotal(
        int baseCents,
        int[] addOnCents,
        byte[] addOnQuantities)
    {
        var planResolver = new Mock<IPlanPricingResolver>();
        var basePrice = Math.Max(0, baseCents % 10_000_000) / 100m;
        SetupPlanPrice(planResolver, basePrice);

        var engine = CreateEngine(planResolver, new Mock<IPromoDiscountCalculator>());

        var priced = addOnCents
            .Select((cents, index) =>
            {
                var unitPrice = Math.Max(0, cents % 10_000_000) / 100m;
                var quantity = addOnQuantities.Length == 0 ? 1 : addOnQuantities[index % addOnQuantities.Length] % 51;
                return (
                    Input: new PricingAddOnInput(
                        $"addon-{index}",
                        $"Add-on {index}",
                        quantity,
                        new Money(unitPrice, "USD")),
                    Total: decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero));
            })
            .ToArray();
        var expectedPartsSum = basePrice + priced.Sum(addOn => addOn.Total);

        var result = engine.CalculateAsync(new PricingCalculationRequest(
            PlanId: Guid.NewGuid(),
            AddOns: priced.Select(addOn => addOn.Input).ToArray(),
            IncludeTax: false,
            RecordDecision: false))
            .GetAwaiter().GetResult();

        var lineItemSum = result.LineItems.Where(item => item.Type != "proration").Sum(item => item.Amount.Amount);

        return result.Subtotal.Amount.Equals(expectedPartsSum)
            .ToProperty()
            .Label($"subtotal {result.Subtotal.Amount} does not equal the parts sum {expectedPartsSum}")
            .And(lineItemSum.Equals(result.Subtotal.Amount)
                .ToProperty()
                .Label($"line items sum {lineItemSum} diverges from the subtotal {result.Subtotal.Amount}"))
            .And((result.ValidationErrors.Count == 0)
                .ToProperty()
                .Label($"the engine self-validation reported: {string.Join("; ", result.ValidationErrors)}"));
    }

    /// <summary>
    ///     Promo discounts reduce the customer's payment but never mint or change the
    ///     plan-derived cents: for any plan price and any discount bounded by the
    ///     discountable amount, base plan price, subtotal and line items are identical to a
    ///     promo-free calculation; the applied discount never exceeds what it was applied
    ///     to; the amount due is exactly subtotal - discount (never below zero, never above
    ///     the undiscounted total).
    /// </summary>
    [Property(MaxTest = 400)]
    public Property PromoDiscount_ReducesThePayment_NeverThePlanCents(
        int baseCents,
        int addOnCents,
        int discountCents,
        bool planOnlyScope)
    {
        var basePrice = Math.Max(1, baseCents % 100_000) / 100m;
        var addOn = Math.Max(0, addOnCents % 100_000) / 100m;
        var subtotal = basePrice + addOn;
        var discountable = planOnlyScope ? basePrice : subtotal;
        var discount = Math.Clamp(Math.Max(0, discountCents % 200_000) / 100m, 0m, discountable);

        var planResolver = new Mock<IPlanPricingResolver>();
        SetupPlanPrice(planResolver, basePrice);

        var promo = new Mock<IPromoDiscountCalculator>();
        promo
            .Setup(calculator => calculator.ApplyAsync(It.IsAny<PromoDiscountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromoDiscountResult(
                discount,
                [new AppliedPromoDiscount("SAVE", "save", discount, 10m, DiscountType.FixedAmount, 1, true, 1)],
                [],
                null));

        var engine = CreateEngine(planResolver, promo);
        var request = new PricingCalculationRequest(
            PlanId: Guid.NewGuid(),
            AddOns: [new PricingAddOnInput("extra", "Extra", 1, new Money(addOn, "USD"))],
            PromoCodes: ["SAVE"],
            DiscountScope: planOnlyScope ? PricingDiscountScope.PlanOnly : PricingDiscountScope.AllItems,
            IncludeTax: false,
            RecordDecision: false);

        var withPromo = engine.CalculateAsync(request).GetAwaiter().GetResult();
        var promoFree = engine.CalculateAsync(request with { PromoCodes = null }).GetAwaiter().GetResult();

        var expectedDue = Math.Max(0, subtotal - discount);

        return withPromo.BasePlanPrice.Equals(promoFree.BasePlanPrice)
            .ToProperty()
            .Label("a promo discount changed the base plan price")
            .And(withPromo.Subtotal.Equals(promoFree.Subtotal)
                .ToProperty()
                .Label("a promo discount changed the plan-derived subtotal (parts)"))
            .And(withPromo.TotalDiscounts.Amount.Equals(discount)
                .ToProperty()
                .Label($"applied discount {withPromo.TotalDiscounts.Amount} differs from the quoted {discount}"))
            .And((withPromo.TotalDiscounts.Amount <= discountable)
                .ToProperty()
                .Label($"discount {withPromo.TotalDiscounts.Amount} exceeds the discountable amount {discountable}"))
            .And(withPromo.SubtotalAfterDiscounts.Amount.Equals(expectedDue)
                .ToProperty()
                .Label($"subtotal after discounts {withPromo.SubtotalAfterDiscounts.Amount} is not {expectedDue}"))
            .And(withPromo.AmountDue.Amount.Equals(expectedDue)
                .ToProperty()
                .Label($"amount due {withPromo.AmountDue.Amount} is not the discounted {expectedDue}"))
            .And((withPromo.AmountDue.Amount <= promoFree.AmountDue.Amount)
                .ToProperty()
                .Label("a promo discount increased what the customer pays"))
            .And((withPromo.ValidationErrors.Count == 0)
                .ToProperty()
                .Label($"the engine self-validation reported: {string.Join("; ", withPromo.ValidationErrors)}"));
    }

    /// <summary>
    ///     Proration is an exact, minor-unit-rounded adjustment on top of the discounted
    ///     base: for any credit/charge pair the engine reports the proration parts verbatim,
    ///     applies round(net) into the total, and never lets a credit push the total below
    ///     zero.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property Proration_AppliesTheNetAdjustment_ExactlyOnce(
        int baseCents,
        int discountCents,
        int creditCents,
        int chargeCents)
    {
        var basePrice = Math.Max(1, baseCents % 100_000) / 100m;
        var discount = Math.Clamp(Math.Max(0, discountCents % 100_000) / 100m, 0m, basePrice);
        var credit = Math.Max(0, creditCents % 100_000) / 100m;
        var charge = Math.Max(0, chargeCents % 100_000) / 100m;
        var net = charge - credit;

        var planResolver = new Mock<IPlanPricingResolver>();
        SetupPlanPrice(planResolver, basePrice);

        var promo = new Mock<IPromoDiscountCalculator>();
        promo
            .Setup(calculator => calculator.ApplyAsync(It.IsAny<PromoDiscountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromoDiscountResult(
                discount,
                [new AppliedPromoDiscount("SAVE", "save", discount, 10m, DiscountType.FixedAmount, 1, true, 1)],
                [],
                null));

        var proration = new Mock<IPlanProrationCalculator>();
        proration
            .Setup(calculator => calculator.CalculateProrationAsync(It.IsAny<PlanProrationInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanProrationResult(credit, charge, net, FixedEffectiveDate, 30, 15));

        var engine = CreateEngine(planResolver, promo, proration);
        var result = engine.CalculateAsync(new PricingCalculationRequest(
            PlanId: Guid.NewGuid(),
            PromoCodes: ["SAVE"],
            Proration: new PricingProrationInput(Guid.NewGuid(), Guid.NewGuid(), FixedEffectiveDate.AddDays(-14), FixedEffectiveDate.AddDays(15), FixedEffectiveDate),
            IncludeTax: false,
            RecordDecision: false))
            .GetAwaiter().GetResult();

        var expectedAdjustment = decimal.Round(net, 2, MidpointRounding.AwayFromZero);
        var expectedTotal = Math.Max(0, (basePrice - discount) + expectedAdjustment);

        return (result.Proration is not null)
            .ToProperty()
            .Label("the proration result was not attached")
            .And(result.Proration!.CreditForUnused.Equals(credit)
                .ToProperty()
                .Label("the engine altered the proration credit"))
            .And(result.Proration.ChargeForNew.Equals(charge)
                .ToProperty()
                .Label("the engine altered the proration charge"))
            .And(result.ProrationAdjustment.Equals(expectedAdjustment)
                .ToProperty()
                .Label($"proration adjustment {result.ProrationAdjustment} is not the rounded net {expectedAdjustment}"))
            .And(result.Total.Amount.Equals(expectedTotal)
                .ToProperty()
                .Label($"total {result.Total.Amount} is not max(0, discounted base + proration) = {expectedTotal}"))
            .And((result.Total.Amount >= 0)
                .ToProperty()
                .Label("the total went negative"));
    }
}
