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
///     Compliance gate cross-tenant evaluation (issue #1129): the gate evaluates for the
///     tenant the sign-in actually targets — resolved from the user's memberships and the
///     request's tenant context — never from a caller-asserted or stale value. Therefore a
///     compliance decision rendered for tenant A (an allow) cannot be replayed to reach
///     tenant B: signing in <i>to B</i> evaluates B's own compliance state, and B's deny
///     blocks sign-in even though A was allowed moments earlier. Tenants the user has no
///     active membership in are refused before the gate even runs.
/// </summary>
public sealed class SignInComplianceGateCrossTenantTests
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
    private readonly Mock<ISignInCompliancePolicy> _compliancePolicyMock = new();
    private readonly Mock<ISignInMfaService> _signInMfaMock = SignInMfaPreparationStub.CreateMock();

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _foreignTenant = Guid.NewGuid();
    private readonly User _user = User.CreateWithPassword(
        "crosstenant@example.com", "crosstenant", BCrypt.Net.BCrypt.HashPassword("Password1!"));

    public SignInComplianceGateCrossTenantTests()
    {
        PersistedAuthenticationSessions.Configure(_sessionManagementServiceMock);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "CrossTenantGateAgent/1.0";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("198.51.100.44");
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
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");
        _userRepoMock.Setup(x => x.GetByEmailAsync("crosstenant@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _userRepoMock.Setup(x => x.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);

        // The user holds active memberships in BOTH tenant A and tenant B; the foreign
        // tenant has no membership. A is the default tenant.
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetUserMembershipsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserMembershipsResponse
            {
                TotalCount = 2,
                Memberships =
                [
                    Membership(_tenantA, "Tenant A", isDefault: true),
                    Membership(_tenantB, "Tenant B", isDefault: false)
                ]
            });
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Per-tenant compliance state: A is compliant (allow), B is on hold (deny).
        _compliancePolicyMock
            .Setup(policy => policy.EvaluateAsync(It.IsAny<SignInComplianceContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SignInComplianceContext context, CancellationToken _) =>
                context.TenantId == _tenantA
                    ? SignInComplianceDecision.Allow
                    : SignInComplianceDecision.Deny(SignInComplianceReasons.ComplianceHold));
    }

    private static UserMembershipDto Membership(Guid tenantId, string name, bool isDefault) => new()
    {
        TenantId = tenantId,
        TenantName = name,
        TenantSlug = name.ToLowerInvariant().Replace(' ', '-'),
        TenantIsActive = true,
        TenantIsDefault = isDefault,
        Role = "Member",
        IsActive = true
    };

    private LocalAuthService CreateSut() =>
        new(
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
            complianceGateOptions: Options.Create(new SignInComplianceGateOptions
            {
                Enabled = true,
                Mode = SignInComplianceGateMode.Enforce
            }),
            compliancePolicy: _compliancePolicyMock.Object);

    private LocalSignInRequest RequestFor(Guid? tenantId) => new()
    {
        Email = "crosstenant@example.com",
        Password = "Password1!",
        TenantId = tenantId,
        DeviceFingerprint = "cross-tenant-device"
    };

    [Fact]
    public async Task GateEvaluatesForTheRequestedTenant_NotTheUserDefaultTenant()
    {
        SignInComplianceContext? captured = null;
        _compliancePolicyMock
            .Setup(policy => policy.EvaluateAsync(It.IsAny<SignInComplianceContext>(), It.IsAny<CancellationToken>()))
            .Callback((SignInComplianceContext context, CancellationToken _) => captured = context)
            .ReturnsAsync(SignInComplianceDecision.Allow);

        // Sign-in explicitly targets tenant B (the user's default is A).
        var result = await CreateSut().LocalSignInAsync(RequestFor(_tenantB));

        result.Success.Should().BeTrue();
        result.TenantId.Should().Be(_tenantB);
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(_tenantB,
            "the compliance decision must be rendered for the tenant being signed in to, not the default tenant");
    }

    [Fact]
    public async Task AllowForTenantA_DoesNotAuthorizeSignInToTenantB()
    {
        var sut = CreateSut();

        var intoA = await sut.LocalSignInAsync(RequestFor(_tenantA));

        intoA.Success.Should().BeTrue("tenant A is compliant: sign-in proceeds");
        await FluentActions.Awaiting(() => sut.LocalSignInAsync(RequestFor(_tenantB)))
            .Should().ThrowAsync<AccessDeniedException>(
                "tenant B is on compliance hold: the earlier allow for A cannot be replayed for B");
    }

    [Fact]
    public async Task TenantBSignInDeniedByCompliance_IssuesNoSessionAndNoCredentials()
    {
        var sut = CreateSut();

        await FluentActions.Awaiting(() => sut.LocalSignInAsync(RequestFor(_tenantB)))
            .Should().ThrowAsync<AccessDeniedException>();

        _jwtTokenServiceMock.Verify(
            tokens => tokens.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _jwtTokenServiceMock.Verify(
            tokens => tokens.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TenantWithoutMembership_IsRefusedBeforeTheComplianceGate()
    {
        var sut = CreateSut();

        // The user is not a member of the foreign tenant at all: tenant access fails
        // closed before any compliance evaluation could even be consulted.
        await FluentActions.Awaiting(() => sut.LocalSignInAsync(RequestFor(_foreignTenant)))
            .Should().ThrowAsync<AccessDeniedException>();

        _compliancePolicyMock.Verify(
            policy => policy.EvaluateAsync(It.IsAny<SignInComplianceContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OmittingTheTenant_DoesNotFallBackToADeniedTenant()
    {
        // A caller cannot "forget" the tenant to escape B's hold: without a tenant the
        // resolver selects the user's default tenant (A here) — never a denied one.
        var result = await CreateSut().LocalSignInAsync(RequestFor(tenantId: null));

        result.Success.Should().BeTrue();
        result.TenantId.Should().Be(_tenantA);
    }

    [Fact]
    public async Task ComplianceAuditForDeniedTenantB_CarriesTenantBNotTenantA()
    {
        var auditSink = new Mock<IAuthenticationAuditEventSink>();
        auditSink
            .Setup(sink => sink.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LocalAuthService(
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
            auditEventSink: auditSink.Object,
            complianceGateOptions: Options.Create(new SignInComplianceGateOptions { Enabled = true, Mode = SignInComplianceGateMode.Enforce }),
            compliancePolicy: _compliancePolicyMock.Object);

        await FluentActions.Awaiting(() => sut.LocalSignInAsync(RequestFor(_tenantB)))
            .Should().ThrowAsync<AccessDeniedException>();

        // The denial must be audited against the tenant that was denied (B) so the hold
        // is attributed to the right compliance regime — not to the allowed tenant A.
        auditSink.Verify(
            sink => sink.RecordAsync(
                It.Is<AuthenticationAuditEvent>(audit =>
                    audit.ActionType == "Authentication.ComplianceDenied" &&
                    audit.TenantId == _tenantB &&
                    audit.ErrorMessage == SignInComplianceReasons.ComplianceHold),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChallengeOnlyMode_TenantBChallengeDoesNotProduceASessionForEitherTenant()
    {
        _compliancePolicyMock
            .Setup(policy => policy.EvaluateAsync(It.IsAny<SignInComplianceContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SignInComplianceContext context, CancellationToken _) =>
                context.TenantId == _tenantA
                    ? SignInComplianceDecision.Allow
                    : SignInComplianceDecision.Challenge(SignInComplianceReasons.VerificationPending));

        var sut = new LocalAuthService(
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
            complianceGateOptions: Options.Create(new SignInComplianceGateOptions { Enabled = true, Mode = SignInComplianceGateMode.ChallengeOnly }),
            compliancePolicy: _compliancePolicyMock.Object);

        var result = await sut.LocalSignInAsync(RequestFor(_tenantB));

        result.Success.Should().BeFalse();
        result.RequiresStepUp.Should().BeTrue("tenant B demands verification: step-up, never silent access");
        result.TenantId.Should().Be(_tenantB, "the challenge is bound to the tenant that required it");
        _sessionManagementServiceMock.Verify(
            sessions => sessions.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
