using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Regression pin for the admin-reset lockout: a successful
///     <see cref="AdminResetUserMfaCommandHandler" /> run leaves the subject unenrolled, and the
///     next sign-in preparation must offer a validly bound EnrollFactor challenge instead of a
///     permanently deniable bare one.
/// </summary>
public sealed class MfaResetRecoveryPathTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<IMfaAttemptTrackingService> _tracking = new();
    private readonly Mock<IMfaRecoveryRequestStore> _store = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Mock<ITenantSecurityVersionStore> _versionStore = new();
    private readonly Mock<IPermissionAuditService> _audit = new();

    private readonly Guid _targetUserId = Guid.NewGuid();

    [Fact]
    public async Task AdminResetIsFollowedByAValidBoundEnrollFactorChallengeOnTheNextSignIn()
    {
        SetupActor(isSystemAdmin: true, tenantId: TenantId);
        SetupTargetUser();
        _tracking.Setup(t => t.DisableMfaAsync(_targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var reset = await CreateHandler().Handle(new AdminResetUserMfaCommand { UserId = _targetUserId }, CancellationToken.None);

        reset.Success.Should().BeTrue();
        reset.MfaWasEnabled.Should().BeTrue();

        var fixture = new ResetSignInFixture(_targetUserId, TenantId);
        var response = await fixture.BeginAsync();

        response.RequiresMfa.Should().BeTrue();
        response.MfaToken.Should().NotBeNullOrEmpty();
        var challenge = fixture.Stored.Should().ContainSingle().Which;
        challenge.Purpose.Should().Be(SignInMfaPurpose.EnrollFactor);
        challenge.EnrollmentConfigurationId.Should().Be(fixture.EnrollmentSetup.ConfigurationId);
        challenge.EnrollmentSecretFingerprint.Should().Be(fixture.EnrollmentSetup.SecretFingerprint);
        SignInMfaChallengeToken.IsDigest(challenge.EnrollmentSecretFingerprint!).Should().BeTrue();
        Assert.InRange(challenge.EnrollmentInitializedAt!.Value, challenge.CreatedAt, challenge.ExpiresAt);
        Assert.True(challenge.EnrollmentInitializedAt.Value < challenge.ExpiresAt);
        response.MfaEnrollment.Should().NotBeNull();
        response.MfaEnrollment!.SecretKey.Should().Be(fixture.EnrollmentSetup.SecretKey);
        response.MfaEnrollment.QrCodeUri.Should().Be(fixture.EnrollmentSetup.QrCodeUri);
        response.MfaEnrollment.ExpiresAt.Should().Be(fixture.EnrollmentSetup.SetupExpiresAt);
        var validate = () => challenge.ValidateForPersistence();
        validate.Should().NotThrow();
    }

    private AdminResetUserMfaCommandHandler CreateHandler() =>
        new(_tracking.Object, _store.Object, _users.Object, _actorAccessor.Object, _versionStore.Object,
            _audit.Object, NullLogger<AdminResetUserMfaCommandHandler>.Instance);

    private void SetupTargetUser()
    {
        var user = new User { Id = _targetUserId, Email = "reset-admin@test.com", Username = "reset-admin" };
        user.SetTenantId(TenantId);
        _users.Setup(r => r.GetByIdAsync(_targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    private void SetupActor(bool isAuthenticated = true, bool isSystemAdmin = false, Guid? tenantId = null)
    {
        var roles = new HashSet<string>();
        if (isSystemAdmin) { roles.Add("SystemAdmin"); }

        _actorAccessor.SetupGet(a => a.ActorContext).Returns(ActorContext.Anonymous with
        {
            ActorKind = ActorKind.User,
            IsAuthenticated = isAuthenticated,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId ?? TenantId,
            Roles = roles,
            Permissions = new HashSet<string>()
        });
    }

    private sealed class ResetSignInFixture
    {
        private readonly Guid _subjectId;
        private readonly Guid _tenantId;
        private readonly Mock<ISignInMfaSubjectReader> _subjects = new(MockBehavior.Strict);
        private readonly Mock<IMfaSubjectRequirementPolicy> _policy = new(MockBehavior.Strict);
        private readonly Mock<ISignInMfaChallengeStore> _challenges = new(MockBehavior.Strict);
        private readonly Mock<IMfaService> _mfa = new(MockBehavior.Strict);
        private readonly Mock<IAuthenticatedSessionIssuer> _issuer = new(MockBehavior.Strict);
        private readonly Mock<IAuthenticationAuditEventSink> _audit = new(MockBehavior.Strict);
        private readonly Mock<ISessionMfaEvidenceStore> _sessionEvidence = new(MockBehavior.Strict);
        private readonly Mock<ISignInMfaEnrollmentPort> _enrollment = new(MockBehavior.Strict);
        private readonly Mock<IMfaRecoveryRequestStore> _recoveryRequests = new(MockBehavior.Strict);

        public List<SignInMfaChallenge> Stored { get; } = [];
        public SignInMfaEnrollmentSetup EnrollmentSetup { get; } =
            new(Guid.NewGuid(), Digest(), "post-reset-enrollment-secret", "otpauth://totp/post-reset-enrollment",
                DateTimeOffset.UtcNow.AddMinutes(5));

        public ResetSignInFixture(Guid subjectId, Guid tenantId)
        {
            _subjectId = subjectId;
            _tenantId = tenantId;
            var user = new User { Id = subjectId, TokenVersion = 3, Email = "reset-admin@example.test", Version = 1 };
            _subjects.Setup(port => port.ReadCurrentAsync(subjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SignInMfaSubjectState(user, HasEnrolledMfa: false, LockedOutUntil: null));
            _policy.Setup(port => port.EvaluateAsync(subjectId, tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MfaRequirementDecision(subjectId, tenantId, true, ["Member"], Digest(), RequiredByDefault: true));
            _enrollment.Setup(port => port.AcquireSubjectLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _enrollment.Setup(port => port.StartOrResumeAsync(subjectId, user.Email, It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(EnrollmentSetup);
            _challenges.Setup(port => port.AddAsync(It.IsAny<SignInMfaChallenge>(), It.IsAny<CancellationToken>()))
                .Returns((SignInMfaChallenge challenge, CancellationToken cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    challenge.ValidateForPersistence();
                    Stored.Add(challenge);
                    return Task.CompletedTask;
                });
            _audit.Setup(port => port.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public Task<SignInResponse> BeginAsync() =>
            new SignInMfaService(_subjects.Object, _policy.Object, _challenges.Object, _mfa.Object, _issuer.Object,
                _audit.Object, _sessionEvidence.Object, _enrollment.Object, _recoveryRequests.Object)
                .BeginAsync(_subjectId, 3, _tenantId,
                    new DeviceInfo { Fingerprint = "post-reset", IpAddress = "127.0.0.1", UserAgent = "reset-recovery-test" },
                    SignInFirstFactor.Password, requiresRiskStepUp: false, CancellationToken.None);

        private static string Digest() => Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();
    }
}
