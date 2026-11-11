using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Guest (discoverable-credential) ceremony resolution (issue #1198): completion must
///     correlate the assertion to the exact ceremony via the base64url challenge echoed in
///     the client data — not via a single shared guest slot that a concurrent tab overwrote.
/// </summary>
public sealed class WebAuthnGuestCeremonyTests
{
    // Base64url ceremony token, built at runtime so no high-entropy literal sits in source.
    private static readonly string ChallengeToken = Base64Url(Encoding.UTF8.GetBytes("guest-ceremony-challenge"));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>A wire-shaped assertion JSON whose client data echoes the ceremony challenge token.</summary>
    private static string AssertionJson(string challengeToken, byte[] rawId)
    {
        var clientData = JsonSerializer.Serialize(new
        {
            type = "webauthn.get",
            challenge = challengeToken,
            origin = "https://localhost"
        });
        // Every byte[] on the wire is base64url (no padding, URL-safe) — Fido2's converters
        // reject standard padded base64.
        var clientDataBytes = Base64Url(Encoding.UTF8.GetBytes(clientData));
        return JsonSerializer.Serialize(new
        {
            id = Base64Url(rawId),
            rawId = Base64Url(rawId),
            type = "public-key",
            response = new
            {
                clientDataJSON = clientDataBytes,
                authenticatorData = Base64Url(new byte[37]),
                signature = Base64Url(new byte[70]),
                userHandle = Base64Url(Guid.NewGuid().ToByteArray())
            }
        });
    }

    private static (Mock<IFido2> Fido2, Mock<IWebAuthnCredentialRepository> Credentials, Mock<IWebAuthnSessionStore> Sessions, WebAuthnAuthenticationSubService Service) CreateSubject()
    {
        var fido2 = new Mock<IFido2>();
        var credentials = new Mock<IWebAuthnCredentialRepository>();
        var sessions = new Mock<IWebAuthnSessionStore>();
        var service = new WebAuthnAuthenticationSubService(
            fido2.Object,
            credentials.Object,
            Mock.Of<IUserRepository>(),
            sessions.Object,
            NullLogger<WebAuthnAuthenticationSubService>.Instance,
            auditEventSink: null,
            httpContextAccessor: Mock.Of<IHttpContextAccessor>());
        return (fido2, credentials, sessions, service);
    }

    private static UserWebAuthnCredential Credential(Guid userId, byte[] rawId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CredentialId = Convert.ToBase64String(rawId),
        PublicKey = Convert.ToBase64String(new byte[32]),
        SignatureCounter = 0,
        IsPasswordless = true
    };

    [Fact]
    public async Task CompleteAuthentication_GuestCeremonyMissingForChallenge_ReturnsSessionNotFound()
    {
        var (fido2, credentials, sessions, service) = CreateSubject();
        var userId = Guid.NewGuid();
        var rawId = new byte[] { 7, 7, 7 };
        credentials
            .Setup(repository => repository.GetByCredentialIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential(userId, rawId));
        sessions
            .Setup(store => store.FindAssertionSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebAuthnSession?)null);
        sessions
            .Setup(store => store.FindGuestAssertionSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebAuthnSession?)null);

        var result = await service.CompleteAuthenticationAsync(AssertionJson(ChallengeToken, rawId));

        Assert.False(result.Success);
        Assert.Equal("Authentication session not found or expired", result.Error);
        // The guest lookup is keyed by the exact challenge token the browser echoed.
        sessions.Verify(
            store => store.FindGuestAssertionSessionAsync(ChallengeToken, It.IsAny<CancellationToken>()),
            Times.Once);
        fido2.Verify(f => f.MakeAssertionAsync(It.IsAny<MakeAssertionParams>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAuthentication_ConcurrentGuestCeremony_UsesTheChallengeKeyedSession()
    {
        var (fido2, credentials, sessions, service) = CreateSubject();
        var userId = Guid.NewGuid();
        var rawId = new byte[] { 8, 8, 8 };
        credentials
            .Setup(repository => repository.GetByCredentialIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential(userId, rawId));
        // The owner has no user-keyed assertion session (discoverable-credential flow): only the
        // challenge-keyed guest ceremony issued to THIS tab exists — a concurrent tab holds a
        // different challenge and is untouched.
        sessions
            .Setup(store => store.FindAssertionSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebAuthnSession?)null);
            sessions
            .Setup(store => store.FindGuestAssertionSessionAsync(ChallengeToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnSession
            {
                UserId = null,
                Challenge = Encoding.UTF8.GetBytes("guest-ceremony-challenge"),
                AssertionOptions = GuestAssertionOptions(ChallengeToken)
            });
        fido2
            .Setup(f => f.MakeAssertionAsync(It.IsAny<MakeAssertionParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyAssertionResult { SignCount = 1 });

        var result = await service.CompleteAuthenticationAsync(AssertionJson(ChallengeToken, rawId));

        Assert.True(result.Success, result.Error);
        // Only the ceremony that was actually completed is removed.
        sessions.Verify(
            store => store.RemoveGuestAssertionSessionAsync(ChallengeToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AssertionOptions GuestAssertionOptions(string challengeToken) =>
        AssertionOptions.FromJson($$"""
            {
                "challenge": "{{challengeToken}}",
                "timeout": 30000,
                "allowCredentials": [],
                "userVerification": "preferred"
            }
            """);
}
