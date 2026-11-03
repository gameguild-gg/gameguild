using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Issue #1202 (payments gaps N1): the refund leg must convert the amount to Stripe's
///     minor units using the PAYMENT's actual currency — the same conversion the charge leg
///     uses — instead of a hardcoded two-decimal currency that over-refunds zero-decimal
///     currencies (JPY: 100x) and under-refunds three-decimal ones (KWD/BHD).
/// </summary>
public sealed class StripeRefundCurrencyConversionTests
{
    [Theory]
    [InlineData("USD", 50.00, 5000L)]     // two-decimal: 50.00 USD -> 5000 cents
    [InlineData("EUR", 12.34, 1234L)]     // two-decimal
    [InlineData("JPY", 9000, 9000L)]      // zero-decimal: 9,000 JPY -> 9000 (was 900,000 with the USD hardcode)
    [InlineData("KWD", 12.345, 12345L)]   // three-decimal: 12.345 KWD -> 12345 fils
    [InlineData("BHD", 1.5, 1500L)]       // three-decimal
    public async Task ProcessRefundAsync_ScalesTheAmountByTheRefundCurrencysMinorUnitExponent(
        string currency,
        decimal refundAmount,
        long expectedMinorUnits)
    {
        RefundCreateOptions? captured = null;
        var service = Service((createOptions, _, _) =>
        {
            captured = createOptions;
            return Task.FromResult(Refund(currency, createOptions.Amount ?? 0));
        });

        var result = await service.ProcessRefundAsync(Request(refundAmount, currency));

        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(expectedMinorUnits,
            "the exact minor-unit value sent to the gateway must follow the payment currency's exponent");
        captured.PaymentIntent.Should().Be("pi_refund_currency");
    }

    [Fact]
    public async Task ProcessRefundAsync_WithoutAnExplicitAmountLeavesTheMinorUnitAmountUnset()
    {
        RefundCreateOptions? captured = null;
        var service = Service((createOptions, _, _) =>
        {
            captured = createOptions;
            return Task.FromResult(Refund("USD", 5000));
        });

        var result = await service.ProcessRefundAsync(Request(amount: null, currency: null));

        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Amount.Should().BeNull(
            "a null amount asks Stripe to refund the remaining balance in its own minor units");
    }

    [Fact]
    public async Task ProcessRefundAsync_FailsClosedWhenTheCurrencyIsMissing()
    {
        var service = Service((_, _, _) => Task.FromResult(Refund("USD", 1)));

        await FluentActions.Awaiting(() => service.ProcessRefundAsync(Request(10m, currency: null)))
            .Should().ThrowAsync<InvalidOperationException>(
                "converting an explicit amount without the payment's currency would silently mis-scale it");
    }

    private static StripePaymentService Service(
        Func<RefundCreateOptions, RequestOptions, CancellationToken, Task<Stripe.Refund>> createRefund) => new(
        Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = false }),
        NullLogger<StripePaymentService>.Instance,
        createPaymentIntentAsync: (_, _, _) => Task.FromResult(new PaymentIntent { Id = "pi_unused" }),
        createRefundAsync: createRefund);

    private static GatewayRefundRequest Request(decimal? amount, string? currency) => new(
        IdempotencyKey: "refund_currency_test",
        OriginalTransactionId: "pi_refund_currency",
        Amount: amount,
        Reason: "requested_by_customer",
        ProviderId: "stripe",
        Currency: currency);

    private static Stripe.Refund Refund(string currency, long amount) => new()
    {
        Id = "re_refund_currency",
        Status = "succeeded",
        Currency = currency,
        Amount = amount
    };
}
