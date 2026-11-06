using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Producer-path pins for the federated sign-in family and the logout revocation
///     (issue #1199: the dead event layer / suspicious-login coverage gap). The federated
///     families funnel through OAuthAuthService.CompleteSignInAsync, so a Discord callback
///     stands in for every provider callback, OIDC and SAML.
/// </summary>
public class FederatedSignInEventEmissionTests
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
    private readonly Mock<IAuthenticationAnomalyDetectionService> _anomalyDetectionMock = new();
    private readonly Mock<ISuspiciousLoginAlertPublisher> _suspiciousLoginAlertsMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly IConfiguration _configuration;

    private const string RedirectUri = "https://web.example.com/api/auth/callback/discord";

    public FederatedSignInEventEmissionTests()
    {
        PersistedAuthenticationSessions.Configure(_sessionManagementServiceMock);
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:RefreshTokenExpiryInDays", "7" },
                { "Jwt:RefreshTokenExpirationDays", "7" },
                { "Jwt:AccessTokenExpirationMinutes", "60" }
            })
            .Build();

        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile
            {
                ProviderId = "discord-snowflake-emit",
                Provider = "Discord",
                Email = "discord-emit@example.com",
                EmailVerified = true,
                Name = "Discord User",
                Username = "discorduser",
                AccessToken = "discord-access-token"
            });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("127.0.0.1");
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns((string token) => $"hash-{token}");

        _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepoMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _externalLoginRepoMock
            .Setup(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin dto, CancellationToken _) => dto);

        _mfaServiceMock
            .Setup(x => x.IsMfaRequiredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetUserMembershipsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserMembershipsResponse());
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        // Default: no confirmed anomalies.
        _anomalyDetectionMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new AuthenticationAnomalyResult { RiskLevel = RiskLevel.Low, RiskScore = 1, DetectedAnomalies = [] });
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
        _configuration,
        _authAttemptServiceMock.Object,
        _httpContextAccessorMock.Object,
        _senderMock.Object,
        _sessionManagementServiceMock.Object,
        NullLogger<OAuthAuthService>.Instance,
        anomalyDetectionService: _anomalyDetectionMock.Object,
        suspiciousLoginAlerts: _suspiciousLoginAlertsMock.Object,
        eventPublisher: _publisherMock.Object);

    private void SeedFreshDiscordIdentity()
    {
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("discord", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);
        _userRepoMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
    }

    private static DiscordSignInRequest Request() =>
        new() { Code = "discord-auth-code", State = "state-123", RedirectUri = RedirectUri };

    [Fact]
    public async Task FederatedSignIn_WithImpossibleTravel_FiresSuspiciousLoginAlert()
    {
        SeedFreshDiscordIdentity();
        Guid? resolvedUserId = null;
        _userRepoMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback((User user, CancellationToken _) => resolvedUserId = user.Id)
            .Returns(Task.CompletedTask);
        _anomalyDetectionMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new AuthenticationAnomalyResult
            {
                IsAnomalous = true,
                RiskLevel = RiskLevel.High,
                RiskScore = 95,
                DetectedAnomalies = [SecurityAlertKinds.ImpossibleTravel]
            });

        var result = await CreateSut().DiscordSignInAsync(Request());

        result.Success.Should().BeTrue();
        // The containment gap (issue #1199): federated sessions previously never reached
        // RiskEventSessionRevocationHandler; now every federated completion does.
        _suspiciousLoginAlertsMock.Verify(
            x => x.RecordAsync(
                It.Is<Guid>(id => resolvedUserId.HasValue && id == resolvedUserId.Value),
                It.IsAny<Guid?>(),
                SecurityAlertKinds.ImpossibleTravel,
                RiskLevel.High,
                95,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FederatedSignIn_Success_PublishesUserSignedInEventWithProviderMethod()
    {
        SeedFreshDiscordIdentity();

        var result = await CreateSut().DiscordSignInAsync(Request());

        result.Success.Should().BeTrue();
        _publisherMock.Verify(
            x => x.Publish(It.Is<UserSignedInEvent>(e =>
                e.AuthMethod == "Discord" &&
                e.Email == "discord-emit@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FederatedSignIn_ProviderFailure_PublishesAuthenticationFailedEvent()
    {
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("code exchange rejected"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateSut().DiscordSignInAsync(Request()));

        _publisherMock.Verify(
            x => x.Publish(It.Is<AuthenticationFailedEvent>(e =>
                e.Reason == "UnauthorizedAccessException" &&
                e.IpAddress == "127.0.0.1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FederatedSignIn_WithoutConfirmedSignals_DoesNotAlert()
    {
        SeedFreshDiscordIdentity();

        var result = await CreateSut().DiscordSignInAsync(Request());

        result.Success.Should().BeTrue();
        _suspiciousLoginAlertsMock.Verify(
            x => x.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<RiskLevel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

/// <summary>Producer-path pin for the logout-everywhere refresh-token revocation (issue #1199).</summary>
public class LogoutEventEmissionTests
{
    [Fact]
    public async Task LogoutEverywhere_PublishesRefreshTokenRevokedEventForAllTokens()
    {
        var publisher = new Mock<IPublisher>();
        var sessions = new Mock<IUserSessionRepository>();
        sessions
            .Setup(repository => repository.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var userId = Guid.NewGuid();
        var handler = new LogoutHandler(
            Mock.Of<ITokenRevocationService>(),
            Mock.Of<IRefreshTokenRepository>(),
            sessions.Object,
            NullLogger<LogoutHandler>.Instance,
            eventPublisher: publisher.Object);

        var response = await handler.Handle(
            new LogoutCommand { UserId = userId, LogoutEverywhere = true, Reason = "User initiated logout everywhere" },
            CancellationToken.None);

        response.Success.Should().BeTrue();
        publisher.Verify(
            x => x.Publish(It.Is<RefreshTokenRevokedEvent>(e =>
                e.TokenId == "all" &&
                e.UserId == userId &&
                e.Reason == "User initiated logout everywhere"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
