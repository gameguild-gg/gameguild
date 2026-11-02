using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     OAuth provider failure surfacing (issue #1125): when the upstream provider fails
///     (5xx / timeout / malformed JSON / API-version change), the orchestrating
///     <see cref="OAuthAuthService" /> must keep the failure diagnosable and side-effect free:
///     no user is provisioned or linked, no session is created and no tokens are minted from
///     a failed exchange; the failed attempt is recorded for lockout analytics and an
///     <c>Authentication.ExternalLoginFailed</c> security audit event is emitted. The raw
///     provider exception propagates so the API edge maps it to an error response — a
///     provider outage is an infrastructure failure, never a silent sign-in.
/// </summary>
public sealed class OAuthProviderFailureSurfacingTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock = new();
    private readonly Mock<IOAuthService> _oauthServiceMock = new();
    private readonly Mock<IGoogleIdTokenVerifier> _googleVerifierMock = new();
    private readonly Mock<IOidcFederationService> _oidcFederationServiceMock = new();
    private readonly Mock<IMfaService> _mfaServiceMock = new();
    private readonly Mock<IExternalLoginRepository> _externalLoginRepoMock = new();
    private readonly Mock<IAuthAttemptService> _authAttemptServiceMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ISessionManagementService> _sessionManagementServiceMock = new();
    private readonly Mock<IAuthenticationAuditEventSink> _auditEventSinkMock = new();

    public OAuthProviderFailureSurfacingTests()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "ProviderFailureTestAgent/1.0";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("198.51.100.77");
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns((string token) => $"hash-{token}");
        _auditEventSinkMock
            .Setup(sink => sink.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _oauthServiceMock
            .Setup(x => x.ResolveAuthorizationScopes(It.IsAny<string>(), It.IsAny<string[]?>()))
            .Returns<string, string[]>((_, scopes) => scopes ?? []);
    }

    private OAuthAuthService CreateSut() => new(
        _userRepoMock.Object,
        _jwtTokenServiceMock.Object,
        _refreshTokenHasherMock.Object,
        _oauthServiceMock.Object,
        _googleVerifierMock.Object,
        _oidcFederationServiceMock.Object,
        _mfaServiceMock.Object,
        _externalLoginRepoMock.Object,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build(),
        _authAttemptServiceMock.Object,
        _httpContextAccessorMock.Object,
        _senderMock.Object,
        _sessionManagementServiceMock.Object,
        NullLogger<OAuthAuthService>.Instance,
        auditEventSink: _auditEventSinkMock.Object);

    private static GitHubCallbackSignInRequest Callback() => new()
    {
        Code = "provider-code",
        State = "csrf-state",
        RedirectUri = "https://app.example.test/callback"
    };

    private void ProviderThrows(Exception providerFailure) => _oauthServiceMock
        .Setup(x => x.HandleCallbackAsync("github", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .ThrowsAsync(providerFailure);

    private void AssertNoSignInSideEffects()
    {
        _jwtTokenServiceMock.Verify(
            tokens => tokens.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _jwtTokenServiceMock.Verify(
            tokens => tokens.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _userRepoMock.Verify(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _externalLoginRepoMock.Verify(
            repo => repo.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void AssertFailureAccounted(string expectedFailureReason) =>
        _authAttemptServiceMock.Verify(
            attempts => attempts.RecordFailedAttemptAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                expectedFailureReason, It.IsAny<TimeSpan>(), It.IsAny<string>()),
            Times.Once);

    private void AssertExternalLoginFailedAudited() => _auditEventSinkMock.Verify(
        sink => sink.RecordAsync(
            It.Is<AuthenticationAuditEvent>(audit =>
                audit.ActionType == "Authentication.ExternalLoginFailed" && audit.Success == false),
            It.IsAny<CancellationToken>()),
        Times.Once);

    [Fact]
    public async Task ProviderReturns500_FailurePropagatesWithoutIssuingCredentialsOrSessions()
    {
        ProviderThrows(new HttpRequestException("Response status code does not indicate success: 500"));

        var act = () => CreateSut().GitHubCallbackSignInAsync(Callback());

        await act.Should().ThrowAsync<HttpRequestException>();
        AssertNoSignInSideEffects();
        AssertFailureAccounted("HttpRequestException");
        AssertExternalLoginFailedAudited();
    }

    [Fact]
    public async Task ProviderTimesOut_FailurePropagatesWithoutIssuingCredentialsOrSessions()
    {
        // HttpClient timeout surface. Cancellation-class failures propagate undecorated so
        // they are never mistaken for a credential rejection; no half-signed-in state remains.
        ProviderThrows(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        var act = () => CreateSut().GitHubCallbackSignInAsync(Callback());

        await act.Should().ThrowAsync<TaskCanceledException>();
        AssertNoSignInSideEffects();
    }

    [Fact]
    public async Task ProviderReturnsMalformedJson_FailurePropagatesWithoutIssuingCredentialsOrSessions()
    {
        ProviderThrows(new JsonException("is an invalid start of a value"));

        var act = () => CreateSut().GitHubCallbackSignInAsync(Callback());

        await act.Should().ThrowAsync<JsonException>();
        AssertNoSignInSideEffects();
        AssertFailureAccounted("JsonException");
        AssertExternalLoginFailedAudited();
    }

    [Fact]
    public async Task ProviderChangedApiResponseShape_FailurePropagatesWithoutIssuingCredentialsOrSessions()
    {
        // "Provider changes API version": the token exchange succeeds but the profile
        // payload no longer contains the expected members (KeyNotFoundException from the
        // strict member access). Still: no session, no tokens, failure accounted.
        ProviderThrows(new KeyNotFoundException("The requested key 'id' was not present in the response."));

        var act = () => CreateSut().GitHubCallbackSignInAsync(Callback());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        AssertNoSignInSideEffects();
        AssertFailureAccounted("KeyNotFoundException");
        AssertExternalLoginFailedAudited();
    }

    [Fact]
    public async Task SharedProviderCallbackPath_PropagatesProviderFailureWithoutSideEffects()
    {
        // Steam/Epic/Facebook/Microsoft sign-ins run through the shared callback pipeline;
        // pin the same guarantee for one of them (Microsoft).
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("microsoft", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Response status code does not indicate success: 502"));

        var act = () => CreateSut().MicrosoftCallbackSignInAsync(new MicrosoftCallbackSignInRequest
        {
            Code = "provider-code",
            State = "csrf-state",
            RedirectUri = "https://app.example.test/callback"
        });

        await act.Should().ThrowAsync<HttpRequestException>();
        AssertNoSignInSideEffects();
        AssertFailureAccounted("HttpRequestException");
        AssertExternalLoginFailedAudited();
    }

    [Fact]
    public async Task AuditSinkFailure_DoesNotMaskTheProviderFailure()
    {
        // The audit sink is best-effort: when it is unavailable the original provider
        // failure (not the audit error) reaches the caller.
        ProviderThrows(new HttpRequestException("Response status code does not indicate success: 500"));
        _auditEventSinkMock
            .Setup(sink => sink.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit sink unavailable"));

        var act = () => CreateSut().GitHubCallbackSignInAsync(Callback());

        await act.Should().ThrowAsync<HttpRequestException>();
        AssertNoSignInSideEffects();
    }
}
