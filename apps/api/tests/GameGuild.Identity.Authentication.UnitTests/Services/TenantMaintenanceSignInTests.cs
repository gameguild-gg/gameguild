using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Sign-in during tenant maintenance mode (issue #1134): once a tenant's maintenance
///     window is open, non-admin sign-ins are refused with the typed
///     <see cref="TenantMaintenanceException" /> that the controllers map to 503 +
///     Retry-After, while tenant Owner/Admin members and platform SystemAdmins still
///     authenticate (they must be able to verify the tenant before reopening it). The gate
///     runs after tenant resolution with the first factor verified, fails open when the
///     maintenance store is unavailable, and is inert when no window is set.
/// </summary>
public sealed class TenantMaintenanceSignInTests
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
    private readonly Mock<ITenantMaintenanceStateService> _maintenanceMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly User _user = User.CreateWithPassword(
        "maint@example.com", "maintuser", BCrypt.Net.BCrypt.HashPassword("Password1!"));

    public TenantMaintenanceSignInTests()
    {
        PersistedAuthenticationSessions.Configure(_sessionManagementServiceMock);

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(() =>
            {
                var httpContext = new DefaultHttpContext();
                httpContext.Request.Headers.UserAgent = "MaintenanceTest/1.0";
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

        _userRepoMock.Setup(x => x.GetByEmailAsync("maint@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _userRepoMock.Setup(x => x.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, string _, string[] _, Guid? _, int _, Guid sessionId, CancellationToken _) => $"access-{sessionId:N}");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"refresh-{Guid.NewGuid():N}");

        // No maintenance window by default; individual tests open one.
        _maintenanceMock
            .Setup(x => x.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMaintenanceState?)null);
    }

    private void MemberHasRole(string role)
    {
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
                        TenantName = "Maintenance tenant",
                        TenantSlug = "maintenance-tenant",
                        TenantIsActive = true,
                        TenantIsDefault = true,
                        Role = role,
                        IsActive = true
                    }
                ]
            });
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
    }

    private void MaintenanceWindowOpen(DateTimeOffset? resumesAt = null) =>
        _maintenanceMock
            .Setup(x => x.FindAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantMaintenanceState
            {
                TenantId = _tenantId,
                Reason = "database migration",
                OpenedBy = "ops@example.com",
                OpenedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                ResumesAt = resumesAt
            });

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
            _signInMfaMock.Object,
            tenantMaintenanceState: _maintenanceMock.Object);
    }

    private LocalSignInRequest ValidRequest => new()
    {
        Email = "maint@example.com",
        Password = "Password1!",
        DeviceFingerprint = "maintenance-device"
    };

    // ── The issue's scenario: maintenance mode → regular user 503, admin 200 ─────────

    [Fact]
    public async Task MaintenanceMode_RegularMember_SignInRefusedWith503Exception()
    {
        MemberHasRole("Member");
        MaintenanceWindowOpen(resumesAt: SystemClock.UtcNow.AddMinutes(10));

        var act = () => CreateSut().LocalSignInAsync(ValidRequest);

        // The typed refusal the controllers map to 503 — never a generic 401/500.
        var exception = (await act.Should().ThrowAsync<TenantMaintenanceException>())
            .Which;
        exception.TenantId.Should().Be(_tenantId);
        exception.RetryAfterSeconds.Should().NotBeNull();
        exception.RetryAfterSeconds.Should().BeInRange(590, 610, "Retry-After derives from the scheduled resume time");
        exception.Message.Should().Contain("maintenance", "the error is explicit about the cause");
    }

    [Fact]
    public async Task MaintenanceMode_TenantAdmin_SignInSucceeds()
    {
        MemberHasRole("Admin");
        MaintenanceWindowOpen();

        var response = await CreateSut().LocalSignInAsync(ValidRequest);

        response.Success.Should().BeTrue("tenant admins keep access during maintenance so they can verify the tenant");
        response.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    public async Task MaintenanceMode_TenantAdministrators_SignInSucceeds(string role)
    {
        MemberHasRole(role);
        MaintenanceWindowOpen();

        var response = await CreateSut().LocalSignInAsync(ValidRequest);

        response.Success.Should().BeTrue($"the {role} role is a maintenance bypass for its own tenant");
    }

    [Fact]
    public async Task MaintenanceMode_SystemAdmin_SignInSucceeds()
    {
        MemberHasRole("SystemAdmin");

        var response = await CreateSut().LocalSignInAsync(ValidRequest);

        response.Success.Should().BeTrue("the platform SystemAdmin role is a maintenance bypass");
    }

    [Fact]
    public async Task NoMaintenanceWindow_SignInSucceeds()
    {
        MemberHasRole("Member");
        // Default mock: no window set.

        var response = await CreateSut().LocalSignInAsync(ValidRequest);

        response.Success.Should().BeTrue("without a window the gate is inert");
    }

    [Fact]
    public async Task MaintenanceStoreUnavailable_GateFailsOpen()
    {
        MemberHasRole("Member");
        _maintenanceMock
            .Setup(x => x.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cache unreachable"));

        var response = await CreateSut().LocalSignInAsync(ValidRequest);

        response.Success.Should().BeTrue("the maintenance gate must never become a sign-in outage of its own");
    }

    [Fact]
    public async Task OpenEndedWindow_NoRetryAfterHint()
    {
        MemberHasRole("Member");
        MaintenanceWindowOpen(resumesAt: null);

        var act = () => CreateSut().LocalSignInAsync(ValidRequest);

        (await act.Should().ThrowAsync<TenantMaintenanceException>())
            .Which.RetryAfterSeconds.Should().BeNull("an open-ended window gives no false resume promise");
    }

    // ── The controller mapping: 503 with Retry-After for members, 200 for admins ─────

    [Fact]
    public async Task Controller_MaintenanceException_Answers503WithRetryAfterHeader()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ICommand<SignInResponse>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TenantMaintenanceException(Guid.NewGuid(), retryAfterSeconds: 120));
        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.LocalSignIn(new LocalSignInRequest
        {
            Email = "maint@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        controller.Response.Headers.RetryAfter.ToString().Should().Be("120");
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Contain("maintenance");
        problem.Extensions["errorCode"].Should().Be("tenant_in_maintenance");
    }

    [Fact]
    public async Task Controller_AdminSignIn_DuringMaintenance_Answers200()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ICommand<SignInResponse>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResponse { Success = true, SessionId = Guid.NewGuid() });
        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.LocalSignIn(new LocalSignInRequest
        {
            Email = "admin@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        controller.Response.Headers.RetryAfter.ToString().Should().BeEmpty("no retry hint on a successful sign-in");
    }

    [Fact]
    public async Task Controller_GuestSessionExpired_SignalsThroughHeader()
    {
        // Issue #1128's wire surface: the body contract is unchanged, the signal rides the
        // X-Guest-Session-Expired response header.
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ICommand<SignInResponse>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResponse { Success = true, SessionId = Guid.NewGuid(), GuestSessionExpired = true });
        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        await controller.LocalSignIn(new LocalSignInRequest
        {
            Email = "guest-grad@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        controller.Response.Headers["X-Guest-Session-Expired"].ToString().Should().Be("true");
    }
}
