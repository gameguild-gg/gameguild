using System.Collections.Concurrent;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Concurrent sign-in race condition (issue #1126): two simultaneous sign-in requests
///     for the same user. Each request must produce its own independent, internally
///     consistent authentication result — a distinct session id, a distinct refresh-token
///     lineage, and credentials bound to that request's session only. Shared per-user state
///     (the account row, MFA preparation) is read-only during issuance, so interleaving the
///     two flows must never cross wires: no shared session id, no duplicated refresh-token
///     hash, no response observing the other request's session. Rotation races on a shared
///     refresh token are handled atomically by
///     <see cref="IRefreshTokenRepository.TryRevokeForRotationAsync" /> (one winner, the
///     replay is contained) — these tests pin the sign-in-time invariant that makes that
///     possible: two sessions never share a refresh-token hash in the first place.
/// </summary>
public sealed class ConcurrentSignInRaceConditionTests
{
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

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly User _user = User.CreateWithPassword(
        "race@example.com", "raceuser", BCrypt.Net.BCrypt.HashPassword("Password1!"));

    // Barrier makes both requests enter the authenticated core simultaneously instead of
    // racing with scheduling noise — the two flows genuinely overlap from tenant
    // resolution through session creation.
    private Barrier? _barrier;

    public ConcurrentSignInRaceConditionTests()
    {
        PersistedAuthenticationSessions.Configure(_sessionManagementServiceMock);

        // A FRESH HttpContext per access: the two concurrent sign-ins must never share
        // request state (HttpContext.Items is not thread-safe; each request owns its own).
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(() =>
            {
                var httpContext = new DefaultHttpContext();
                httpContext.Request.Headers.UserAgent = "RaceTestAgent/1.0";
                return httpContext;
            });

        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("203.0.113.9");
        _enumerationProtectionMock.Setup(x => x.GetGenericErrorMessage(It.IsAny<string>())).Returns("Authentication failed");
        _enumerationProtectionMock.Setup(x => x.BeginAuthenticationTiming()).Returns(new AuthenticationTimingScope());
        _enumerationProtectionMock.Setup(x => x.AddTimingProtectionDelayAsync(It.IsAny<AuthenticationTimingScope>(), It.IsAny<CredentialWorkClassification>())).Returns(Task.CompletedTask);
        _anomalyDetectionMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new AuthenticationAnomalyResult { RiskLevel = RiskLevel.Low });
        _anomalyDetectionMock
            .Setup(x => x.AnalyzeBehavioralPatternsAsync(It.IsAny<Guid>(), It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new BehavioralAnalysisResult { MatchesTypicalBehavior = true });
        _anomalyDetectionMock
            .Setup(x => x.RecordSuspiciousActivityAsync(It.IsAny<SuspiciousActivity>()))
            .Returns(Task.CompletedTask);
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns((string token) => $"hash-{token}");

        _userRepoMock.Setup(x => x.GetByEmailAsync("race@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(_user);
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
                        TenantId = _tenantId,
                        TenantName = "Race tenant",
                        TenantSlug = "race-tenant",
                        TenantIsActive = true,
                        TenantIsDefault = true,
                        Role = "Member",
                        IsActive = true
                    }
                ]
            });
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Distinct credentials per issuance, like the real token service: a shared
        // constant here would hide exactly the collision this test must expose.
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, string email, string[] roles, Guid? tenantId, int version, Guid sessionId, CancellationToken _) => $"access-{sessionId:N}");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"refresh-{Guid.NewGuid():N}");
    }

    private LocalAuthService CreateSut()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var passwordHasher = new PasswordHasher(NullLogger<PasswordHasher>.Instance, configuration);

        return new LocalAuthService(
            _userRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _tokenLineageRepoMock.Object,
            _jwtTokenServiceMock.Object,
            _refreshTokenHasherMock.Object,
            configuration,
            _authAttemptServiceMock.Object,
            passwordHasher,
            _anomalyDetectionMock.Object,
            _enumerationProtectionMock.Object,
            _httpContextAccessorMock.Object,
            NullLogger<LocalAuthService>.Instance,
            _senderMock.Object,
            _sessionManagementServiceMock.Object,
            _signInMfaMock.Object);
    }

    private LocalSignInRequest ValidRequest => new()
    {
        Email = "race@example.com",
        Password = "Password1!",
        DeviceFingerprint = "race-device-fingerprint"
    };

    private async Task<(SignInResponse First, SignInResponse Second)> SignInConcurrentlyAsync()
    {
        var sut = CreateSut();
        // Both requests pass the barrier inside anomaly analysis (post-credential),
        // overlapping the tenant-resolution → session-creation critical region.
        _barrier = new Barrier(2);
        _anomalyDetectionMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .Returns(async (AuthenticationAttemptContext _) =>
            {
                _barrier.SignalAndWait(TimeSpan.FromSeconds(30));
                return new AuthenticationAnomalyResult { RiskLevel = RiskLevel.Low };
            });

        var first = Task.Run(() => sut.LocalSignInAsync(ValidRequest));
        var second = Task.Run(() => sut.LocalSignInAsync(ValidRequest));
        var results = await Task.WhenAll(first, second);

        return (results[0], results[1]);
    }

    [Fact]
    public async Task TwoSimultaneousSignInRequests_BothSucceedWithDistinctSessions()
    {
        var (first, second) = await SignInConcurrentlyAsync();

        first.Success.Should().BeTrue("the first concurrent sign-in must complete on its own terms");
        second.Success.Should().BeTrue("the second concurrent sign-in must complete on its own terms");
        first.SessionId.Should().NotBe(second.SessionId, "each request must mint its own session");
        first.SessionId.Should().NotBe(Guid.Empty);
        second.SessionId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task TwoSimultaneousSignInRequests_IssueDistinctRefreshTokens()
    {
        var (first, second) = await SignInConcurrentlyAsync();

        first.RefreshToken.Should().NotBeNullOrEmpty();
        second.RefreshToken.Should().NotBeNullOrEmpty();
        first.RefreshToken.Should().NotBe(second.RefreshToken,
            "a shared refresh token between sessions would corrupt later rotation: each lineage must be independent");
    }

    [Fact]
    public async Task TwoSimultaneousSignInRequests_PersistBothSessionsWithDistinctTokenHashes()
    {
        var (first, second) = await SignInConcurrentlyAsync();

        var hashes = new[]
        {
            $"hash-{first.RefreshToken}",
            $"hash-{second.RefreshToken}"
        };
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(
                first.SessionId, _user.Id, It.IsAny<string>(), It.IsAny<string>(), hashes[0], It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(
                second.SessionId, _user.Id, It.IsAny<string>(), It.IsAny<string>(), hashes[1], It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        hashes[0].Should().NotBe(hashes[1], "the persisted session rows must bind different refresh-token hashes");

        var persistedFirst = await _sessionManagementServiceMock.Object.GetSessionAsync(first.SessionId);
        var persistedSecond = await _sessionManagementServiceMock.Object.GetSessionAsync(second.SessionId);
        persistedFirst.Should().NotBeNull();
        persistedSecond.Should().NotBeNull();
        persistedFirst!.Id.Should().NotBe(persistedSecond!.Id);
        persistedFirst.RefreshToken.Should().NotBe(persistedSecond.RefreshToken);
    }

    [Fact]
    public async Task TwoSimultaneousSignInRequests_EachAccessTokenIsBoundToItsOwnSession()
    {
        var (first, second) = await SignInConcurrentlyAsync();

        first.AccessToken.Should().Be($"access-{first.SessionId:N}",
            "credentials must be bound to the session minted by the same request");
        second.AccessToken.Should().Be($"access-{second.SessionId:N}");
        first.AccessToken.Should().NotBe(second.AccessToken);
    }

    [Fact]
    public async Task TwoSimultaneousSignInRequests_ShareNoCrossContaminatedResponseState()
    {
        var (first, second) = await SignInConcurrentlyAsync();

        foreach (var result in new[] { first, second })
        {
            result.UserId.Should().Be(_user.Id, "both requests authenticate the same account");
            result.TenantId.Should().Be(_tenantId, "both requests resolve the same tenant membership");
            result.Success.Should().BeTrue();
        }

        // Successful-attempt accounting stays per-request: exactly two recorded successes.
        _authAttemptServiceMock.Verify(
            attempts => attempts.RecordSuccessfulAttemptAsync("race@example.com", _user.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task TwoSimultaneousSignInRequests_PrepareMfaIndependentlyPerRequest()
    {
        var (first, second) = await SignInConcurrentlyAsync();

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();

        // MFA preparation runs once per request (subject + resolved tenant), never once
        // for both — a shared preparation would leak one request's factor state into the other.
        _signInMfaMock.Verify(
            mfa => mfa.PrepareAsync(_user.Id, It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<DeviceInfo>(), It.IsAny<SignInFirstFactor>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ManySimultaneousSignInRequests_AllSessionsRemainDistinct()
    {
        // Widen the race: 8 parallel sign-ins for the same account.
        const int degreeOfParallelism = 8;
        var sut = CreateSut();
        _barrier = new Barrier(degreeOfParallelism);
        _anomalyDetectionMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .Returns(async (AuthenticationAttemptContext _) =>
            {
                _barrier.SignalAndWait(TimeSpan.FromSeconds(30));
                return new AuthenticationAnomalyResult { RiskLevel = RiskLevel.Low };
            });

        var responses = await Task.WhenAll(Enumerable.Range(0, degreeOfParallelism)
            .Select(_ => Task.Run(() => sut.LocalSignInAsync(ValidRequest))));

        responses.Should().OnlyContain(response => response.Success);
        var sessionIds = responses.Select(response => response.SessionId).ToHashSet();
        var refreshTokens = responses.Select(response => response.RefreshToken).ToHashSet();
        sessionIds.Should().HaveCount(degreeOfParallelism, "every concurrent request gets a unique session");
        refreshTokens.Should().HaveCount(degreeOfParallelism, "every concurrent request gets a unique refresh token lineage");
    }
}
