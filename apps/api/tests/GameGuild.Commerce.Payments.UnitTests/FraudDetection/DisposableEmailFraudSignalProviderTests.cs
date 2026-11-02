using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Unit tests for the disposable-email fraud signal (issue #409: flag disposable email
///     providers such as mailinator or guerrillamail).
/// </summary>
public class DisposableEmailFraudSignalProviderTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _paymentId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();

    private DisposableEmailFraudSignalProvider CreateProvider() =>
        new(NullLogger<DisposableEmailFraudSignalProvider>.Instance);

    private FraudSignalContext Context(string? payerEmail) =>
        new(
            _tenantId,
            _paymentId,
            _subscriptionId,
            ExternalCustomerId: null,
            PaymentMethodId: null,
            50m,
            "USD",
            BillingCycleNumber: 2,
            Country: null,
            SourceIpAddress: null,
            DeviceFingerprint: null,
            PayerEmail: payerEmail);

    [Theory]
    [InlineData("user@mailinator.com")]
    [InlineData("user@GUERRILLAMAIL.com")]
    [InlineData("throwaway@10MinuteMail.com")]
    public async Task EvaluateAsync_Should_Flag_Disposable_Email_Domains(string email)
    {
        var signals = await CreateProvider().EvaluateAsync(Context(email), new FraudAssessmentOptions(), CancellationToken.None);

        var signal = signals.Should().ContainSingle().Subject;
        signal.SignalType.Should().Be("Email.DisposableDomain");
        signal.Weight.Should().Be(30); // DisposableEmailWeight capped by MaxSignalScore 50
        var details = JsonDocument.Parse(signal.Details).RootElement;
        details.GetProperty("domain").GetString().Should().Be(email.Split('@')[1].ToLowerInvariant());
    }

    [Fact]
    public async Task EvaluateAsync_Should_Not_Flag_Corporate_Email_Domains()
    {
        var signals = await CreateProvider().EvaluateAsync(
            Context("user@gameguild.gg"),
            new FraudAssessmentOptions(),
            CancellationToken.None);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_Should_Return_Nothing_When_No_Payer_Email_Is_Present()
    {
        var signals = await CreateProvider().EvaluateAsync(
            Context(null),
            new FraudAssessmentOptions(),
            CancellationToken.None);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_Should_Honor_An_Empty_Configured_List()
    {
        var options = new FraudAssessmentOptions { DisposableEmailDomains = [] };

        var signals = await CreateProvider().EvaluateAsync(
            Context("user@mailinator.com"),
            options,
            CancellationToken.None);

        signals.Should().BeEmpty("an empty configured list disables the signal");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Honor_Configured_Custom_Domains()
    {
        var options = new FraudAssessmentOptions
        {
            DisposableEmailDomains = ["burner.example"],
            DisposableEmailWeight = 45
        };

        var signals = await CreateProvider().EvaluateAsync(
            Context("someone@burner.example"),
            options,
            CancellationToken.None);

        var signal = signals.Should().ContainSingle().Subject;
        signal.Weight.Should().Be(45);
    }

    [Fact]
    public void DefaultOptions_Should_Cover_The_Canonical_Disposable_Providers()
    {
        var defaults = new FraudAssessmentOptions().DisposableEmailDomains;

        defaults.Should().Contain("mailinator.com");
        defaults.Should().Contain("guerrillamail.com");
        defaults.Should().Contain("10minutemail.com");
        defaults.Should().Contain("temp-mail.org");
    }
}
