using System.Security.Cryptography;
using System.Text.Json;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class SignInMfaServiceTests
{
    [Theory]
    [InlineData(SignInFirstFactor.Password)]
    [InlineData(SignInFirstFactor.Federated)]
    [InlineData(SignInFirstFactor.Wallet)]
    [InlineData(SignInFirstFactor.MagicLink)]
    [InlineData(SignInFirstFactor.Passkey)]
    public async Task MandatoryPolicyPersistsBoundChallengeBeforeReturningOnlyLimitedBearer(SignInFirstFactor factor)
    {
        var fixture = new FlowFixture();
        var result = await fixture.BeginAsync(factor);
        AssertLimited(result, fixture.User, fixture.TenantId);
        Assert.False(CommandOutcome.ShouldRollback(result));
        Assert.True(SignInMfaChallengeToken.TryHash(result.MfaToken, out var hash));
        var stored = Assert.Single(fixture.Stored);
        Assert.Equal(hash, stored.TokenHash);
        Assert.NotEqual(result.MfaToken, stored.TokenHash);
        Assert.Equal(fixture.User.Id, stored.SubjectId);
        Assert.Equal(fixture.User.TokenVersion, stored.SubjectTokenVersion);
        Assert.Equal(fixture.TenantId, stored.TenantId);
        Assert.Equal(fixture.Decision.PolicyFingerprint, stored.PolicyFingerprint);
        Assert.Equal(factor, stored.FirstFactor);
        Assert.Equal(SignInMfaPurpose.VerifyFactor, stored.Purpose);
        Assert.Equal(TimeSpan.FromMinutes(5), stored.ExpiresAt - stored.CreatedAt);
        Assert.Equal(new[] { "subject", "policy", "persist", "audit:Authentication.MfaSignInRequired" }, fixture.Trace);
        fixture.Issuer.VerifyNoOtherCalls();
        fixture.Mfa.VerifyNoOtherCalls();
        Assert.DoesNotContain(result.MfaToken!, JsonSerializer.Serialize(Assert.Single(fixture.Audits)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OptionalUnenrolledPolicyCanUseTheExistingIssuerWithTheResolvedTenant()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.Decision = fixture.Decision with { RequiresMfa = false };
        var result = await fixture.BeginAsync();
        Assert.Same(fixture.Issued, result);
        Assert.Equal(new[] { "subject", "policy", "issue" }, fixture.Trace);
        Assert.Empty(fixture.Stored);
    }

    [Theory]
    [InlineData("enrolled")]
    [InlineData("risk")]
    public async Task EnrollmentOrRiskStillGatesAnOtherwiseOptionalPolicy(string mode)
    {
        var fixture = new FlowFixture { Enrolled = mode == "enrolled" };
        fixture.Decision = fixture.Decision with { RequiresMfa = false };
        var result = await fixture.BeginAsync(risk: mode == "risk");
        AssertLimited(result, fixture.User, fixture.TenantId);
        Assert.Equal(mode == "risk", result.RequiresStepUp);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequiredUnenrolledAccountGetsOnlyAnEnrollmentBoundChallenge()
    {
        var fixture = new FlowFixture { Enrolled = false };
        var configurationId = Guid.NewGuid();
        var fingerprint = FlowFixture.Digest();
        fixture.SetupEnrollment(configurationId, fingerprint, "sign-in-enrollment-secret", "otpauth://totp/sign-in-enrollment");
        var result = await fixture.BeginAsync();
        AssertLimited(result, fixture.User, fixture.TenantId);
        var stored = Assert.Single(fixture.Stored);
        Assert.Equal(SignInMfaPurpose.EnrollFactor, stored.Purpose);
        Assert.Equal(configurationId, stored.EnrollmentConfigurationId);
        Assert.NotEqual(Guid.Empty, stored.EnrollmentConfigurationId!.Value);
        Assert.Equal(fingerprint, stored.EnrollmentSecretFingerprint);
        Assert.True(SignInMfaChallengeToken.IsDigest(stored.EnrollmentSecretFingerprint!));
        Assert.NotNull(stored.EnrollmentInitializedAt);
        Assert.InRange(stored.EnrollmentInitializedAt!.Value, stored.CreatedAt, stored.ExpiresAt);
        Assert.True(stored.EnrollmentInitializedAt < stored.ExpiresAt);
        Assert.Contains("enrollment", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { "subject", "policy", "lock", "subject", "enroll", "persist", "audit:Authentication.MfaSignInRequired" }, fixture.Trace);
        fixture.Challenge = stored;
        var completed = await fixture.CompleteAsync(result.MfaToken!);
        AssertDenied(completed);
        fixture.Issuer.VerifyNoOtherCalls();
        fixture.Mfa.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConcurrentEnrollmentCompletionUnderLockFallsBackToVerificationChallenge()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.SetupEnrollment(Guid.NewGuid(), FlowFixture.Digest(), "raced-secret", "otpauth://totp/raced");
        // The subject re-read under the enrollment lock observes the concurrent completion.
        fixture.AfterLock = () => fixture.Enrolled = true;
        var result = await fixture.BeginAsync();
        AssertLimited(result, fixture.User, fixture.TenantId);
        var stored = Assert.Single(fixture.Stored);
        Assert.Equal(SignInMfaPurpose.VerifyFactor, stored.Purpose);
        Assert.Null(stored.EnrollmentConfigurationId);
        Assert.Null(stored.EnrollmentSecretFingerprint);
        Assert.Null(stored.EnrollmentInitializedAt);
        Assert.DoesNotContain("enroll", fixture.Trace);
        Assert.Equal(new[] { "subject", "policy", "lock", "subject", "persist", "audit:Authentication.MfaSignInRequired" }, fixture.Trace);
        Assert.Null(result.MfaEnrollment);
        fixture.Enrollment.Verify(port => port.StartOrResumeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequiredUnenrolledAccountResponseOffersEnrollment()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.SetupEnrollment(Guid.NewGuid(), FlowFixture.Digest(), "offered-enrollment-secret", "otpauth://totp/offered-enrollment");
        var result = await fixture.BeginAsync();
        AssertLimited(result, fixture.User, fixture.TenantId);
        Assert.Equal(new[] { "TOTP" }, result.AvailableMethods);
        var enrollment = result.MfaEnrollment;
        Assert.NotNull(enrollment);
        Assert.Equal("offered-enrollment-secret", enrollment!.SecretKey);
        Assert.Equal("otpauth://totp/offered-enrollment", enrollment.QrCodeUri);
        var stored = Assert.Single(fixture.Stored);
        Assert.InRange(enrollment.ExpiresAt, stored.CreatedAt, stored.ExpiresAt);
    }

    [Fact]
    public async Task EnrollmentChallengeSurvivesValidateForPersistence()
    {
        // The mocked store mirrors the real one: AddAsync validates before persisting.
        var fixture = new FlowFixture { Enrolled = false };
        fixture.SetupEnrollment(Guid.NewGuid(), FlowFixture.Digest(), "validated-enrollment-secret", "otpauth://totp/validated-enrollment");
        await fixture.BeginAsync();
        var stored = Assert.Single(fixture.Stored);
        stored.ValidateForPersistence();
    }

    [Fact]
    public async Task EnrollmentChallengesCompleteThroughProofMatchingConsumptionAndBackupCodes()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.SetupEnrollmentMatchSequence(preProof: true, postProof: true);
        // VerifyTotpAsync flips the provisioned configuration to enrolled inside provider verification.
        fixture.AfterVerification = () => fixture.Enrolled = true;
        var began = await fixture.BeginAsync();
        AssertLimited(began, fixture.User, fixture.TenantId);
        fixture.Challenge = Assert.Single(fixture.Stored);
        fixture.Trace.Clear();
        fixture.Mfa.Setup(port => port.GenerateBackupCodesAsync(fixture.User.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "enrollment-backup-code-one", "enrollment-backup-code-two" });

        var completed = await fixture.CompleteAsync(began.MfaToken!, MfaMethod.Totp);

        Assert.Same(fixture.Issued, completed);
        Assert.True(completed.Success);
        Assert.Equal(new[] { "find", "lock", "find", "matches:false", "subject", "policy", "verify", "subject", "policy", "matches:true", "consume", "issue", "audit:Authentication.MfaSignInVerified" }, fixture.Trace);
        Assert.Equal(SignInMfaPurpose.EnrollFactor, fixture.ConsumedBinding!.Purpose);
        Assert.Equal(MfaMethod.Totp, fixture.ConsumedMethod);
        Assert.Equal(new[] { "enrollment-backup-code-one", "enrollment-backup-code-two" }, completed.MfaEnrollmentBackupCodes);
        fixture.Enrollment.Verify(port => port.MatchesAsync(It.IsAny<SignInMfaChallenge>(), false, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Enrollment.Verify(port => port.MatchesAsync(It.IsAny<SignInMfaChallenge>(), true, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrollmentCompletionIsDeniedWhenTheConfigurationNoLongerMatchesPreProof()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.SetupEnrollmentMatchSequence(preProof: false, postProof: true);
        var began = await fixture.BeginAsync();
        fixture.Challenge = Assert.Single(fixture.Stored);
        fixture.Trace.Clear();

        var completed = await fixture.CompleteAsync(began.MfaToken!, MfaMethod.Totp);

        AssertDenied(completed);
        Assert.Equal(new[] { "find", "lock", "find", "matches:false" }, fixture.Trace);
        Assert.Null(fixture.ConsumedBinding);
        fixture.Mfa.VerifyNoOtherCalls();
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnrollmentChallengesCannotBeCompletedWithABackupCode()
    {
        var fixture = new FlowFixture { Enrolled = false };
        var began = await fixture.BeginAsync();
        fixture.Challenge = Assert.Single(fixture.Stored);
        fixture.Trace.Clear();

        var completed = await fixture.CompleteAsync(began.MfaToken!, MfaMethod.BackupCode);

        AssertDenied(completed);
        Assert.Equal(new[] { "find" }, fixture.Trace);
        Assert.Null(fixture.ConsumedBinding);
        fixture.Mfa.VerifyNoOtherCalls();
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("different-subject")]
    [InlineData("changed-version")]
    [InlineData("inactive")]
    [InlineData("suspended")]
    [InlineData("deleted")]
    public async Task FirstFactorCannotBeReboundToAnUnavailableOrChangedAccount(string mode)
    {
        var fixture = new FlowFixture();
        fixture.ApplySubjectFault(mode);
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => fixture.BeginAsync());
        fixture.Issuer.VerifyNoOtherCalls();
        fixture.Policy.VerifyNoOtherCalls();
        Assert.Empty(fixture.Stored);
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("tenant")]
    [InlineData("fingerprint")]
    public async Task MismatchedPolicyEvidenceCannotAuthorizeAnIssuerOrChallenge(string mode)
    {
        var fixture = new FlowFixture();
        fixture.Decision = mode switch
        {
            "subject" => fixture.Decision with { SubjectId = Guid.NewGuid() },
            "tenant" => fixture.Decision with { TenantId = Guid.NewGuid() },
            "fingerprint" => fixture.Decision with { PolicyFingerprint = "invalid" },
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.BeginAsync());
        Assert.Empty(fixture.Stored);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("store")]
    [InlineData("audit")]
    public async Task FailedChallengePersistenceOrAuditCannotReturnACommitOnDenialOutcome(string mode)
    {
        var fixture = new FlowFixture();
        if (mode == "store")
        {
            fixture.Store.Setup(port => port.AddAsync(It.IsAny<SignInMfaChallenge>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("storage unavailable"));
        }
        else
        {
            fixture.Audit.Setup(port => port.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("audit unavailable"));
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.BeginAsync());
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("bearer")]
    [InlineData("method")]
    [InlineData("code-empty")]
    [InlineData("code-too-long")]
    public async Task InvalidCompletionInputCannotReadSubjectOrCallVerification(string mode)
    {
        var fixture = new FlowFixture();
        var result = await fixture.CompleteAsync(mode == "bearer" ? "invalid" : fixture.Bearer,
            mode == "method" ? MfaMethod.WebAuthn : MfaMethod.BackupCode,
            mode == "code-empty" ? "" : mode == "code-too-long" ? new string('A', 65) : fixture.Code);
        AssertDenied(result);
        fixture.Store.VerifyNoOtherCalls();
        fixture.Subjects.VerifyNoOtherCalls();
        fixture.Mfa.VerifyNoOtherCalls();
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(MfaMethod.Totp)]
    [InlineData(MfaMethod.BackupCode)]
    public async Task VerifiedProofAndFreshBindingPrecedeAtomicConsumptionAndExistingIssuer(MfaMethod method)
    {
        var fixture = new FlowFixture();
        var result = await fixture.CompleteAsync(method: method);
        Assert.Same(fixture.Issued, result);
        Assert.Equal(new[] { "find", "subject", "policy", "verify", "subject", "policy", "consume", "issue", "audit:Authentication.MfaSignInVerified" }, fixture.Trace);
        Assert.Equal(new SignInMfaChallengeBinding(fixture.User.Id, fixture.TenantId, fixture.User.TokenVersion,
            fixture.Decision.PolicyFingerprint, SignInMfaPurpose.VerifyFactor), fixture.ConsumedBinding);
        Assert.Equal(method, fixture.ConsumedMethod);
        var audit = Assert.Single(fixture.Audits);
        Assert.Equal(result.SessionId, audit.SessionId);
        Assert.Equal(fixture.User.Id, audit.UserId);
        Assert.Equal(fixture.TenantId, audit.TenantId);
        var serialized = JsonSerializer.Serialize(audit);
        Assert.DoesNotContain(fixture.Bearer, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Code, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(result.AccessToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(result.RefreshToken, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuccessfulCompletionCancelsTheUsersLiveRecoveryRequest()
    {
        var fixture = new FlowFixture { LiveRecovery = new MfaRecoveryRequest { Id = Guid.NewGuid(), UserId = "recovery-user", Status = MfaRecoveryRequestStatus.AwaitingConfirmation } };
        var result = await fixture.CompleteAsync(method: MfaMethod.Totp);
        Assert.Same(fixture.Issued, result);
        Assert.Equal(MfaRecoveryRequestStatus.Cancelled, fixture.LiveRecovery.Status);
        Assert.Equal("recovery-user", fixture.CancelledRecoveryUserId);
    }

    [Fact]
    public async Task SuccessfulTotpCompletionCancelsPendingLiveRecoveryRequest()
    {
        // Pending (pre-cooldown) is the other live state alongside AwaitingConfirmation covered above.
        var fixture = new FlowFixture { LiveRecovery = new MfaRecoveryRequest { Id = Guid.NewGuid(), UserId = "recovery-user", Status = MfaRecoveryRequestStatus.Pending } };
        var result = await fixture.CompleteAsync(method: MfaMethod.Totp);
        Assert.Same(fixture.Issued, result);
        Assert.Equal(MfaRecoveryRequestStatus.Cancelled, fixture.LiveRecovery.Status);
        Assert.Equal("recovery-user", fixture.CancelledRecoveryUserId);
    }

    [Fact]
    public async Task FailedVerificationLeavesRecoveryRequestsUntouched()
    {
        var fixture = new FlowFixture { VerificationSuccess = false, LiveRecovery = new MfaRecoveryRequest { UserId = "recovery-user", Status = MfaRecoveryRequestStatus.Pending } };
        var result = await fixture.CompleteAsync();
        Assert.False(result.Success);
        Assert.Equal(MfaRecoveryRequestStatus.Pending, fixture.LiveRecovery.Status);
        Assert.Null(fixture.CancelledRecoveryUserId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("different-subject")]
    [InlineData("changed-version")]
    [InlineData("inactive")]
    [InlineData("suspended")]
    [InlineData("deleted")]
    [InlineData("unenrolled")]
    [InlineData("locked")]
    public async Task CurrentAccountChangesDenyBeforeProviderVerification(string mode)
    {
        var fixture = new FlowFixture();
        fixture.ApplySubjectFault(mode);
        AssertDenied(await fixture.CompleteAsync());
        fixture.Mfa.VerifyNoOtherCalls();
        fixture.Issuer.VerifyNoOtherCalls();
        Assert.Null(fixture.ConsumedBinding);
    }

    [Theory]
    [InlineData("changed-version")]
    [InlineData("inactive")]
    [InlineData("suspended")]
    [InlineData("deleted")]
    [InlineData("unenrolled")]
    [InlineData("locked")]
    [InlineData("policy")]
    public async Task ChangesDuringProviderVerificationDenyWithoutConsumingChallengeOrIssuingTokens(string mode)
    {
        var fixture = new FlowFixture();
        fixture.AfterVerification = () =>
        {
            if (mode == "policy") { fixture.Decision = fixture.Decision with { PolicyFingerprint = FlowFixture.Digest() }; }
            else { fixture.ApplySubjectFault(mode); }
        };
        AssertDenied(await fixture.CompleteAsync());
        Assert.Null(fixture.ConsumedBinding);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WrongCodeKeepsFailureAccountingOnlyAfterAuditSucceeds()
    {
        var fixture = new FlowFixture { VerificationSuccess = false };
        var result = await fixture.CompleteAsync();
        Assert.False(result.Success);
        Assert.False(CommandOutcome.ShouldRollback(result));
        Assert.Empty(result.AccessToken);
        Assert.Empty(result.RefreshToken);
        Assert.Null(fixture.ConsumedBinding);
        Assert.Equal("Authentication.MfaSignInDenied", Assert.Single(fixture.Audits).ActionType);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("additional-proof")]
    [InlineData("lost-consumption")]
    public async Task IncompleteProofOrLostConsumptionCannotCommitProviderProofOrIssueCredentials(string mode)
    {
        var fixture = new FlowFixture { AdditionalProof = mode == "additional-proof", ConsumeResult = mode != "lost-consumption" };
        AssertDenied(await fixture.CompleteAsync());
        fixture.Issuer.VerifyNoOtherCalls();
        Assert.Empty(fixture.Audits);
    }

    [Theory]
    [InlineData("provider")]
    [InlineData("issuer")]
    [InlineData("audit")]
    public async Task InfrastructureFailuresPropagateToTheCommandTransactionOwner(string mode)
    {
        var fixture = new FlowFixture();
        switch (mode)
        {
            case "provider": fixture.Mfa.Setup(port => port.VerifyMfaAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<MfaMethod>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("provider unavailable")); break;
            case "issuer": fixture.Issuer.Setup(port => port.IssueMfaAsync(It.IsAny<User>(), It.IsAny<Guid?>(), It.IsAny<DeviceInfo>(), It.IsAny<SignInMfaProof>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("issuer unavailable")); break;
            case "audit": fixture.Audit.Setup(port => port.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("audit unavailable")); break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CompleteAsync());
    }

    [Fact]
    public async Task CancellationDuringProofVerificationPreventsConsumptionAndIssuance()
    {
        var fixture = new FlowFixture();
        using var cancellation = new CancellationTokenSource();
        fixture.AfterVerification = cancellation.Cancel;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.CompleteAsync(cancellationToken: cancellation.Token));
        Assert.Null(fixture.ConsumedBinding);
        fixture.Issuer.VerifyNoOtherCalls();
        Assert.Empty(fixture.Audits);
    }

    [Fact]
    public async Task RefreshRestoresOnlyTheOriginalBoundProof()
    {
        var fixture = new FlowFixture();
        var proof = SignInMfaProof.FromVerifiedChallenge(fixture.Challenge!, MfaMethod.BackupCode, DateTimeOffset.UtcNow);
        var stored = SessionMfaEvidence.Create(fixture.Issued.SessionId, proof);
        fixture.SessionEvidence.Setup(port => port.FindAsync(fixture.Issued.SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        var restored = await fixture.ReadSessionAsync(proof.FirstFactorVerifiedAt);
        Assert.NotNull(restored);
        Assert.Equal(proof.VerifiedAt, restored.VerifiedAt);
        Assert.Equal(proof.FirstFactorVerifiedAt, restored.FirstFactorVerifiedAt);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("missing-proof")]
    [InlineData("subject")]
    [InlineData("tenant")]
    [InlineData("version")]
    [InlineData("session")]
    [InlineData("policy")]
    [InlineData("auth-time")]
    [InlineData("unenrolled")]
    [InlineData("locked")]
    public async Task StaleOrUnboundSessionProofCannotRefreshMfaCredentials(string fault)
    {
        var fixture = new FlowFixture();
        var proof = SignInMfaProof.FromVerifiedChallenge(fixture.Challenge!, MfaMethod.BackupCode, DateTimeOffset.UtcNow);
        var stored = SessionMfaEvidence.Create(fixture.Issued.SessionId, proof);
        switch (fault)
        {
            case "subject": stored.SubjectId = Guid.NewGuid(); break;
            case "tenant": stored.TenantId = Guid.NewGuid(); break;
            case "version": stored.TokenVersion++; break;
            case "session": stored.SessionId = Guid.NewGuid(); break;
            case "policy": stored.PolicyFingerprint = FlowFixture.Digest(); break;
            case "unenrolled": fixture.Enrolled = false; break;
            case "locked": fixture.Lockout = DateTime.UtcNow.AddMinutes(5); break;
            case "missing-proof":
            case "auth-time": break;
            default: throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown MFA session proof fault.");
        }
        fixture.SessionEvidence.Setup(port => port.FindAsync(fixture.Issued.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fault == "missing-proof" ? null : stored);
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => fixture.ReadSessionAsync(
            fault == "auth-time" ? proof.FirstFactorVerifiedAt.AddSeconds(1) : proof.FirstFactorVerifiedAt));
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ElevatedRoleOnlyRequirementWithoutAFactorSignsInWithCompliancePending()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.Decision = fixture.Decision with { RequiredByDefault = false };
        var result = await fixture.BeginAsync();
        Assert.Same(fixture.Issued, result);
        Assert.Equal(new[] { "subject", "policy", "issue" }, fixture.Trace);
        Assert.Empty(fixture.Stored);
    }

    [Fact]
    public async Task EnrolledUserIsStillChallengedUnderARoleOnlyRequirement()
    {
        var fixture = new FlowFixture { Enrolled = true };
        fixture.Decision = fixture.Decision with { RequiredByDefault = false };
        var result = await fixture.BeginAsync();
        AssertLimited(result, fixture.User, fixture.TenantId);
        Assert.False(fixture.Decision.RequiredByDefault);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequireMfaByDefaultStillForcesTheEnrollmentChallenge()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.SetupEnrollment(Guid.NewGuid(), FlowFixture.Digest(), "by-default-secret", "otpauth://totp/by-default");
        var result = await fixture.BeginAsync();
        AssertLimited(result, fixture.User, fixture.TenantId);
        Assert.Contains("enrollment", result.Message, StringComparison.OrdinalIgnoreCase);
        var stored = Assert.Single(fixture.Stored);
        Assert.Equal(SignInMfaPurpose.EnrollFactor, stored.Purpose);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OptionalUnenrolledSessionRetainsOrdinaryRefresh()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.Decision = fixture.Decision with { RequiresMfa = false, RequiredByDefault = false };
        fixture.SessionEvidence.Setup(port => port.FindAsync(fixture.Issued.SessionId, It.IsAny<CancellationToken>())).ReturnsAsync((SessionMfaEvidence?)null);
        Assert.Null(await fixture.ReadSessionAsync(DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public async Task ElevatedRoleOnlyUnenrolledSessionRetainsOrdinaryRefresh()
    {
        var fixture = new FlowFixture { Enrolled = false };
        fixture.Decision = fixture.Decision with { RequiredByDefault = false };
        fixture.SessionEvidence.Setup(port => port.FindAsync(fixture.Issued.SessionId, It.IsAny<CancellationToken>())).ReturnsAsync((SessionMfaEvidence?)null);
        Assert.Null(await fixture.ReadSessionAsync(DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    private static void AssertLimited(SignInResponse result, User verifiedSubject, Guid tenantId)
    {
        Assert.False(result.Success);
        Assert.True(result.RequiresMfa);
        Assert.Empty(result.AccessToken);
        Assert.Empty(result.RefreshToken);
        Assert.Equal(Guid.Empty, result.SessionId);
        Assert.Equal(verifiedSubject.Id, result.UserId);
        Assert.Equal(verifiedSubject.Email, result.Email);
        Assert.Equal(verifiedSubject.Id, result.User.Id);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Null(result.User.PhoneNumber);
        Assert.False(result.User.PhoneNumberVerified);
        Assert.Null(result.AvailableTenants);
        Assert.NotNull(result.MfaToken);
    }

    private static void AssertDenied(SignInResponse result)
    {
        Assert.False(result.Success);
        Assert.True(CommandOutcome.ShouldRollback(result));
        Assert.Empty(result.AccessToken);
        Assert.Empty(result.RefreshToken);
        Assert.Equal(Guid.Empty, result.SessionId);
        Assert.Equal(Guid.Empty, result.UserId);
        Assert.Null(result.MfaToken);
    }

    private sealed class FlowFixture
    {
        public User User { get; } = new() { Id = Guid.NewGuid(), TokenVersion = 7, Email = "mfa-flow@example.test", Version = 1 };
        public Guid VerifiedSubjectId { get; }
        public int VerifiedVersion { get; }
        public Guid TenantId { get; } = Guid.NewGuid();
        public DeviceInfo Device { get; } = new() { Fingerprint = "synthetic-device", IpAddress = "127.0.0.1", UserAgent = "candidate-test" };
        public MfaRequirementDecision Decision { get; set; }
        public string Bearer { get; } = SignInMfaChallengeToken.Create();
        public string Code { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        public SignInMfaChallenge? Challenge { get; set; }
        public Mock<ISignInMfaSubjectReader> Subjects { get; } = new(MockBehavior.Strict);
        public Mock<IMfaSubjectRequirementPolicy> Policy { get; } = new(MockBehavior.Strict);
        public Mock<ISignInMfaChallengeStore> Store { get; } = new(MockBehavior.Strict);
        public Mock<IMfaService> Mfa { get; } = new(MockBehavior.Strict);
        public Mock<IAuthenticatedSessionIssuer> Issuer { get; } = new(MockBehavior.Strict);
        public Mock<IAuthenticationAuditEventSink> Audit { get; } = new(MockBehavior.Strict);
        public Mock<ISessionMfaEvidenceStore> SessionEvidence { get; } = new(MockBehavior.Strict);
        public Mock<ISignInMfaEnrollmentPort> Enrollment { get; } = new(MockBehavior.Strict);
        public Mock<IMfaRecoveryRequestStore> RecoveryRequests { get; } = new(MockBehavior.Strict);
        public SignInMfaEnrollmentSetup? EnrollmentSetup { get; private set; }
        public MfaRecoveryRequest? LiveRecovery { get; set; }
        public string? CancelledRecoveryUserId { get; private set; }
        public List<string> Trace { get; } = [];
        public List<SignInMfaChallenge> Stored { get; } = [];
        public List<AuthenticationAuditEvent> Audits { get; } = [];
        public SignInResponse Issued { get; }
        public bool Enrolled { get; set; } = true;
        public bool MissingSubject { get; set; }
        public DateTime? Lockout { get; set; }
        public bool VerificationSuccess { get; init; } = true;
        public bool AdditionalProof { get; init; }
        public bool ConsumeResult { get; init; } = true;
        public Action? AfterVerification { get; set; }
        public Action? AfterLock { get; set; }
        public SignInMfaChallengeBinding? ConsumedBinding { get; private set; }
        public MfaMethod? ConsumedMethod { get; private set; }

        public FlowFixture()
        {
            VerifiedSubjectId = User.Id;
            VerifiedVersion = User.TokenVersion;
            Decision = new MfaRequirementDecision(User.Id, TenantId, true, ["Member"], Digest(), RequiredByDefault: true);
            Assert.True(SignInMfaChallengeToken.TryHash(Bearer, out var hash));
            Challenge = new SignInMfaChallenge { Id = Guid.NewGuid(), SubjectId = User.Id, TenantId = TenantId,
                SubjectTokenVersion = User.TokenVersion, TokenHash = hash, PolicyFingerprint = Decision.PolicyFingerprint,
                Purpose = SignInMfaPurpose.VerifyFactor, FirstFactor = SignInFirstFactor.Password,
                CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) };
            Issued = new SignInResponse { Success = true, UserId = User.Id, TenantId = TenantId, SessionId = Guid.NewGuid(),
                AccessToken = SignInMfaChallengeToken.Create(), RefreshToken = SignInMfaChallengeToken.Create() };
            Subjects.Setup(port => port.ReadCurrentAsync(VerifiedSubjectId, It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                Trace.Add("subject");
                return MissingSubject ? null : new SignInMfaSubjectState(User, Enrolled, Lockout);
            });
            Policy.Setup(port => port.EvaluateAsync(VerifiedSubjectId, TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                Trace.Add("policy");
                return Decision;
            });
            Store.Setup(port => port.AddAsync(It.IsAny<SignInMfaChallenge>(), It.IsAny<CancellationToken>())).Returns((SignInMfaChallenge challenge, CancellationToken cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                challenge.ValidateForPersistence();
                Trace.Add("persist");
                Stored.Add(challenge);
                return Task.CompletedTask;
            });
            Store.Setup(port => port.FindActiveAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                Trace.Add("find");
                return Challenge;
            });
            Store.Setup(port => port.TryConsumeAsync(It.IsAny<string>(), It.IsAny<SignInMfaChallengeBinding>(), It.IsAny<MfaMethod>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string tokenHash, SignInMfaChallengeBinding binding, MfaMethod method, DateTimeOffset now, CancellationToken cancellationToken) =>
                {
                    _ = tokenHash; _ = now;
                    cancellationToken.ThrowIfCancellationRequested();
                    Trace.Add("consume");
                    ConsumedBinding = binding; ConsumedMethod = method;
                    return ConsumeResult;
                });
            Mfa.Setup(port => port.VerifyMfaAsync(VerifiedSubjectId, It.IsAny<string>(), It.IsAny<MfaMethod>(), It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                Trace.Add("verify");
                AfterVerification?.Invoke();
                return new MfaVerificationResult { IsSuccess = VerificationSuccess, RequiresAdditionalVerification = AdditionalProof };
            });
            Issuer.Setup(port => port.IssueAsync(It.IsAny<User>(), TenantId, Device, It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                Trace.Add("issue");
                return Issued;
            });
            Issuer.Setup(port => port.IssueMfaAsync(It.IsAny<User>(), TenantId, Device, It.IsAny<SignInMfaProof>(), It.IsAny<CancellationToken>())).ReturnsAsync(() =>
            {
                Trace.Add("issue");
                return Issued;
            });
            Audit.Setup(port => port.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>())).Returns((AuthenticationAuditEvent auditEvent, CancellationToken cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Trace.Add("audit:" + auditEvent.ActionType);
                Audits.Add(auditEvent);
                return Task.CompletedTask;
            });
            RecoveryRequests.Setup(port => port.FindLiveByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string userId, CancellationToken cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return LiveRecovery;
            });
            RecoveryRequests.Setup(port => port.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                CancelledRecoveryUserId = LiveRecovery?.UserId;
                return Task.CompletedTask;
            });
            SetupEnrollment(Guid.NewGuid(), Digest(), "default-enrollment-secret", "otpauth://totp/default-enrollment");
        }

        public void SetupEnrollment(Guid configurationId, string fingerprint, string secret, string qr)
        {
            EnrollmentSetup = new SignInMfaEnrollmentSetup(configurationId, fingerprint, secret, qr, Challenge!.ExpiresAt);
            Enrollment.Setup(port => port.AcquireSubjectLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid _, CancellationToken cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Trace.Add("lock");
                    AfterLock?.Invoke();
                    return Task.CompletedTask;
                });
            Enrollment.Setup(port => port.StartOrResumeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, string _, DateTimeOffset _, DateTimeOffset _, CancellationToken cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Trace.Add("enroll");
                    return EnrollmentSetup!;
                });
        }

        public void SetupEnrollmentMatchSequence(bool preProof, bool postProof)
        {
            Enrollment.Setup(port => port.MatchesAsync(It.IsAny<SignInMfaChallenge>(), false, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SignInMfaChallenge _, bool _, DateTimeOffset _, CancellationToken _) =>
                {
                    Trace.Add("matches:false");
                    return preProof;
                });
            Enrollment.Setup(port => port.MatchesAsync(It.IsAny<SignInMfaChallenge>(), true, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SignInMfaChallenge _, bool _, DateTimeOffset _, CancellationToken _) =>
                {
                    Trace.Add("matches:true");
                    return postProof;
                });
        }

        public static string Digest() => Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();
        public Task<SignInResponse> BeginAsync(SignInFirstFactor factor = SignInFirstFactor.Password, bool risk = false) =>
            Service().BeginAsync(VerifiedSubjectId, VerifiedVersion, TenantId, Device, factor, risk, CancellationToken.None);
        public Task<SignInResponse> CompleteAsync(string? bearer = null, MfaMethod method = MfaMethod.BackupCode, string? code = null, CancellationToken cancellationToken = default) =>
            Service().CompleteCodeAsync(bearer ?? Bearer, code ?? Code, method, Device, cancellationToken);
        public Task<SignInMfaProof?> ReadSessionAsync(DateTimeOffset authenticatedAt) =>
            Service().ReadSessionProofAsync(VerifiedSubjectId, VerifiedVersion, TenantId, Issued.SessionId, authenticatedAt, CancellationToken.None);
        private SignInMfaService Service() => new(Subjects.Object, Policy.Object, Store.Object, Mfa.Object, Issuer.Object, Audit.Object, SessionEvidence.Object, Enrollment.Object, RecoveryRequests.Object);
        public void ApplySubjectFault(string mode)
        {
            switch (mode)
            {
                case "missing": MissingSubject = true; break;
                case "different-subject": User.Id = Guid.NewGuid(); break;
                case "changed-version": User.TokenVersion++; break;
                case "inactive": User.IsActive = false; break;
                case "suspended": User.IsSuspended = true; break;
                case "deleted": User.DeletedAt = SystemClock.UtcNow; break;
                case "unenrolled": Enrolled = false; break;
                case "locked": Lockout = SystemClock.UtcNow.AddMinutes(5); break;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
    }
}
