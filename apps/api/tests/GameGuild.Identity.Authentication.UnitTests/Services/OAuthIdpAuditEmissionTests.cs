using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
/// Identity-provider integration auditing (issue #180): every federated/external sign-in —
/// success, failure, just-in-time provisioning and account linking — journals a durable
/// Authentication.External* event through the central authentication audit sink, and an audit
/// sink failure never breaks sign-in itself.
/// </summary>
public class OAuthIdpAuditEmissionTests
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
    private readonly RecordingAuthenticationAuditEventSink _auditSink = new();
    private readonly IConfiguration _configuration;

    public OAuthIdpAuditEmissionTests()
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
            .Setup(x => x.GetUserProfileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile
            {
                ProviderId = "oauth-sub-default",
                Email = "oauth@example.com",
                EmailVerified = true,
                Name = "OAuth User",
                AccessToken = "provider-token"
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
            .ReturnsAsync((ExternalLogin dto, CancellationToken _) =>
            {
                dto.Id = Guid.NewGuid();
                return dto;
            });

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
    }

    private OAuthAuthService CreateSut(IAuthenticationAuditEventSink? sink = null) => new(
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
        auditEventSink: sink ?? _auditSink);

    [Fact]
    public async Task SuccessfulExternalSignInJournalsExternalLoginSucceeded()
    {
        var user = User.CreateOAuthUser("oauth@example.com", "OAuth User");
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("github", "oauth-sub-default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLogin { Id = Guid.NewGuid(), UserId = user.Id, Provider = "github", ProviderKey = "oauth-sub-default" });
        _userRepoMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateSut().GitHubSignInAsync(new OAuthSignInRequest { AccessToken = "gh-token" });

        result.Success.Should().BeTrue();
        var succeeded = _auditSink.Events.Should().ContainSingle(
            auditEvent => auditEvent.ActionType == "Authentication.ExternalLoginSucceeded").Subject;
        succeeded.UserId.Should().Be(user.Id);
        succeeded.Success.Should().BeTrue();
        succeeded.SessionId.Should().Be(result.SessionId);
        succeeded.Method.Should().Be("GitHub");
    }

    [Fact]
    public async Task JustInTimeProvisioningJournalsExternalAccountProvisioned()
    {
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);
        _userRepoMock.Setup(x => x.GetByEmailAsync("oauth@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await CreateSut().GitHubSignInAsync(new OAuthSignInRequest { AccessToken = "gh-token" });

        _auditSink.Events.Should().Contain(auditEvent =>
            auditEvent.ActionType == "Authentication.ExternalAccountProvisioned" && auditEvent.Success);
        _auditSink.Events.Should().Contain(auditEvent => auditEvent.ActionType == "Authentication.ExternalLoginSucceeded");
    }

    [Fact]
    public async Task VerifiedEmailMatchJournalsExternalAccountLinked()
    {
        var existingUser = User.CreateWithPassword(
            "oauth@example.com", "existing", BCrypt.Net.BCrypt.HashPassword("irrelevant"));
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);
        _userRepoMock.Setup(x => x.GetByEmailAsync("oauth@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        await CreateSut().GitHubSignInAsync(new OAuthSignInRequest { AccessToken = "gh-token" });

        _auditSink.Events.Should().Contain(auditEvent =>
            auditEvent.ActionType == "Authentication.ExternalAccountLinked"
            && auditEvent.UserId == existingUser.Id
            && auditEvent.Success);
    }

    [Fact]
    public async Task ProviderFailureJournalsExternalLoginFailedWithoutAUser()
    {
        _oauthServiceMock
            .Setup(x => x.GetUserProfileAsync("github", "bad-token"))
            .ThrowsAsync(new UnauthorizedAccessException("bad provider token"));
        _authAttemptServiceMock
            .Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.Invoking(service => service.GitHubSignInAsync(new OAuthSignInRequest { AccessToken = "bad-token" }))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        var failed = _auditSink.Events.Should().ContainSingle(
            auditEvent => auditEvent.ActionType == "Authentication.ExternalLoginFailed").Subject;
        failed.UserId.Should().BeNull("a failed federated sign-in may resolve no user");
        failed.Success.Should().BeFalse();
        failed.Method.Should().Be("GitHub");
    }

    [Fact]
    public async Task AuditSinkFailureNeverBreaksTheSignIn()
    {
        var user = User.CreateOAuthUser("oauth@example.com", "OAuth User");
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("github", "oauth-sub-default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLogin { Id = Guid.NewGuid(), UserId = user.Id, Provider = "github", ProviderKey = "oauth-sub-default" });
        _userRepoMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateSut(new ThrowingAuthenticationAuditEventSink())
            .GitHubSignInAsync(new OAuthSignInRequest { AccessToken = "gh-token" });

        result.Success.Should().BeTrue("auditing must never break sign-in");
    }

    private sealed class RecordingAuthenticationAuditEventSink : IAuthenticationAuditEventSink
    {
        public List<AuthenticationAuditEvent> Events { get; } = [];

        public Task RecordAsync(AuthenticationAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAuthenticationAuditEventSink : IAuthenticationAuditEventSink
    {
        public Task RecordAsync(AuthenticationAuditEvent auditEvent, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("Synthetic audit sink failure."));
    }
}
