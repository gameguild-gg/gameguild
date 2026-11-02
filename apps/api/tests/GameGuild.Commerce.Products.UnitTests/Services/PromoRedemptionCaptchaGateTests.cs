using FluentAssertions;
using GameGuild.Commerce.Products;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Services;

/// <summary>
/// Issue #418 CAPTCHA gate: config-flagged, fail-closed on untrusted redemption
/// contexts. Disabled by default; enabled without a verifier rejects every
/// untrusted redemption; a valid self-issued token passes.
/// </summary>
public class PromoRedemptionCaptchaGateTests
{
    private static PromoRedemptionCaptchaGate CreateGate(bool enabled, string? secret = null)
        => new(Options.Create(new PromoRedemptionCaptchaOptions
        {
            Enabled = enabled,
            VerificationSecret = secret
        }));

    [Fact]
    public void Check_Disabled_AllowsEverything()
    {
        var gate = CreateGate(enabled: false, secret: null);

        var anonymous = gate.Check(isTrustedContext: false, captchaToken: null);
        var trusted = gate.Check(isTrustedContext: true, captchaToken: null);

        anonymous.Allowed.Should().BeTrue();
        trusted.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Check_Enabled_TrustedContext_PassesWithoutToken()
    {
        var gate = CreateGate(enabled: true, secret: "s3cret");

        var decision = gate.Check(isTrustedContext: true, captchaToken: null);

        decision.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Check_Enabled_UntrustedWithoutToken_FailsClosed()
    {
        var gate = CreateGate(enabled: true, secret: "s3cret");

        var decision = gate.Check(isTrustedContext: false, captchaToken: null);

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain("CAPTCHA");
        decision.Outcome.Should().Be(PromoCodeValidationOutcome.CaptchaRequired);
    }

    [Fact]
    public void Check_Enabled_UntrustedWithoutSecret_FailsClosed()
    {
        // Enabled with no verifier configured: nothing can be verified, so untrusted
        // redemption is rejected outright — fail closed, never fail open.
        var gate = CreateGate(enabled: true, secret: null);

        var decision = gate.Check(isTrustedContext: false, captchaToken: "any-token");

        decision.Allowed.Should().BeFalse();
        decision.Outcome.Should().Be(PromoCodeValidationOutcome.CaptchaRequired);
    }

    [Fact]
    public void Check_Enabled_UntrustedWithFreshIssuedToken_Allows()
    {
        const string secret = "s3cret";
        var gate = CreateGate(enabled: true, secret);
        var token = PromoRedemptionCaptchaGate.IssueToken("challenge-1", secret);

        var decision = gate.Check(isTrustedContext: false, captchaToken: token);

        decision.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Check_Enabled_UntrustedWithGarbageToken_FailsClosed()
    {
        var gate = CreateGate(enabled: true, secret: "s3cret");

        var decision = gate.Check(isTrustedContext: false, captchaToken: "not-a-token");

        decision.Allowed.Should().BeFalse();
    }

    [Fact]
    public void Check_Enabled_UntrustedWithWrongSecretToken_FailsClosed()
    {
        var gate = CreateGate(enabled: true, secret: "s3cret");
        var token = PromoRedemptionCaptchaGate.IssueToken("challenge-1", "other-secret");

        var decision = gate.Check(isTrustedContext: false, captchaToken: token);

        decision.Allowed.Should().BeFalse();
    }

    [Fact]
    public void Check_Enabled_UntrustedWithExpiredToken_FailsClosed()
    {
        var gate = CreateGate(enabled: true, secret: "s3cret");
        var token = PromoRedemptionCaptchaGate.IssueToken(
            "challenge-1", "s3cret", utcNow: SystemClock.UtcNow.AddMinutes(-30));

        var decision = gate.Check(isTrustedContext: false, captchaToken: token);

        decision.Allowed.Should().BeFalse();
    }
}
