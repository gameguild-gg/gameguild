using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.StepUp;

/// <summary>
///     Issue #1191: the step-up trigger must answer with the structured
///     <see cref="StepUpAuthenticationRequiredResponse"/> — the binding to challenge, the
///     account's enrolled MFA methods and the challenge window — instead of a generic 403.
/// </summary>
public sealed class StepUpRequiredResponderTests
{
    private readonly Mock<IUserMfaConfigurationRepository> _configurations = new();
    private readonly Mock<IWebAuthnCredentialRepository> _credentials = new();

    private StepUpRequiredResponder CreateSut() =>
        new(_configurations.Object, _credentials.Object, new MfaOptions { Enabled = true },
            NullLogger<StepUpRequiredResponder>.Instance);

    private static UserMfaConfiguration Configuration(params string[] methods) => new()
    {
        IsEnabled = methods.Length > 0,
        IsSetupComplete = methods.Length > 0,
        TotpSecretKey = methods.Contains("Totp") ? "synthetic-enrollment" : null,
        BackupCodes = methods.Contains("BackupCode") ? "set-v1$1$synthetic-remaining-hash" : string.Empty
    };

    [Fact]
    public async Task BuildAsync_CarriesTheOperationBindingAndChallengeWindow()
    {
        _configurations.Setup(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Configuration("Totp", "BackupCode"));

        var response = await CreateSut().BuildAsync(
            Guid.NewGuid(), "economy.payout.reserve", "payout-request:42", "A valid step-up receipt is required.");

        response.StepUpRequired.Should().BeTrue();
        response.Reason.Should().Be("A valid step-up receipt is required.");
        response.AvailableMethods.Should().BeEquivalentTo(["Totp", "BackupCode"]);
        response.RiskFactors.Should().Contain(["operation:economy.payout.reserve", "target:payout-request:42"]);
        response.RiskLevel.Should().Be(RiskLevel.High);
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        response.StepUpToken.Should().BeEmpty("the challenge ceremony is created by the authenticated caller; no temp token is issued");
    }

    [Fact]
    public async Task BuildAsync_FromTheException_UsesTheExceptionBinding()
    {
        _configurations.Setup(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Configuration("WebAuthn"));
        _credentials.Setup(s => s.HasActiveCredentialsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var requirement = new StepUpRequiredException(
            "Step-up receipt is invalid, expired, mismatched, or already consumed.",
            "trust-device", "current-session",
            Convert.ToHexString(new byte[32]).ToLowerInvariant());

        var response = await CreateSut().BuildAsync(Guid.NewGuid(), requirement);

        response.Reason.Should().Be(requirement.Message);
        response.RiskFactors.Should().Contain("operation:trust-device");
        response.AvailableMethods.Should().BeEquivalentTo(["WebAuthn"]);
    }

    [Fact]
    public async Task BuildAsync_WithoutEnrolledFactors_ReturnsAnEmptyMethodList()
    {
        _configurations.Setup(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Configuration());

        var response = await CreateSut().BuildAsync(
            Guid.NewGuid(), "trust-device", "current-session", "reason");

        response.AvailableMethods.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_MfaConfigurationFailure_DoesNotFailTheTrigger()
    {
        _configurations.Setup(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var response = await CreateSut().BuildAsync(
            Guid.NewGuid(), "trust-device", "current-session", "reason");

        response.StepUpRequired.Should().BeTrue();
        response.AvailableMethods.Should().BeEmpty();
    }
}
