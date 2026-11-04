using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     MFA enrollment during an active session (issue #1127) — EXPECTED BEHAVIOR
///     (documented, then pinned):
///     <list type="number">
///         <item>
///             Enrolling MFA from session A does <b>not</b> terminate the user's other
///             sessions outright — there is no global session revocation on enrollment.
///         </item>
///         <item>
///             Instead, every session that was authenticated <i>without</i> a second factor
///             (session B, issued before enrollment) loses the ability to mint new
///             credentials: <see cref="SignInMfaService.ReadSessionProofAsync" /> throws
///             <see cref="AuthenticationRequiredException" /> for it on the next refresh,
///             forcing a fresh first-factor + MFA sign-in on that device. The session that
///             completed factor verification (session A, carrying stored
///             <see cref="SessionMfaEvidence" />) keeps refreshing normally.
///         </item>
///         <item>
///             While the user has no enrolled factors and no policy requires MFA, a legacy
///             session refreshes without a proof (null = ordinary refresh).
///         </item>
///     </list>
///     Net: enabling MFA invalidates the <i>capability</i> of pre-enrollment sessions to
///     continue (a stolen pre-enrollment refresh token cannot outlive enrollment) without
///     a disruptive global sign-out of the user's other devices.
/// </summary>
public sealed class MfaEnrollmentDuringActiveSessionTests
{
    [Fact]
    public async Task BeforeEnrollment_LegacySessionB_RefreshesWithoutMfaProof()
    {
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = false };

        var proof = await fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionB);

        proof.Should().BeNull("pre-enrollment: an ordinary refresh needs no second factor");
    }

    [Fact]
    public async Task AfterEnrollmentOnSessionA_SessionBWithoutEvidence_IsRejectedOnNextRefresh()
    {
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = true };

        await FluentActions.Awaiting(() => fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionB))
            .Should().ThrowAsync<AuthenticationRequiredException>(
                "enrollment must close the refresh path of sessions that never verified a factor");
    }

    [Fact]
    public async Task AfterEnrollmentOnSessionA_SessionAWithEvidence_ContinuesToRefresh()
    {
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = true };

        var proof = await fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionA);

        proof.Should().NotBeNull("the enrolling session keeps its verified binding and refreshes normally");
    }

    [Fact]
    public async Task AfterEnrollment_EvidenceBoundToAnotherSession_DoesNotAuthorizeSessionB()
    {
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = true };

        // Session B cannot borrow session A's evidence: the stored proof is bound to A's
        // session id, and the binding check rejects a mismatched row.
        var foreignEvidence = fixture.StoredEvidenceFor(MfaEnrollmentSessionFixture.SessionA);
        fixture.SessionEvidence.Setup(port => port.FindAsync(MfaEnrollmentSessionFixture.SessionB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignEvidence);

        await FluentActions.Awaiting(() => fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionB))
            .Should().ThrowAsync<AuthenticationRequiredException>();
    }

    [Fact]
    public async Task AfterEnrollment_EvidenceWithStalePolicyFingerprint_IsRejected()
    {
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = true };

        // The enrollment changed the subject's MFA policy after A's evidence was stored:
        // the row's fingerprint no longer matches the live policy.
        fixture.PolicyFingerprint = MfaEnrollmentSessionFixture.FreshDigest();

        await FluentActions.Awaiting(() => fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionA))
            .Should().ThrowAsync<AuthenticationRequiredException>();
    }

    [Fact]
    public async Task PolicyRequiresMfaWhileUserUnenrolled_SessionRefreshDemandsVerification()
    {
        // Tenant policy mandates MFA but the user has not enrolled yet: even a session
        // with no evidence cannot refresh silently — the gate pushes the user to enroll.
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = false, PolicyRequiresMfa = true };

        await FluentActions.Awaiting(() => fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionB))
            .Should().ThrowAsync<AuthenticationRequiredException>();
    }

    [Fact]
    public async Task EnrollmentTransition_LocksSessionBOutWhileSessionAContinues()
    {
        var fixture = new MfaEnrollmentSessionFixture { Enrolled = false };

        (await fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionB))
            .Should().BeNull("pre-enrollment: ordinary refresh");

        fixture.Enrolled = true; // MFA enabled from session A; A stores its evidence now.

        (await fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionA))
            .Should().NotBeNull("the enrolling session remains usable after enrollment");
        await FluentActions.Awaiting(() => fixture.ReadSessionProofAsync(MfaEnrollmentSessionFixture.SessionB))
            .Should().ThrowAsync<AuthenticationRequiredException>("the pre-enrollment session must re-verify");
    }
}

