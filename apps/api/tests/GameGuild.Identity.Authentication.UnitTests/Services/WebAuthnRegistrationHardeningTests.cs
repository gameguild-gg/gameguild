using Fido2NetLib;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     WebAuthn registration hardening (issue #1143): the ceremony must ask for anonymized
///     attestation (indirect conveyance) instead of none, so provenance can be validated for
///     enterprise policy, and must never assume user verification happened.
/// </summary>
public sealed class WebAuthnRegistrationHardeningTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IWebAuthnCredentialRepository> _credentialRepository = new();
    private readonly Mock<IWebAuthnSessionStore> _sessionStore = new();

    private WebAuthnRegistrationService CreateSut() => new(
        new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "GameGuild unit tests",
            Origins = new HashSet<string> { "https://localhost:3000" },
            TimestampDriftTolerance = 60000
        }),
        _credentialRepository.Object,
        _sessionStore.Object,
        NullLogger<WebAuthnRegistrationService>.Instance);

    [Fact]
    public async Task BeginRegistration_RequestsIndirectAttestationConveyance()
    {
        _credentialRepository
            .Setup(repo => repo.GetActiveByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserWebAuthnCredential>());
        _sessionStore
            .Setup(store => store.StoreRegistrationSessionAsync(It.IsAny<WebAuthnSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult("registration-session"));

        var begun = await CreateSut().BeginRegistrationAsync(UserId, "webauthn@example.test", "WebAuthn User");

        begun.Success.Should().BeTrue();
        // The wire options are the contract the browser sees: conveyance must be indirect,
        // not none (issue #1143).
        begun.OptionsJson.Should().Contain("indirect");
        begun.OptionsJson.Should().NotContain("\"none\"");
    }
}
