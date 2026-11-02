using FluentAssertions;
using Fido2NetLib.Objects;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Configuration;

/// <summary>
///     WebAuthn ceremony options (issue #1143): attestation conveyance and the
///     user-verification requirement are configuration-driven with the previous
///     hard-coded values as safe defaults.
/// </summary>
public sealed class WebAuthnCeremonyOptionsTests
{
    [Fact]
    public void Defaults_ReproduceThePreviousHardCodedCeremonyValues()
    {
        var options = new WebAuthnCeremonyOptions();

        options.AttestationConveyance.Should().Be("none");
        options.UserVerification.Should().Be("preferred");
        options.ResolvedAttestationConveyance.Should().Be(AttestationConveyancePreference.None);
        options.ResolvedUserVerification.Should().Be(UserVerificationRequirement.Preferred);
    }

    [Theory]
    [InlineData("none", AttestationConveyancePreference.None)]
    [InlineData("Indirect", AttestationConveyancePreference.Indirect)]
    [InlineData("DIRECT", AttestationConveyancePreference.Direct)]
    [InlineData("enterprise", AttestationConveyancePreference.Enterprise)]
    public void AttestationConveyance_ResolvesConfiguredValues(string configured, AttestationConveyancePreference expected)
    {
        var options = new WebAuthnCeremonyOptions { AttestationConveyance = configured };

        options.ResolvedAttestationConveyance.Should().Be(expected);
    }

    [Theory]
    [InlineData("preferred", UserVerificationRequirement.Preferred)]
    [InlineData("Required", UserVerificationRequirement.Required)]
    [InlineData("DISCOURAGED", UserVerificationRequirement.Discouraged)]
    public void UserVerification_ResolvesConfiguredValues(string configured, UserVerificationRequirement expected)
    {
        var options = new WebAuthnCeremonyOptions { UserVerification = configured };

        options.ResolvedUserVerification.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bogus")]
    public void UnrecognizedValues_FallBackToSafeDefaults(string configured)
    {
        var options = new WebAuthnCeremonyOptions { AttestationConveyance = configured, UserVerification = configured };

        options.ResolvedAttestationConveyance.Should().Be(AttestationConveyancePreference.None);
        options.ResolvedUserVerification.Should().Be(UserVerificationRequirement.Preferred);
    }
}