/// <summary>
///     Two-session fixture for the enrollment-transition tests: session A is the device the
///     user enrolls MFA from (it carries stored <see cref="SessionMfaEvidence" />), session
///     B predates enrollment (no evidence). The subject reader reflects the live enrollment
///     state via <see cref="Enrolled" />.
/// </summary>
public sealed class MfaEnrollmentSessionFixture
{
    public static readonly Guid SessionA = Guid.NewGuid();
    public static readonly Guid SessionB = Guid.NewGuid();

    public Mock<ISignInMfaSubjectReader> Subjects { get; } = new(MockBehavior.Strict);
    public Mock<IMfaSubjectRequirementPolicy> Policy { get; } = new(MockBehavior.Strict);
    public Mock<ISignInMfaChallengeStore> Store { get; } = new(MockBehavior.Strict);
    public Mock<IMfaService> Mfa { get; } = new(MockBehavior.Strict);
    public Mock<IAuthenticatedSessionIssuer> Issuer { get; } = new(MockBehavior.Strict);
    public Mock<IAuthenticationAuditEventSink> Audit { get; } = new(MockBehavior.Strict);
    public Mock<ISessionMfaEvidenceStore> SessionEvidence { get; } = new(MockBehavior.Strict);
    public Mock<IMfaRecoveryRequestStore> RecoveryRequests { get; } = new(MockBehavior.Strict);

    public User User { get; } = new() { Id = Guid.NewGuid(), TokenVersion = 7, Email = "enrollment@example.test", Version = 1 };
    public Guid TenantId { get; } = Guid.NewGuid();
    public string PolicyFingerprint { get; set; } = FreshDigest();
    public bool Enrolled { get; set; }
    public bool PolicyRequiresMfa { get; set; }

    private SessionMfaEvidence StoredEvidenceA { get; }

    public MfaEnrollmentSessionFixture()
    {
        Subjects.Setup(port => port.ReadCurrentAsync(User.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new SignInMfaSubjectState(User, Enrolled, null));
        Policy.Setup(port => port.EvaluateAsync(User.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MfaRequirementDecision(User.Id, TenantId, PolicyRequiresMfa, ["Member"], PolicyFingerprint,
                RequiredByDefault: PolicyRequiresMfa));
        RecoveryRequests.Setup(port => port.FindLiveByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaRecoveryRequest?)null);

        // Session A is the enrolling device: its evidence row is discoverable (snapshot
        // taken once — the proof binding check is second-exact on FirstFactorVerifiedAt).
        // Session B has no row: it authenticated before any factor was enrolled.
        StoredEvidenceA = StoredEvidenceFor(SessionA, PolicyFingerprint);
        SessionEvidence.Setup(port => port.FindAsync(SessionA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredEvidenceA);
        SessionEvidence.Setup(port => port.FindAsync(SessionB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionMfaEvidence?)null);
    }

    public static string FreshDigest() =>
        Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();

    public SessionMfaEvidence StoredEvidenceFor(Guid sessionId) => StoredEvidenceFor(sessionId, PolicyFingerprint);

    public SessionMfaEvidence StoredEvidenceFor(Guid sessionId, string policyFingerprint)
    {
        var challenge = new SignInMfaChallenge
        {
            Id = Guid.NewGuid(), SubjectId = User.Id, TenantId = TenantId,
            SubjectTokenVersion = User.TokenVersion, TokenHash = FreshDigest(),
            PolicyFingerprint = policyFingerprint, Purpose = SignInMfaPurpose.VerifyFactor,
            FirstFactor = SignInFirstFactor.Password,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5), ExpiresAt = DateTimeOffset.UtcNow
        };
        var proof = SignInMfaProof.FromVerifiedChallenge(challenge, MfaMethod.Totp, DateTimeOffset.UtcNow.AddMinutes(-4));
        return SessionMfaEvidence.Create(sessionId, proof);
    }

    public Task<SignInMfaProof?> ReadSessionProofAsync(Guid sessionId) =>
        Service().ReadSessionProofAsync(User.Id, User.TokenVersion, TenantId, sessionId,
            StoredEvidenceA.ToProof().FirstFactorVerifiedAt, CancellationToken.None);

    private SignInMfaService Service() => new(
        Subjects.Object, Policy.Object, Store.Object, Mfa.Object, Issuer.Object, Audit.Object,
        SessionEvidence.Object, Mock.Of<ISignInMfaEnrollmentPort>(MockBehavior.Strict), RecoveryRequests.Object);
}
