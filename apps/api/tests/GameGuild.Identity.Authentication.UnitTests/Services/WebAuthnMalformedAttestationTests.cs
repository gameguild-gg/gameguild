using Fido2NetLib;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     WebAuthn malformed attestation payloads (issue #1130): a non-standard browser (or a
///     hostile client) can send corrupted JSON, an empty body, an oversized payload, or a
///     well-formed envelope whose attestation is semantically garbage. The registration
///     service must answer every one of these with a failure <b>result</b> — never an
///     unhandled exception — so the controller maps it to HTTP 400, not 500. The tests run
///     the REAL Fido2 verifier so the corrupted-attestation path is exercised end to end
///     (parse → session lookup → verification), not just the JSON parser.
/// </summary>
public sealed class WebAuthnMalformedAttestationTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IWebAuthnCredentialRepository> _credentialRepository = new();
    private readonly Mock<IWebAuthnSessionStore> _sessionStore = new();

    private WebAuthnRegistrationService CreateSut() => new(
        CreateRealFido2(),
        _credentialRepository.Object,
        _sessionStore.Object,
        NullLogger<WebAuthnRegistrationService>.Instance);

    private static IFido2 CreateRealFido2() => new Fido2(new Fido2Configuration
    {
        ServerDomain = "localhost",
        ServerName = "GameGuild unit tests",
        Origins = new HashSet<string> { "https://localhost:3000" },
        TimestampDriftTolerance = 60000
    });

    private void SessionLookupReturns(WebAuthnSession? session) => _sessionStore
        .Setup(store => store.FindRegistrationSessionAsync(UserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(session);

    /// <summary>Starts a real registration so the session carries genuine Fido2 options.</summary>
    private async Task<WebAuthnSession> BeginRealRegistrationAsync()
    {
        WebAuthnSession? stored = null;
        _credentialRepository
            .Setup(repo => repo.GetActiveByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserWebAuthnCredential>());
        _sessionStore
            .Setup(store => store.StoreRegistrationSessionAsync(It.IsAny<WebAuthnSession>(), It.IsAny<CancellationToken>()))
            .Returns((WebAuthnSession session, CancellationToken _) =>
            {
                stored = session;
                return Task.FromResult("registration-session");
            });

        var begun = await CreateSut().BeginRegistrationAsync(UserId, "webauthn@example.test", "WebAuthn User");
        begun.Success.Should().BeTrue("the happy path must work for these fixtures to be meaningful");
        return stored!;
    }

    // ── Structurally malformed payloads ───────────────────────────────────

    [Theory]
    [InlineData("")]                                   // empty payload
    [InlineData("   ")]                                // whitespace-only payload
    [InlineData("{corrupted attestation")]             // truncated JSON
    [InlineData("{\"id\":123,\"rawId\":")]             // truncated mid-member
    [InlineData("\u0000{\"id\":1}")]                   // binary garbage (NUL-prefixed)
    public async Task CompleteRegistration_MalformedPayload_ReturnsFailureResult_NotAnException(string payload)
    {
        var sut = CreateSut();

        var result = await sut.CompleteRegistrationAsync(UserId, payload);

        result.Success.Should().BeFalse("a malformed payload is a client error, never a server error");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteRegistration_JsonNullLiteral_ReturnsInvalidAttestationFailure()
    {
        // "null" parses successfully to a null object: the explicit invalid-attestation branch.
        var sut = CreateSut();

        var result = await sut.CompleteRegistrationAsync(UserId, "null");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid attestation response");
    }

    [Fact]
    public async Task CompleteRegistration_WellFormedEnvelopeWithUnknownMembers_FailsGracefully()
    {
        // Valid JSON, wrong shape (an API-version-changed browser envelope): parses, but no
        // ceremony was ever started for the user — reported as a session failure.
        SessionLookupReturns(null);
        var sut = CreateSut();

        var result = await sut.CompleteRegistrationAsync(UserId, """{"foo":"bar","unrelated":42}""");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Registration session not found or expired");
    }

    // ── Oversized payloads ────────────────────────────────────────────────

    [Fact]
    public async Task CompleteRegistration_OversizedGarbagePayload_ReturnsFailureResult()
    {
        // ~2 MB of unparsable content (larger than any legitimate attestation).
        var payload = "{" + new string('x', 2 * 1024 * 1024);
        var sut = CreateSut();

        var result = await sut.CompleteRegistrationAsync(UserId, payload);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteRegistration_OversizedWellFormedPayload_FailsGracefully()
    {
        // ~2 MB but valid JSON shape: the payload either fails member conversion or parses
        // and misses the ceremony — both are client failures, never a 500.
        SessionLookupReturns(null);
        var payload = """{"id":""" + $"\"{new string('a', 2 * 1024 * 1024)}\"" + ""","rawId":"x","type":"public-key"}""";
        var sut = CreateSut();

        var result = await sut.CompleteRegistrationAsync(UserId, payload);

        result.Success.Should().BeFalse();
        result.Error.Should().BeOneOf(
            "Registration session not found or expired",
            "Failed to complete WebAuthn registration");
    }

    // ── Corrupted attestation against the real verifier ───────────────────

    [Fact]
    public async Task CompleteRegistration_CorruptedAttestationAgainstRealVerifier_ReturnsFailureResult()
    {
        // Real options from a real registration:base64url of random bytes — every field decodes
        // but nothing verifies. The REAL Fido2 verifier must reject it as a failure result.
        var session = await BeginRealRegistrationAsync();
        SessionLookupReturns(session);
        var sut = CreateSut();

        var result = await sut.CompleteRegistrationAsync(UserId, CorruptButWellFormedAttestation());

        result.Success.Should().BeFalse("a corrupted attestation must never register a credential");
        _credentialRepository.Verify(
            repo => repo.CreateAsync(It.IsAny<UserWebAuthnCredential>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteRegistration_Base64CorruptedClientData_ReturnsFailureResult()
    {
        var session = await BeginRealRegistrationAsync();
        SessionLookupReturns(session);
        var sut = CreateSut();

        var payload = """
            {
              "id": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
              "rawId": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
              "type": "public-key",
              "response": {
                "clientDataJSON": "!!! not base64 !!!",
                "attestationObject": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
              }
            }
            """;

        var result = await sut.CompleteRegistrationAsync(UserId, payload);

        result.Success.Should().BeFalse();
        _credentialRepository.Verify(
            repo => repo.CreateAsync(It.IsAny<UserWebAuthnCredential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteRegistration_EmptyAttestationMembers_ReturnsFailureResult()
    {
        var session = await BeginRealRegistrationAsync();
        SessionLookupReturns(session);
        var sut = CreateSut();

        var payload = """
            {
              "id": "",
              "rawId": "",
              "type": "public-key",
              "response": {
                "clientDataJSON": "",
                "attestationObject": ""
              }
            }
            """;

        var result = await sut.CompleteRegistrationAsync(UserId, payload);

        result.Success.Should().BeFalse();
        _credentialRepository.Verify(
            repo => repo.CreateAsync(It.IsAny<UserWebAuthnCredential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>A structurally valid envelope whose attestation content is random bytes.</summary>
    private static string CorruptButWellFormedAttestation()
    {
        var garbage = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(96));
        return $$"""
            {
              "id": "{{garbage}}",
              "rawId": "{{garbage}}",
              "type": "public-key",
              "response": {
                "clientDataJSON": "{{garbage}}",
                "attestationObject": "{{garbage}}"
              }
            }
            """;
    }
}
