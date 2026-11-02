using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Unit tests for the high-risk-country fraud signal (issue #409: flag transactions from
///     countries with elevated fraud rates).
/// </summary>
public class HighRiskCountryFraudSignalProviderTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _paymentId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();

    private HighRiskCountryFraudSignalProvider CreateProvider() =>
        new(NullLogger<HighRiskCountryFraudSignalProvider>.Instance);

    private FraudSignalContext Context(string? country) =>
        new(
            _tenantId,
            _paymentId,
            _subscriptionId,
            ExternalCustomerId: null,
            PaymentMethodId: null,
            250m,
            "USD",
            BillingCycleNumber: 1,
            Country: country,
            SourceIpAddress: null,
            DeviceFingerprint: null,
            PayerEmail: null);

    private static FraudAssessmentOptions Options(params string[] countries) =>
        new() { HighRiskCountries = countries };

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Listed_Country_Case_Insensitively()
    {
        var signals = await CreateProvider().EvaluateAsync(
            Context("ng"),
            Options("NG"),
            CancellationToken.None);

        var signal = signals.Should().ContainSingle().Subject;
        signal.SignalType.Should().Be("Geography.HighRiskCountry");
        signal.Weight.Should().Be(25); // HighRiskCountryWeight capped by MaxSignalScore 50
        var details = JsonDocument.Parse(signal.Details).RootElement;
        details.GetProperty("billingCountry").GetString().Should().Be("NG");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Not_Flag_Countries_Off_The_List()
    {
        var signals = await CreateProvider().EvaluateAsync(
            Context("PT"),
            Options("NG", "XX"),
            CancellationToken.None);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_Should_Return_Nothing_Without_A_Country()
    {
        var signals = await CreateProvider().EvaluateAsync(
            Context(null),
            Options("NG"),
            CancellationToken.None);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_Should_Return_Nothing_When_The_List_Is_Empty()
    {
        var signals = await CreateProvider().EvaluateAsync(
            Context("NG"),
            Options(),
            CancellationToken.None);

        signals.Should().BeEmpty("the high-risk list is opt-in and empty by default");
    }
}
