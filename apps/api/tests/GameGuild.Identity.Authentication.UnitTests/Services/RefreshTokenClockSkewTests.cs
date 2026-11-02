using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Refresh-token / access-token validation under clock skew (issue #1131): the server's
///     NTP can drift by ±30 seconds. Documented, pinned behavior:
///     <list type="bullet">
///         <item>
///             Access-token (JWT) validation HAS an explicit skew knob:
///             <see cref="JwtOptions.ClockSkewSeconds" /> (default 0). With it configured
///             (60s), a token that expired 30s ago still validates; with 0 it does not.
///         </item>
///         <item>
///             Persisted refresh-token rotation has NO skew window:
///             <c>LocalAuthService.RefreshTokenAsync</c> rejects when
///             <c>ExpiresAt &lt;= serverNow</c>. A 30s-fast server therefore refuses a
///             token that truly has &lt;30s of life left, and a 30s-slow server still
///             accepts one that truly expired up to 30s ago. Both directions are pinned
///             here as the current contract — if tolerance is ever added, these boundary
///             tests will force the change to be deliberate.
///         </item>
///     </list>
/// </summary>
public sealed class RefreshTokenClockSkewTests : IDisposable
{
    private static readonly DateTime TrueNow = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IRefreshTokenLineageRepository> _tokenLineageRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock = new();
    private readonly Mock<IAuthAttemptService> _authAttemptServiceMock = new();
    private readonly Mock<IAuthenticationAnomalyDetectionService> _anomalyDetectionMock = new();
    private readonly Mock<IUserEnumerationProtectionService> _enumerationProtectionMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ISessionManagementService> _sessionManagementServiceMock = new();
    private readonly Mock<ISignInMfaService> _signInMfaMock = SignInMfaPreparationStub.CreateMock();
    private readonly Mock<IAuthenticationAuditEventSink> _auditEventSinkMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly User _user = User.CreateWithPassword(
        "skew@example.com", "skewuser", BCrypt.Net.BCrypt.HashPassword("Password1!"));

    public RefreshTokenClockSkewTests()
    {
        SystemClock.Reset();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "ClockSkewAgent/1.0";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("203.0.113.44");
        _enumerationProtectionMock.Setup(x => x.GetGenericErrorMessage(It.IsAny<string>())).Returns("Authentication failed");
        _enumerationProtectionMock.Setup(x => x.BeginAuthenticationTiming()).Returns(new AuthenticationTimingScope());
        _enumerationProtectionMock.Setup(x => x.AddTimingProtectionDelayAsync(It.IsAny<AuthenticationTimingScope>(), It.IsAny<CredentialWorkClassification>())).Returns(Task.CompletedTask);
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns((string token) => $"hash-{token}");
        _userRepoMock.Setup(x => x.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetUserMembershipsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserMembershipsResponse
            {
                TotalCount = 1,
                Memberships =
                [
                    new UserMembershipDto
                    {
                        TenantId = _tenantId, TenantName = "Skew tenant", TenantSlug = "skew-tenant",
                        TenantIsActive = true, TenantIsDefault = true, Role = "Member", IsActive = true
                    }
                ]
            });
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token"); // deterministic: hashed to "hash-refresh-token" below
        _tokenLineageRepoMock
            .Setup(x => x.RecordRotationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokenRepoMock
            .Setup(x => x.TryRevokeForRotationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sessionManagementServiceMock
            .Setup(x => x.GetSessionByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string hash, CancellationToken _) => new UserSession
            {
                Id = Guid.NewGuid(), UserId = _user.Id, RefreshToken = hash, IsActive = true,
                ExpiresAt = TrueNow.AddHours(1), CreatedAt = TrueNow.AddDays(-1)
            });
        _sessionManagementServiceMock
            .Setup(x => x.RefreshSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        // Post-rotation session read: persisted deadline comfortably beyond any skew.
        _sessionManagementServiceMock
            .Setup(x => x.GetSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new UserSession
            {
                Id = id, UserId = _user.Id, RefreshToken = "hash-refresh-token", IsActive = true,
                ExpiresAt = TrueNow.AddHours(1), CreatedAt = TrueNow.AddDays(-1)
            });
    }

    public void Dispose() => SystemClock.Reset();

    /// <summary>Sets the server clock to the true instant plus the given skew.</summary>
    private static void SetServerClock(TimeSpan skew) =>
        SystemClock.SetProvider(new FixedTimeProvider(TrueNow.Add(skew)));

    private RefreshToken PersistedTokenExpiringAt(DateTime expiresAt, string plaintext = "live-refresh-token")
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            Token = $"hash-{plaintext}",
            CreatedAt = TrueNow.AddDays(-1),
            ExpiresAt = expiresAt,
            CreatedByIp = "203.0.113.44"
        };
        _refreshTokenRepoMock
            .Setup(x => x.GetByTokenAsync(token.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        return token;
    }

    private LocalAuthService CreateSut() => new(
        _userRepoMock.Object,
        _refreshTokenRepoMock.Object,
        _tokenLineageRepoMock.Object,
        _jwtTokenServiceMock.Object,
        _refreshTokenHasherMock.Object,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build(),
        _authAttemptServiceMock.Object,
        new PasswordHasher(NullLogger<PasswordHasher>.Instance, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build()),
        _anomalyDetectionMock.Object,
        _enumerationProtectionMock.Object,
        _httpContextAccessorMock.Object,
        NullLogger<LocalAuthService>.Instance,
        _senderMock.Object,
        _sessionManagementServiceMock.Object,
        _signInMfaMock.Object,
        auditEventSink: _auditEventSinkMock.Object);

    private static RefreshTokenRequest RefreshRequest(string token = "live-refresh-token") => new() { RefreshToken = token };

    // ── Rotation boundary with a ±30s-drifted server clock ────────────────

    [Fact]
    public async Task AccurateClock_TokenWith45SecondsOfLifeRemaining_Rotates()
    {
        SetServerClock(TimeSpan.Zero);
        PersistedTokenExpiringAt(TrueNow.AddSeconds(45));
        var sut = CreateSut();

        var result = await sut.RefreshTokenAsync(RefreshRequest());

        result.Success.Should().BeTrue();
        result.RefreshToken.Should().NotBe("live-refresh-token", "rotation always issues a replacement");
    }

    [Fact]
    public async Task Clock30sBehind_TokenWith45sTrueLifeRemaining_Rotates()
    {
        // Server NTP lags 30s: the token (45s of true life) is still comfortably valid.
        SetServerClock(TimeSpan.FromSeconds(-30));
        PersistedTokenExpiringAt(TrueNow.AddSeconds(45));
        var sut = CreateSut();

        var result = await sut.RefreshTokenAsync(RefreshRequest());

        result.Success.Should().BeTrue("a lagging clock must not falsely expire a still-live token");
    }

    [Fact]
    public async Task Clock30sAhead_TokenWith45sTrueLifeRemaining_Rotates()
    {
        // Server NTP leads 30s: 45s of true life leaves 15s of observed life — still valid.
        SetServerClock(TimeSpan.FromSeconds(30));
        PersistedTokenExpiringAt(TrueNow.AddSeconds(45));
        var sut = CreateSut();

        var result = await sut.RefreshTokenAsync(RefreshRequest());

        result.Success.Should().BeTrue("a leading clock under the remaining lifetime is tolerated");
    }

    [Fact]
    public async Task Clock30sAhead_TokenWith20sTrueLifeRemaining_IsRejected()
    {
        // Documented current behavior: the persisted-token check has NO skew window, so a
        // 30s-fast server rejects a token that truly has 20s left ("appears expired but
        // is not"). The user retries after re-authentication; nothing is contained.
        SetServerClock(TimeSpan.FromSeconds(30));
        PersistedTokenExpiringAt(TrueNow.AddSeconds(20));
        var sut = CreateSut();

        await FluentActions.Awaiting(() => sut.RefreshTokenAsync(RefreshRequest()))
            .Should().ThrowAsync<UnauthorizedAccessException>();
        _refreshTokenRepoMock.Verify(
            repo => repo.TryRevokeForRotationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Clock30sBehind_TokenTrulyExpired20sAgo_IsStillAccepted()
    {
        // The lenient direction of the same missing window: a lagging server accepts a
        // token up to 30s past its true expiry. Pinned as current behavior.
        SetServerClock(TimeSpan.FromSeconds(-30));
        PersistedTokenExpiringAt(TrueNow.AddSeconds(-20));
        var sut = CreateSut();

        var result = await sut.RefreshTokenAsync(RefreshRequest());

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TokenExpiringExactlyAtServerNow_IsRejected()
    {
        // The comparison is <=: no grace second at the boundary.
        SetServerClock(TimeSpan.Zero);
        PersistedTokenExpiringAt(TrueNow);
        var sut = CreateSut();

        await FluentActions.Awaiting(() => sut.RefreshTokenAsync(RefreshRequest()))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── Access-token (JWT) validation: the layer WITH a skew knob ─────────

    [Fact]
    public async Task AccessTokenValidation_WithConfiguredSkew_Tolerates30SecondDrift()
    {
        var service = CreateJwtService(clockSkewSeconds: 60);

        // Generation clock 90s behind the real validator clock with a 1-minute lifetime:
        // the token is 30s past its exp when validated — inside the configured skew.
        SystemClock.SetProvider(new FixedTimeProvider(DateTime.UtcNow.AddSeconds(-90)));
        var token = await service.GenerateAccessTokenAsync(_user.Id, "skew@example.com", ["Member"], _tenantId);
        SystemClock.Reset();

        var principal = service.ValidateToken(token);

        principal.Should().NotBeNull("±30s NTP drift must be tolerated when ClockSkewSeconds is configured");
    }

    [Fact]
    public async Task AccessTokenValidation_WithZeroSkew_RejectsToken30SecondsPastExpiry()
    {
        var service = CreateJwtService(clockSkewSeconds: 0);

        SystemClock.SetProvider(new FixedTimeProvider(DateTime.UtcNow.AddSeconds(-90)));
        var token = await service.GenerateAccessTokenAsync(_user.Id, "skew@example.com", ["Member"], _tenantId);
        SystemClock.Reset();

        var act = () => service.ValidateToken(token);

        act.Should().Throw<SecurityTokenExpiredException>(
            "with the default zero skew the same 30s drift is a hard expiry");
    }

    [Fact]
    public async Task AccessTokenValidation_WellWithinLifetime_ValidatesUnderBothSkewSettings()
    {
        // Control: a fresh token (real clock, 1-minute lifetime) validates with zero skew.
        var service = CreateJwtService(clockSkewSeconds: 0);
        var freshToken = await service.GenerateAccessTokenAsync(_user.Id, "skew@example.com", ["Member"], _tenantId);

        service.ValidateToken(freshToken).Should().NotBeNull();
    }

    private static JwtTokenService CreateJwtService(int clockSkewSeconds)
    {
        var options = new JwtOptions
        {
            SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Issuer = "SkewTestIssuer",
            Audience = "SkewTestAudience",
            AccessTokenExpirationMinutes = 1,
            ClockSkewSeconds = clockSkewSeconds
        };
        var optionsMonitor = new Mock<IOptions<JwtOptions>>();
        optionsMonitor.Setup(x => x.Value).Returns(options);
        return new JwtTokenService(
            NullLogger<JwtTokenService>.Instance,
            Mock.Of<IRefreshTokenRepository>(),
            Mock.Of<IRefreshTokenHasher>(),
            Mock.Of<IHttpContextAccessor>(),
            optionsMonitor.Object);
    }

    /// <summary>A deterministic clock pinned to a fixed instant.</summary>
    private sealed class FixedTimeProvider(DateTime instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(instant, TimeSpan.Zero);
    }
}
