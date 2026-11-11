using FsCheck;
using FsCheck.Xunit;
using GameGuild;
using Stripe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Property-based invariants of the refund minor-unit conversion (issue #1202 gaps N1,
///     fixed via #1266): for every currency/amount pair the amount → minor units → gateway
///     → minor units back → amount round-trip is lossless; zero-decimal (JPY-class)
///     currencies are never multiplied by 100; three-decimal (KWD-class) currencies scale
///     by 1000; and the conversion always uses the payment's OWN currency — never a
///     two-decimal default.
/// </summary>
public sealed class StripeAmountConverterPropertyTests
{
    /// <summary>Keeps every scaled product comfortably inside long/decimal range.</summary>
    private const int MaxMinorUnits = 99_999_999;

    /// <summary>
    ///     Stripe's API exponent for a currency, mirroring
    ///     <see cref="StripeAmountConverter"/>: ISK/UGX bill in two-decimal whole units,
    ///     MGA in whole units, everything else follows the ISO 4217 catalog.
    /// </summary>
    internal static int StripeExponent(string currency) =>
        currency is "ISK" or "UGX" ? 2
        : currency == "MGA" ? 0
        : Iso4217CurrencyCatalog.GetMinorUnitExponent(currency);

    /// <summary>All catalog currencies plus the three Stripe special cases.</summary>
    internal static IReadOnlyList<string> AllCurrencies() =>
        Iso4217CurrencyCatalog.All.Select(info => info.Code).Concat(["ISK", "UGX", "MGA"]).Distinct().ToArray();

    /// <summary>Rebuilds the major-unit amount from minor units for the Stripe exponent.</summary>
    internal static decimal MajorFromMinor(long minorUnits, string currency)
    {
        var exponent = StripeExponent(currency);
        return exponent == 0 ? minorUnits : minorUnits / MinorScale(exponent);
    }

    private static decimal MinorScale(int exponent) =>
        decimal.Parse("1".PadRight(exponent + 1, '0'), System.Globalization.CultureInfo.InvariantCulture);

    private static Arbitrary<string> CurrencyArb(IReadOnlyList<string> currencies) =>
        Arb.From(Gen.Elements(currencies.ToArray()));

    private static Arbitrary<long> MinorUnitsArb() =>
        Arb.From(Gen.Choose(0, MaxMinorUnits).Select(value => (long)value));

    // ---------- Property 1: the round-trip is lossless for every currency/amount pair ----------

    /// <summary>
    ///     amount → minor units → gateway (long) → minor units back → amount must be the
    ///     identity for every catalog currency and every exponent-aligned amount.
    /// </summary>
    [Property(MaxTest = 500)]
    public Property AmountToMinorUnitsAndBack_IsLosslessForEveryCatalogCurrency()
    {
        var currencies = AllCurrencies();

        return Prop.ForAll(
            CurrencyArb(currencies),
            MinorUnitsArb(),
            (currency, minorUnits) =>
            {
                // ISK/UGX bill in whole units through a two-decimal representation: only
                // minor-unit totals that are whole majors are legal inputs there.
                var alignedMinorUnits = currency is "ISK" or "UGX" ? minorUnits * 100 : minorUnits;
                var amount = MajorFromMinor(alignedMinorUnits, currency);

                var sentToGateway = StripeAmountConverter.ToStripeAmount(amount, currency);
                var roundTripped = StripeAmountConverter.FromStripeAmount(sentToGateway, currency);

                return roundTripped.Equals(amount)
                    .ToProperty()
                    .Label($"currency {currency}: {amount} -> {sentToGateway} -> {roundTripped}");
            });
    }

    // ---------- Property 2: zero-decimal (JPY-class) currencies never multiply by 100 ----------

    [Property(MaxTest = 300)]
    public Property ZeroDecimalCurrencies_NeverScaleByOneHundred()
    {
        var zeroDecimal = AllCurrencies()
            .Where(code => StripeExponent(code) == 0)
            .ToArray();

        var jpyClassPinned = zeroDecimal.Contains("JPY")
            ? true.ToProperty()
            : false.ToProperty().Label("JPY must be a zero-decimal currency");

        return jpyClassPinned.And(Prop.ForAll(
            CurrencyArb(zeroDecimal),
            MinorUnitsArb(),
            (currency, wholeUnits) =>
            {
                var amount = (decimal)wholeUnits;
                return StripeAmountConverter.ToStripeAmount(amount, currency).Equals(wholeUnits)
                    .ToProperty()
                    .Label($"{amount} {currency} must map to {wholeUnits} minor units, not {wholeUnits * 100L}");
            }));
    }

    // ---------- Property 3: three-decimal (KWD-class) currencies scale by 1000 ----------

    [Property(MaxTest = 300)]
    public Property ThreeDecimalCurrencies_ScaleByOneThousand()
    {
        var threeDecimal = AllCurrencies()
            .Where(code => Iso4217CurrencyCatalog.GetMinorUnitExponent(code) == 3)
            .ToArray();

        var kwdClassPinned = threeDecimal.Contains("KWD")
            ? true.ToProperty()
            : false.ToProperty().Label("KWD must be a three-decimal currency");

        return kwdClassPinned.And(Prop.ForAll(
            CurrencyArb(threeDecimal),
            MinorUnitsArb(),
            (currency, fils) =>
            {
                var amount = fils / 1000m;
                return StripeAmountConverter.ToStripeAmount(amount, currency).Equals(fils)
                    .ToProperty()
                    .Label($"{amount} {currency} must map to {fils} minor units (x1000)");
            }));
    }

    // ---------- Property 4: the conversion is currency-dependent, never a silent two-decimal default ----------

    /// <summary>
    ///     For every currency whose Stripe exponent differs from the historical USD
    ///     hardcode's exponent (2), converting with the payment's own currency MUST produce
    ///     a different minor-unit value than the default would have — pinning that the
    ///     payment's own currency drives the scale (#1266 N1), never a default.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property ConversionWithTheOwnCurrency_NeverMatchesATwoDecimalDefault()
    {
        var nonDefaultExponent = AllCurrencies()
            .Where(code => StripeExponent(code) != 2)
            .ToArray();

        return Prop.ForAll(
            CurrencyArb(nonDefaultExponent),
            Arb.From(Gen.Choose(1, MaxMinorUnits).Select(value => (long)value)),
            (currency, minorUnits) =>
            {
                var amount = MajorFromMinor(minorUnits, currency);
                var ownCurrencyUnits = StripeAmountConverter.ToStripeAmount(amount, currency);
                var defaultUnits = StripeAmountConverter.ToStripeAmount(amount, "USD");

                return (!ownCurrencyUnits.Equals(defaultUnits))
                    .ToProperty()
                    .Label($"{amount} {currency}: own-currency conversion {ownCurrencyUnits} collapsed to the USD default {defaultUnits}");
            });
    }

    // ---------- Property 5: the refund leg of StripePaymentService uses the payment's currency ----------

    /// <summary>
    ///     The refund request path (post-#1266) resolves the payment's own currency before
    ///     the gateway call: the exact minor-unit value sent to Stripe follows that
    ///     currency's exponent for every currency/amount pair, and the gateway's echo of
    ///     the minor-unit amount converts back to the exact requested decimal amount
    ///     (amount → minor units → gateway → minor units back → amount, lossless).
    /// </summary>
    [Property(MaxTest = 300)]
    public Property RefundLeg_ConvertsWithThePaymentsOwnCurrency_AndRoundTripsLosslessly()
    {
        var currencies = AllCurrencies()
            .Where(code => code is not ("ISK" or "UGX" or "MGA")) // specials demand whole-unit amounts (example-tested)
            .ToArray();

        return Prop.ForAll(
            CurrencyArb(currencies),
            Arb.From(Gen.Choose(1, MaxMinorUnits).Select(value => (long)value)),
            (currency, minorUnits) =>
            {
                var refundAmount = MajorFromMinor(minorUnits, currency);
                RefundCreateOptions? captured = null;
                var service = Service(
                    currency,
                    createOptions =>
                    {
                        captured = createOptions;
                        return createOptions.Amount ?? 0;
                    });

                var result = service.ProcessRefundAsync(Request(refundAmount, currency))
                    .GetAwaiter().GetResult();

                var sentMinorUnits = captured?.Amount;
                var expectedMinorUnits = StripeAmountConverter.ToStripeAmount(refundAmount, currency);
                var echoedAmount = result.AmountRefunded;

                return sentMinorUnits.Equals(expectedMinorUnits)
                    .ToProperty()
                    .Label($"refunding {refundAmount} {currency}: sent {sentMinorUnits}, expected {expectedMinorUnits}")
                    .And(echoedAmount.Equals(refundAmount)
                        .ToProperty()
                        .Label($"gateway echo of {expectedMinorUnits} converted back to {echoedAmount}, expected {refundAmount}"));
            });
    }

    private static StripePaymentService Service(
        string currency,
        Func<RefundCreateOptions, long> echoAmount) => new(
        Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = false }),
        NullLogger<StripePaymentService>.Instance,
        createPaymentIntentAsync: (_, _, _) => Task.FromResult(new PaymentIntent { Id = "pi_unused" }),
        createRefundAsync: (createOptions, _, _) => Task.FromResult(Refund(currency, echoAmount(createOptions))));

    private static GatewayRefundRequest Request(decimal amount, string? currency) => new(
        IdempotencyKey: "refund_property_test",
        OriginalTransactionId: "pi_refund_property",
        Amount: amount,
        Reason: "requested_by_customer",
        ProviderId: "stripe",
        Currency: currency);

    private static Stripe.Refund Refund(string currency, long amount) => new()
    {
        Id = "re_refund_property",
        Status = "succeeded",
        Currency = currency,
        Amount = amount
    };
}
