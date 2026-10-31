using FluentAssertions;
using GameGuild;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Issue #409 remainder signals: high-risk card BIN ranges, server-known AVS/CVV
///     outcomes, and GeoIP anonymizer/timezone traits.
/// </summary>
public class AdditionalFraudSignalProviderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PaymentId = Guid.NewGuid();
    private static readonly Guid SubscriptionId = Guid.NewGuid();

    // ───────────────────────── BIN range ─────────────────────────

    [Fact]
    public async Task BinRange_Should_Fire_On_Configured_Prefix()
    {
        var options = Options(new FraudAssessmentOptions { HighRiskCardBins = ["530000"] });
        var provider = new BinRangeFraudSignalProvider(NullLogger<BinRangeFraudSignalProvider>.Instance);

        var signals = await provider.EvaluateAsync(Context(cardBin: "530092"), options);

        signals.Should().ContainSingle(signal => signal.SignalType == "CardBin.HighRiskRange"
                                                 && signal.Weight == options.CardBinWeight);
    }

    [Fact]
    public async Task BinRange_Should_Fire_On_Range_Entry_And_Bound_The_Weight()
    {
        var options = Options(new FraudAssessmentOptions
        {
            HighRiskCardBins = ["400000-400050"],
            CardBinWeight = 80 // above MaxSignalScore (50): capped per signal
        });
        var provider = new BinRangeFraudSignalProvider(NullLogger<BinRangeFraudSignalProvider>.Instance);

        var signals = await provider.EvaluateAsync(Context(cardBin: "4000123456"), options);

        signals.Should().ContainSingle(signal => signal.Weight == options.MaxSignalScore);
    }

    [Fact]
    public async Task BinRange_Should_Not_Fire_Outside_The_Range_Or_Without_A_List()
    {
        var provider = new BinRangeFraudSignalProvider(NullLogger<BinRangeFraudSignalProvider>.Instance);

        var outside = await provider.EvaluateAsync(
            Context(cardBin: "400051"), Options(new FraudAssessmentOptions { HighRiskCardBins = ["400000-400050"] }));
        var noList = await provider.EvaluateAsync(Context(cardBin: "400000"), Options(new FraudAssessmentOptions()));

        outside.Should().BeEmpty();
        noList.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345")]
    [InlineData("1234567890")]
    [InlineData("12345X")]
    public async Task BinRange_Should_Reject_Malformed_Bins(string? cardBin)
    {
        var provider = new BinRangeFraudSignalProvider(NullLogger<BinRangeFraudSignalProvider>.Instance);

        var signals = await provider.EvaluateAsync(
            Context(cardBin: cardBin),
            Options(new FraudAssessmentOptions { HighRiskCardBins = ["123456"] }));

        signals.Should().BeEmpty();
    }

    [Fact]
    public void BinRange_MatchEntry_Should_Ignore_Malformed_Entries_Instead_Of_Blocking_Everything()
    {
        // Fail-closed to NO signal on garbage configuration entries.
        BinRangeFraudSignalProvider.MatchEntry("123456", ["not-a-bin", "1-2", "400000-300000", ""])
            .Should().BeNull();
    }

    // ───────────────────────── AVS / CVV ─────────────────────────

    [Fact]
    public async Task AvsCvv_Should_Fire_Strongly_On_Mismatch()
    {
        var options = Options(new FraudAssessmentOptions());
        var provider = new AvsCvvFraudSignalProvider(NullLogger<AvsCvvFraudSignalProvider>.Instance);

        var signals = await provider.EvaluateAsync(
            Context(avs: FraudVerificationOutcome.Mismatch, cvv: FraudVerificationOutcome.Mismatch),
            options);

        signals.Should().HaveCount(2);
        signals.Should().OnlyContain(signal => signal.Weight == options.AvsCvvMismatchWeight);
        signals.Select(signal => signal.SignalType).Should().BeEquivalentTo(["Avs.Outcome", "Cvv.Outcome"]);
    }

    [Fact]
    public async Task AvsCvv_Should_Fire_Weakly_On_NotChecked_And_Not_At_All_On_Match_Or_Absent()
    {
        var options = Options(new FraudAssessmentOptions());
        var provider = new AvsCvvFraudSignalProvider(NullLogger<AvsCvvFraudSignalProvider>.Instance);

        var notChecked = await provider.EvaluateAsync(
            Context(avs: FraudVerificationOutcome.NotChecked), options);
        var match = await provider.EvaluateAsync(
            Context(avs: FraudVerificationOutcome.Match, cvv: FraudVerificationOutcome.Match), options);
        var absent = await provider.EvaluateAsync(Context(), options);

        notChecked.Should().ContainSingle().Which.Weight.Should().Be(options.AvsCvvUncheckedWeight);
        match.Should().BeEmpty("a matching verification is not fraud evidence");
        absent.Should().BeEmpty("absent outcomes are not evidence either");
    }

    // ───────────────────────── GeoIP ─────────────────────────

    [Fact]
    public async Task GeoIp_Should_Fire_On_Every_Anonymizer_Trait()
    {
        var options = Options(new FraudAssessmentOptions());
        foreach (var traits in new[]
                 {
                     new GeolocationResult { IsAnonymousVpn = true },
                     new GeolocationResult { IsPublicProxy = true },
                     new GeolocationResult { IsTorExitNode = true },
                     new GeolocationResult { IsHostingProvider = true }
                 })
        {
            var provider = CreateProvider(traits);

            var signals = await provider.EvaluateAsync(Context(ip: "203.0.113.10"), options);

            signals.Should().ContainSingle(signal => signal.SignalType == GeoIpFraudSignalProvider.Signals.GeoIpAnonymizer,
                $"the {traits} trait must fire the anonymizer signal");
        }
    }

    [Fact]
    public async Task GeoIp_Should_Not_Fire_When_The_Database_Knows_The_Address_Is_Clean()
    {
        var provider = CreateProvider(new GeolocationResult
        {
            IsAnonymousVpn = false,
            IsPublicProxy = false,
            IsTorExitNode = false,
            IsHostingProvider = false,
            Timezone = "Europe/Lisbon"
        });

        var signals = await provider.EvaluateAsync(
            Context(ip: "203.0.113.10", timezone: "Europe/Lisbon"),
            Options(new FraudAssessmentOptions()));

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task GeoIp_Should_Fire_On_Timezone_Mismatch()
    {
        var provider = CreateProvider(new GeolocationResult { Timezone = "Europe/Lisbon" });

        var signals = await provider.EvaluateAsync(
            Context(ip: "203.0.113.10", timezone: "America/New_York"),
            Options(new FraudAssessmentOptions()));

        signals.Should().ContainSingle(signal => signal.SignalType == GeoIpFraudSignalProvider.Signals.GeoIpTimezoneMismatch);
    }

    [Fact]
    public async Task GeoIp_Should_Treat_Fixed_Offset_Claims_As_Equivalent_To_Named_Zones()
    {
        var provider = CreateProvider(new GeolocationResult { Timezone = "Etc/GMT-1" });

        var signals = await provider.EvaluateAsync(
            Context(ip: "203.0.113.10", timezone: "UTC+01:00"),
            Options(new FraudAssessmentOptions()));

        signals.Should().BeEmpty("UTC+01:00 is Etc/GMT-1; an offset claim is not a mismatch");
    }

    [Fact]
    public async Task GeoIp_Should_Stay_Silent_When_Resolution_Returns_Null()
    {
        var provider = CreateProvider(null);

        var signals = await provider.EvaluateAsync(
            Context(ip: "203.0.113.10", timezone: "Mars/Olympus"),
            Options(new FraudAssessmentOptions()));

        signals.Should().BeEmpty("no resolution (no database, private IP, miss) means no evidence");
    }

    [Fact]
    public async Task GeoIp_Should_Stay_Silent_Without_A_Source_Ip()
    {
        var provider = CreateProvider(new GeolocationResult { Timezone = "Europe/Lisbon" });

        var signals = await provider.EvaluateAsync(
            Context(timezone: "America/New_York"),
            Options(new FraudAssessmentOptions()));

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task GeoIp_Should_Swallow_A_Throwing_Provider()
    {
        var geolocation = new Mock<IGeolocationProvider>();
        geolocation
            .Setup(g => g.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broken provider"));
        var provider = new GeoIpFraudSignalProvider(
            geolocation.Object,
            NullLogger<GeoIpFraudSignalProvider>.Instance);

        var signals = await provider.EvaluateAsync(
            Context(ip: "203.0.113.10"),
            Options(new FraudAssessmentOptions()));

        signals.Should().BeEmpty();
    }

    // ───────────────────────── helpers ─────────────────────────

    private static FraudAssessmentOptions Options(FraudAssessmentOptions options) => options;

    private static FraudSignalContext Context(
        string? cardBin = null,
        FraudVerificationOutcome? avs = null,
        FraudVerificationOutcome? cvv = null,
        string? ip = null,
        string? timezone = null) => new(
        TenantId,
        PaymentId,
        SubscriptionId,
        ExternalCustomerId: null,
        PaymentMethodId: "pm_test",
        Amount: 25m,
        Currency: "USD",
        Country: null,
        SourceIpAddress: ip,
        DeviceFingerprint: null,
        PayerEmail: null,
        CardBin: cardBin,
        AvsOutcome: avs,
        CvvOutcome: cvv,
        ClientTimezone: timezone);

    private static GeoIpFraudSignalProvider CreateProvider(GeolocationResult? result)
    {
        var geolocation = new Mock<IGeolocationProvider>();
        geolocation
            .Setup(g => g.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return new GeoIpFraudSignalProvider(
            geolocation.Object,
            NullLogger<GeoIpFraudSignalProvider>.Instance);
    }
}
