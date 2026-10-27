using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.GatewayRouting;

/// <summary>
///     Selection-strategy matrix for the config-driven gateway selector (issue #413 routing
///     expansion): lowest cost, best performance, lowest error rate, geography affinity,
///     round-robin and weighted round-robin — plus the fail-safe degradation to priority
///     order when the strategy's collaborators are missing.
/// </summary>
public class GatewaySelectionStrategyTests
{
    private static ConfigPaymentGatewaySelector CreateSelector(
        IReadOnlyList<IPaymentGateway> gateways,
        GatewayRoutingOptions routing,
        IGatewayMetricsService? metrics = null,
        IGatewayRotationState? rotation = null) =>
        new(
            new FakeGatewayRegistry([.. gateways]),
            Options.Create(routing),
            NullLogger<ConfigPaymentGatewaySelector>.Instance,
            metrics,
            rotation);

    private static GatewayRoutingOptions Routing(
        GatewaySelectionStrategy strategy,
        params GatewayRouteEntry[] entries) =>
        new() { SelectionStrategy = strategy, Gateways = [.. entries] };

    private sealed class FakeMetricsService : IGatewayMetricsService
    {
        public List<GatewayWindowMetrics> Windows { get; set; } = [];

        public Task<IReadOnlyList<GatewayWindowMetrics>> GetWindowMetricsAsync(
            Guid? tenantId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GatewayWindowMetrics>>(Windows);

        public Task<GatewayAnalyticsReport> GetAnalyticsAsync(
            Guid? tenantId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GatewayAnalyticsReport(
                SystemClock.UtcNow,
                Windows,
                [],
                [],
                []));
    }

    private static GatewayWindowMetrics Window(
        string providerId,
        int attempts,
        int succeeded,
        int failed,
        string window = "24h") =>
        new(
            ProviderId: providerId,
            Window: window,
            SinceUtc: SystemClock.UtcNow.AddHours(-24),
            Attempts: attempts,
            Succeeded: succeeded,
            Failed: failed,
            Unresolved: attempts - succeeded - failed,
            SuccessRate: attempts == 0 ? 0.0 : (double)succeeded / attempts,
            AverageProcessingLatencyMs: null);

    [Fact]
    public void LowestCost_OrdersByEstimatedFee_AndSortsUnconfiguredGatewaysLast()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var adyen = new FakePaymentGateway("adyen");
        var routing = Routing(
            GatewaySelectionStrategy.LowestCost,
            new GatewayRouteEntry { ProviderId = "stripe", FeeFixedAmount = 0.30m, FeePercentBasisPoints = 290 },
            new GatewayRouteEntry { ProviderId = "paypal", FeeFixedAmount = 0.49m, FeePercentBasisPoints = 340 },
            new GatewayRouteEntry { ProviderId = "adyen", Priority = 1 }); // no fee schedule

        var selector = CreateSelector([stripe, payPal, adyen], routing);

        var candidates = selector.Select(new GatewayRouteContext("USD", Amount: 100m));

        // stripe: 0.30 + 2.90 = 3.20; paypal: 0.49 + 3.40 = 3.89; adyen: unknown -> last.
        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("stripe", "paypal", "adyen");
    }

    [Fact]
    public void LowestCost_PutsConfiguredGatewayAheadOfCheaperPriorityDefault()
    {
        var stripe = new FakePaymentGateway("stripe"); // default priority 100
        var adyen = new FakePaymentGateway("adyen"); // default priority 500
        var routing = Routing(
            GatewaySelectionStrategy.LowestCost,
            new GatewayRouteEntry { ProviderId = "adyen", FeeFixedAmount = 0.01m, FeePercentBasisPoints = 10 },
            new GatewayRouteEntry { ProviderId = "stripe", FeeFixedAmount = 0.30m, FeePercentBasisPoints = 290 });

        var selector = CreateSelector([stripe, adyen], routing);

        var candidates = selector.Select(new GatewayRouteContext("USD", Amount: 50m));

        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("adyen", "stripe");
    }

