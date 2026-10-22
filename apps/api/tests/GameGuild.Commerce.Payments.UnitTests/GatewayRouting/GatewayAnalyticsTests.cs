using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.GatewayRouting;

/// <summary>
///     Expanded gateway analytics (issue #413 analytics remainders): the error-rate-by-type
///     breakdown groups failed payments by gateway error code, the cost analysis prices the
///     succeeded volume through the configured per-gateway fee schedule (unknown schedules
///     stay null, never zero), and the geographic breakdown aggregates per billing country.
/// </summary>
public class GatewayAnalyticsTests
{
    private readonly Mock<IPaymentRepository> _repository = new();

    private GatewayMetricsService CreateService(GatewayRoutingOptions? routing = null) =>
        new(_repository.Object, Options.Create(routing ?? new GatewayRoutingOptions()));

    private static Payment NewPayment(
        string provider,
        PaymentStatus status,
        DateTime createdAt,
        string? errorCode = null,
        string? billingCountry = null,
        decimal amount = 25m)
    {
        var payment = Payment.Create(
            tenantId: Guid.NewGuid(),
            amount: amount,
            currency: "USD",
            idempotencyKey: $"idem-{Guid.NewGuid():N}",
            provider: provider);
        payment.CreatedAt = createdAt;

        if (status is PaymentStatus.Succeeded or PaymentStatus.Failed)
        {
            payment.MarkAsProcessing();
            if (status == PaymentStatus.Succeeded)
            {
                payment.MarkAsSucceeded("ext-" + payment.Id.ToString("N"));
            }
            else
            {
                payment.MarkAsFailed("test failure", errorCode);
            }
        }

        payment.RecordBillingCountry(billingCountry);
        return payment;
    }

    private void SetupRepository(params Payment[] payments) =>
        _repository
            .Setup(repository => repository.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime start, DateTime end, Guid? _, CancellationToken _) =>
                payments.Where(payment => payment.CreatedAt >= start && payment.CreatedAt <= end).ToList());

    [Fact]
    public async Task ErrorBreakdown_GroupsFailedPaymentsByErrorCode()
    {
        var now = SystemClock.UtcNow;
        SetupRepository(
            NewPayment("stripe", PaymentStatus.Failed, now.AddHours(-1), errorCode: "card_declined"),
            NewPayment("stripe", PaymentStatus.Failed, now.AddHours(-2), errorCode: "card_declined"),
            NewPayment("stripe", PaymentStatus.Failed, now.AddHours(-3), errorCode: "insufficient_funds"),
            NewPayment("stripe", PaymentStatus.Failed, now.AddHours(-4)),
            NewPayment("stripe", PaymentStatus.Succeeded, now.AddMinutes(-30)));

        var report = await CreateService().GetAnalyticsAsync();

        var window = report.ErrorBreakdown.Where(row => row.Window == "24h" && row.ProviderId == "stripe").ToList();
        window.Should().HaveCount(3);

        var declined = window.Single(row => row.ErrorCode == "card_declined");
        declined.Count.Should().Be(2);
        declined.ShareOfFailures.Should().BeApproximately(0.5, 0.0001);

        var unclassified = window.Single(row => row.ErrorCode == "(unclassified)");
        unclassified.Count.Should().Be(1);

        report.ErrorBreakdown.Should().OnlyContain(row => row.Window == "24h",
            "every payment is recent, so only the 24h window has failures");
    }

    [Fact]
    public async Task CostAnalysis_PricesVolumeThroughTheConfiguredFeeSchedule()
    {
        var now = SystemClock.UtcNow;
        SetupRepository(
            NewPayment("stripe", PaymentStatus.Succeeded, now.AddDays(-1), amount: 100m),
            NewPayment("stripe", PaymentStatus.Succeeded, now.AddDays(-2), amount: 50m),
            NewPayment("stripe", PaymentStatus.Failed, now.AddDays(-3), amount: 10m), // failed volume is not priced
            NewPayment("adyen", PaymentStatus.Succeeded, now.AddDays(-1), amount: 70m)); // no fee schedule

        var routing = new GatewayRoutingOptions
        {
            Gateways =
            [
                new GatewayRouteEntry { ProviderId = "stripe", FeeFixedAmount = 0.30m, FeePercentBasisPoints = 290 },
                new GatewayRouteEntry { ProviderId = "adyen" }
            ]
        };

        var report = await CreateService(routing).GetAnalyticsAsync();

        var stripe = report.CostAnalysis.Single(row => row.ProviderId == "stripe");
        stripe.SucceededCount.Should().Be(2);
        stripe.SucceededVolume.Should().Be(150m);
        stripe.EstimatedFee.Should().Be(0.30m * 2 + 150m * 290 / 10000m);

        var adyen = report.CostAnalysis.Single(row => row.ProviderId == "adyen");
        adyen.SucceededVolume.Should().Be(70m);
        adyen.EstimatedFee.Should().BeNull("an unconfigured fee schedule is unknown cost, never zero");
    }

    [Fact]
    public async Task GeographicBreakdown_AggregatesPerBillingCountry()
    {
        var now = SystemClock.UtcNow;
        SetupRepository(
            NewPayment("adyen", PaymentStatus.Succeeded, now.AddHours(-1), billingCountry: "DE"),
            NewPayment("adyen", PaymentStatus.Failed, now.AddHours(-2), billingCountry: "DE"),
            NewPayment("adyen", PaymentStatus.Succeeded, now.AddHours(-3), billingCountry: "FR"),
            NewPayment("adyen", PaymentStatus.Succeeded, now.AddHours(-4))); // no country recorded

        var report = await CreateService().GetAnalyticsAsync();

        var window = report.GeographicBreakdown.Where(row => row.Window == "24h" && row.ProviderId == "adyen").ToList();

        var germany = window.Single(row => row.Country == "DE");
        germany.Attempts.Should().Be(2);
        germany.Succeeded.Should().Be(1);
        germany.SuccessRate.Should().BeApproximately(0.5, 0.0001);

        window.Should().Contain(row => row.Country == "FR" && row.Attempts == 1 && row.SuccessRate == 1.0);
        window.Should().Contain(row => row.Country == "(UNKNOWN)" && row.Attempts == 1);
    }

    [Fact]
    public async Task Analytics_IncludeTheBaseWindowAggregates()
    {
        var now = SystemClock.UtcNow;
        SetupRepository(NewPayment("paypal", PaymentStatus.Succeeded, now.AddHours(-1)));

        var report = await CreateService().GetAnalyticsAsync();

        report.Windows.Should().Contain(row =>
            row.ProviderId == "paypal" && row.Window == "24h" && row.Succeeded == 1);
        report.GeneratedAtUtc.Should().BeOnOrAfter(now.AddSeconds(-5));
    }
}