    [Fact]
    public void BestPerformance_OrdersBySuccessRate_AndSortsNoEvidenceLast()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var adyen = new FakePaymentGateway("adyen");
        var metrics = new FakeMetricsService
        {
            Windows =
            [
                Window("stripe", attempts: 10, succeeded: 9, failed: 1), // 0.9
                Window("paypal", attempts: 8, succeeded: 4, failed: 4), // 0.5
                Window("adyen", attempts: 0, succeeded: 0, failed: 0) // no evidence
            ]
        };
        var selector = CreateSelector([stripe, payPal, adyen], Routing(GatewaySelectionStrategy.BestPerformance), metrics);

        var candidates = selector.Select(new GatewayRouteContext("USD"));

        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("stripe", "paypal", "adyen");
    }

    [Fact]
    public void BestPerformance_FallsBackToPriorityOrder_WhenMetricsAreUnavailable()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var selector = CreateSelector([payPal, stripe], Routing(GatewaySelectionStrategy.BestPerformance));

        var candidates = selector.Select(new GatewayRouteContext("USD"));

        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal(new[] { "stripe", "paypal" }, "priority order is the fail-safe disposition");
    }

    [Fact]
    public void LowestErrorRate_OrdersByErrorRateAscending_AndSortsNoEvidenceLast()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var adyen = new FakePaymentGateway("adyen");
        var metrics = new FakeMetricsService
        {
            Windows =
            [
                Window("stripe", attempts: 10, succeeded: 5, failed: 5), // 0.5 error
                Window("paypal", attempts: 10, succeeded: 9, failed: 1), // 0.1 error
                Window("adyen", attempts: 0, succeeded: 0, failed: 0) // no evidence
            ]
        };
        var selector = CreateSelector([stripe, payPal, adyen], Routing(GatewaySelectionStrategy.LowestErrorRate), metrics);

        var candidates = selector.Select(new GatewayRouteContext("USD"));

        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("paypal", "stripe", "adyen");
    }

    [Fact]
    public void Geography_PrefersGatewaysThatExplicitlyCoverTheBillingCountry()
    {
        var stripe = new FakePaymentGateway("stripe"); // no country restriction
        var adyen = new FakePaymentGateway("adyen");
        var routing = Routing(
            GatewaySelectionStrategy.Geography,
            new GatewayRouteEntry { ProviderId = "stripe", Priority = 1 },
            new GatewayRouteEntry { ProviderId = "adyen", Priority = 2, SupportedCountries = ["DE", "FR"] });

        var selector = CreateSelector([stripe, adyen], routing);

        var deCandidates = selector.Select(new GatewayRouteContext("EUR", Country: "DE"));
        deCandidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("adyen", "stripe");

        // Without a country in the context the explicit-region gateway cannot be preferred;
        // priority order decides.
        var unspecified = selector.Select(new GatewayRouteContext("EUR"));
        unspecified.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("stripe", "adyen");
    }

    [Fact]
    public void RoundRobin_RotatesThroughTheEligibleCandidates()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var adyen = new FakePaymentGateway("adyen");
        var rotation = new InMemoryGatewayRotationState();
        var selector = CreateSelector(
            [stripe, payPal, adyen],
            Routing(GatewaySelectionStrategy.RoundRobin),
            rotation: rotation);

        var first = selector.Select(new GatewayRouteContext("USD"));
        var second = selector.Select(new GatewayRouteContext("USD"));
        var third = selector.Select(new GatewayRouteContext("USD"));
        var fourth = selector.Select(new GatewayRouteContext("USD"));

        first.Select(c => c.Gateway.ProviderId).Should().Equal("paypal", "adyen", "stripe");
        second.Select(c => c.Gateway.ProviderId).Should().Equal("adyen", "stripe", "paypal");
        third.Select(c => c.Gateway.ProviderId).Should().Equal("stripe", "paypal", "adyen");
        fourth.Select(c => c.Gateway.ProviderId).Should().Equal("paypal", "adyen", "stripe");
    }

    [Fact]
    public void WeightedRoundRobin_DistributesSelectionsByConfiguredWeight()
    {
        var stripe = new FakePaymentGateway("stripe");
        var adyen = new FakePaymentGateway("adyen");
        var routing = Routing(
            GatewaySelectionStrategy.WeightedRoundRobin,
            new GatewayRouteEntry { ProviderId = "stripe", Weight = 3 },
            new GatewayRouteEntry { ProviderId = "adyen", Weight = 1 });
        var selector = CreateSelector(
            [stripe, adyen],
            routing,
            rotation: new InMemoryGatewayRotationState());

        var selections = Enumerable.Range(0, 8)
            .Select(_ => selector.SelectPrimary(new GatewayRouteContext("USD")).Gateway.ProviderId)
            .ToList();

        selections.Count(provider => provider == "stripe").Should().Be(6);
        selections.Count(provider => provider == "adyen").Should().Be(2);
    }

    [Fact]
    public void RoundRobin_FallsBackToPriorityOrder_WithoutRotationState()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var selector = CreateSelector([payPal, stripe], Routing(GatewaySelectionStrategy.RoundRobin));

        var candidates = selector.Select(new GatewayRouteContext("USD"));

        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("stripe", "paypal");
    }

    [Fact]
    public void Strategies_PreserveEligibility_StillRejectingFilteredGateways()
    {
        var stripe = new FakePaymentGateway("stripe");
        var adyen = new FakePaymentGateway("adyen");
        var routing = Routing(
            GatewaySelectionStrategy.RoundRobin,
            new GatewayRouteEntry { ProviderId = "stripe", SupportedCurrencies = ["EUR"] });

        var selector = CreateSelector([stripe, adyen], routing, rotation: new InMemoryGatewayRotationState());

        var candidates = selector.Select(new GatewayRouteContext("USD"));

        candidates.Should().ContainSingle();
        candidates[0].Gateway.ProviderId.Should().Be("adyen");
    }

    [Fact]
    public void Priority_Strategy_KeepsTheOriginalOrderingBehaviour()
    {
        var stripe = new FakePaymentGateway("stripe");
        var payPal = new FakePaymentGateway("paypal");
        var selector = CreateSelector([payPal, stripe], Routing(GatewaySelectionStrategy.Priority));

        var candidates = selector.Select(new GatewayRouteContext("USD"));

        candidates.Select(candidate => candidate.Gateway.ProviderId)
            .Should().Equal("stripe", "paypal");
    }

    [Fact]
    public void Options_ValidateFeeScheduleComponentsTogether()
    {
        var validator = new GatewayRoutingOptionsValidator();
        var options = new GatewayRoutingOptions
        {
            Gateways = [new GatewayRouteEntry
            {
                ProviderId = "stripe",
                FeeFixedAmount = 0.30m,
                FeePercentBasisPoints = null
            }]
        };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(entry => entry.Contains("together"));
    }

    [Theory]
    [InlineData(GatewaySelectionStrategy.Priority)]
    [InlineData(GatewaySelectionStrategy.LowestCost)]
    [InlineData(GatewaySelectionStrategy.BestPerformance)]
    [InlineData(GatewaySelectionStrategy.LowestErrorRate)]
    [InlineData(GatewaySelectionStrategy.Geography)]
    [InlineData(GatewaySelectionStrategy.RoundRobin)]
    [InlineData(GatewaySelectionStrategy.WeightedRoundRobin)]
    public void Options_AcceptEveryDefinedStrategy(GatewaySelectionStrategy strategy)
    {
        var validator = new GatewayRoutingOptionsValidator();
        var options = new GatewayRoutingOptions
        {
            SelectionStrategy = strategy,
            Gateways =
            [
                new GatewayRouteEntry
                {
                    ProviderId = "stripe",
                    Weight = 2,
                    FeeFixedAmount = 0.30m,
                    FeePercentBasisPoints = 290
                }
            ]
        };

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }
}
