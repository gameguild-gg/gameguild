using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Users;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Gates a verified first factor before credential issuance. Pending challenges carry no ordinary credentials.
/// The command owner must roll back exceptions and ordinary denials, including provider proof consumption.
/// </summary>
public sealed class SignInMfaService(
    ISignInMfaSubjectReader subjects,
    IMfaSubjectRequirementPolicy policy,
    ISignInMfaChallengeStore challenges,
    IMfaService mfa,
    IAuthenticatedSessionIssuer issuer,
    IAuthenticationAuditEventSink audit,
    ISessionMfaEvidenceStore sessionEvidence,
    ISignInMfaEnrollmentPort enrollment,
    IMfaRecoveryRequestStore recoveryRequests,
    MfaOptions? mfaOptions = null,
    TimeProvider? timeProvider = null) : ISignInMfaService
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly MfaOptions _mfaOptions = mfaOptions ?? new();

    public async Task<SignInResponse> BeginAsync(Guid verifiedSubjectId, int verifiedTokenVersion, Guid? requestedTenantId,
        DeviceInfo deviceInfo, SignInFirstFactor firstFactor, bool requiresRiskStepUp, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(verifiedSubjectId, verifiedTokenVersion, requestedTenantId, deviceInfo,
            firstFactor, requiresRiskStepUp, cancellationToken).ConfigureAwait(false);
        if (prepared.Outcome is { } outcome) { return outcome; }
        if (!prepared.MayIssueOrdinaryCredentials || prepared.Subject is not { } subject || prepared.Decision is not { } decision)
        {
            throw new InvalidOperationException("Current subject policy did not permit ordinary credentials.");
        }
        return await IssueAsync(subject, decision.TenantId, deviceInfo, cancellationToken,
            mfaCompliancePending: prepared.MfaCompliancePending).ConfigureAwait(false);
    }

    public async Task<SignInMfaPreparation> PrepareAsync(Guid verifiedSubjectId, int verifiedTokenVersion, Guid? requestedTenantId,
        DeviceInfo deviceInfo, SignInFirstFactor firstFactor, bool requiresRiskStepUp, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(deviceInfo);
        if (verifiedSubjectId == Guid.Empty || verifiedTokenVersion < 1 || !Enum.IsDefined(firstFactor))
        {
            throw new ArgumentException("Verified first-factor evidence is required.");
        }
        var current = await subjects.ReadCurrentAsync(verifiedSubjectId, cancellationToken).ConfigureAwait(false);
        RequireAccount(current, verifiedSubjectId, verifiedTokenVersion);
        if (IsLocked(current!)) { return SignInMfaPreparation.WithOutcome(Denied()); }
        var decision = await policy.EvaluateAsync(verifiedSubjectId, requestedTenantId, cancellationToken).ConfigureAwait(false);
        RequireDecision(decision, verifiedSubjectId, requestedTenantId);
        if (!decision.RequiresMfa && !current!.HasEnrolledMfa && !requiresRiskStepUp)
        {
            return SignInMfaPreparation.PermitWithoutMfa(current.User, decision);
        }
        if (!_mfaOptions.RequireEnrollmentForElevatedRoles &&
            decision.RequiresMfa && !decision.RequiredByDefault && !current!.HasEnrolledMfa && !requiresRiskStepUp)
        {
            // Elevated-role-only requirement with no enrolled factor: dropping MFA is honored at
            // sign-in. Compliance is reported instead of forcing an enrollment dead-end.
            return SignInMfaPreparation.PermitWithoutMfa(current.User, decision, mfaCompliancePending: true);
        }

        var token = SignInMfaChallengeToken.Create();
        if (!SignInMfaChallengeToken.TryHash(token, out var hash)) { throw new InvalidOperationException("Challenge creation failed."); }
        var now = _time.GetUtcNow();
        var completingEnrollment = !current!.HasEnrolledMfa;
        var challenge = new SignInMfaChallenge
        {
            Id = Guid.NewGuid(), SubjectId = verifiedSubjectId, SubjectTokenVersion = verifiedTokenVersion,
            TenantId = decision.TenantId, PolicyFingerprint = decision.PolicyFingerprint, TokenHash = hash,
            FirstFactor = firstFactor, Purpose = completingEnrollment ? SignInMfaPurpose.EnrollFactor : SignInMfaPurpose.VerifyFactor,
            CreatedAt = now, ExpiresAt = now.AddMinutes(5)
        };
        SignInMfaEnrollmentSetup? setup = null;
        if (completingEnrollment)
        {
            // Serialize against settings enrollment and concurrent sign-ins before provisioning.
            await enrollment.AcquireSubjectLockAsync(verifiedSubjectId, cancellationToken).ConfigureAwait(false);
            // Enrollment state was read before the lock; a concurrent completion (settings
            // setup or another sign-in) may have enabled the factor while this request waited.
            // Re-read the subject under the lock and fall back to a verification challenge.
            var locked = await subjects.ReadCurrentAsync(verifiedSubjectId, cancellationToken).ConfigureAwait(false);
            RequireAccount(locked, verifiedSubjectId, verifiedTokenVersion);
            if (IsLocked(locked!)) { return SignInMfaPreparation.WithOutcome(Denied()); }
            if (locked!.HasEnrolledMfa)
            {
                current = locked;
                challenge = new SignInMfaChallenge
                {
                    Id = Guid.NewGuid(), SubjectId = verifiedSubjectId, SubjectTokenVersion = verifiedTokenVersion,
                    TenantId = decision.TenantId, PolicyFingerprint = decision.PolicyFingerprint, TokenHash = hash,
                    FirstFactor = firstFactor, Purpose = SignInMfaPurpose.VerifyFactor,
                    CreatedAt = now, ExpiresAt = now.AddMinutes(5)
                };
            }
            else
            {
                setup = await enrollment.StartOrResumeAsync(verifiedSubjectId, current.User.Email, now, challenge.ExpiresAt,
                    cancellationToken).ConfigureAwait(false);
                challenge.EnrollmentConfigurationId = setup.ConfigurationId;
                challenge.EnrollmentSecretFingerprint = setup.SecretFingerprint;
                challenge.EnrollmentInitializedAt = now;
            }
        }
        await challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await RecordAsync("Authentication.MfaSignInRequired", challenge, false, firstFactor.ToString(), deviceInfo,
            null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // This marker is created only after challenge persistence and audit both succeed.
        return SignInMfaPreparation.WithOutcome(new SignInMfaPendingResponse(token, challenge.ExpiresAt, challenge.Purpose,
            requiresRiskStepUp, current.User, decision.TenantId, setup));
    }

    public async Task<SignInResponse> CompleteCodeAsync(string bearer, string code, MfaMethod method,
        DeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(deviceInfo);
        if (method is not (MfaMethod.Totp or MfaMethod.BackupCode) || string.IsNullOrWhiteSpace(code) || code.Length > 64 ||
            !SignInMfaChallengeToken.TryHash(bearer, out var hash)) { return Denied(); }
        var challenge = await challenges.FindActiveAsync(hash, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (challenge is null) { return Denied(); }
        var completingEnrollment = challenge.Purpose == SignInMfaPurpose.EnrollFactor;
        if (completingEnrollment)
        {
            if (method != MfaMethod.Totp || challenge.EnrollmentConfigurationId is null ||
                !SignInMfaChallengeToken.IsDigest(challenge.EnrollmentSecretFingerprint ?? string.Empty)) { return Denied(); }
            await enrollment.AcquireSubjectLockAsync(challenge.SubjectId, cancellationToken).ConfigureAwait(false);
            challenge = await challenges.FindActiveAsync(hash, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
            if (challenge is null || challenge.Purpose != SignInMfaPurpose.EnrollFactor ||
                !await enrollment.MatchesAsync(challenge, false, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false)) { return Denied(); }
        }
        else if (challenge.Purpose != SignInMfaPurpose.VerifyFactor) { return Denied(); }
        var current = await ReadBoundSubjectAsync(challenge, cancellationToken, enrolled: !completingEnrollment).ConfigureAwait(false);
        if (current is null || IsLocked(current)) { return Denied(); }

        var verification = await mfa.VerifyMfaAsync(challenge.SubjectId, code, method, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!verification.IsSuccess)
        {
            await RecordAsync("Authentication.MfaSignInDenied", challenge, false, method.ToString(), deviceInfo,
                null, cancellationToken).ConfigureAwait(false);
            // Keep durable failed-attempt accounting only after the trusted audit write succeeds.
            return new SignInMfaCommittedDenial();
        }
        if (verification.RequiresAdditionalVerification) { return Denied(); }

        // Provider verification can await I/O; refresh account/version/enrollment/policy after it.
        current = await ReadBoundSubjectAsync(challenge, cancellationToken).ConfigureAwait(false);
        if (current is null || IsLocked(current) || completingEnrollment &&
            !await enrollment.MatchesAsync(challenge, true, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false)) { return Denied(); }
        var binding = new SignInMfaChallengeBinding(current.User.Id, challenge.TenantId, current.User.TokenVersion,
            challenge.PolicyFingerprint, challenge.Purpose);
        if (!await challenges.TryConsumeAsync(hash, binding, method, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false))
        {
            return Denied();
        }
        var proof = SignInMfaProof.FromVerifiedChallenge(challenge, method, _time.GetUtcNow());
        // A successful second factor proves the user still controls their MFA; any live
        // lost-device recovery request is obsolete and is cancelled before issuance.
        var liveRecovery = await recoveryRequests.FindLiveByUserAsync(current.User.Id.ToString(), cancellationToken).ConfigureAwait(false);
        if (liveRecovery is not null)
        {
            liveRecovery.Status = MfaRecoveryRequestStatus.Cancelled;
            await recoveryRequests.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        var result = await IssueAsync(current.User, challenge.TenantId, deviceInfo, cancellationToken, proof).ConfigureAwait(false);
        if (completingEnrollment)
        {
            var backupCodes = await mfa.GenerateBackupCodesAsync(challenge.SubjectId, cancellationToken).ConfigureAwait(false);
            if (backupCodes is not { Length: > 0 } || backupCodes.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException("Enrollment recovery codes could not be generated.");
            }
            result.MfaEnrollmentBackupCodes = backupCodes;
        }
        await RecordAsync("Authentication.MfaSignInVerified", challenge, true, method.ToString(), deviceInfo,
            result.SessionId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private async Task<SignInMfaSubjectState?> ReadBoundSubjectAsync(SignInMfaChallenge challenge, CancellationToken cancellationToken, bool enrolled = true)
    {
        var current = await subjects.ReadCurrentAsync(challenge.SubjectId, cancellationToken).ConfigureAwait(false);
        if (!AccountMatches(current, challenge.SubjectId, challenge.SubjectTokenVersion) || current!.HasEnrolledMfa != enrolled) { return null; }
        var decision = await policy.EvaluateAsync(challenge.SubjectId, challenge.TenantId, cancellationToken).ConfigureAwait(false);
        RequireDecision(decision, challenge.SubjectId, challenge.TenantId);
        return string.Equals(decision.PolicyFingerprint, challenge.PolicyFingerprint, StringComparison.Ordinal) ? current : null;
    }

    public async Task<SignInMfaSessionProof?> ReadSessionProofAsync(Guid subjectId, int tokenVersion, Guid tenantId, Guid sessionId,
        DateTimeOffset authenticatedAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = await subjects.ReadCurrentAsync(subjectId, cancellationToken).ConfigureAwait(false);
        RequireAccount(current, subjectId, tokenVersion);
        var decision = await policy.EvaluateAsync(subjectId, tenantId, cancellationToken).ConfigureAwait(false);
        RequireDecision(decision, subjectId, tenantId);
        var stored = await sessionEvidence.FindAsync(sessionId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (stored is null)
        {
            if (current!.HasEnrolledMfa || decision.RequiredByDefault)
            {
                throw new AuthenticationRequiredException("MFA verification is required before refreshing this session.");
            }
            // Fail closed unless the surviving requirement is role-only AND the subject still
            // has no enrolled factor; only that reevaluated grace keeps the marker on refresh.
            return decision.RequiresMfa && !current.HasEnrolledMfa
                ? new SignInMfaSessionProof(null, MfaCompliancePending: true)
                : null;
        }
        if (!current!.HasEnrolledMfa || IsLocked(current) || stored.SessionId != sessionId ||
            !string.Equals(stored.PolicyFingerprint, decision.PolicyFingerprint, StringComparison.Ordinal))
        {
            throw new AuthenticationRequiredException("MFA verification evidence is no longer valid.");
        }
        var proof = stored.ToProof();
        proof.RequireBinding(subjectId, tenantId, tokenVersion, authenticatedAt, _time.GetUtcNow());
        return new SignInMfaSessionProof(proof);
    }

    private async Task<SignInResponse> IssueAsync(User user, Guid tenantId, DeviceInfo device, CancellationToken cancellationToken,
        SignInMfaProof? proof = null, bool mfaCompliancePending = false)
    {
        var result = proof is null
            ? await issuer.IssueAsync(user, tenantId, device, cancellationToken, mfaCompliancePending).ConfigureAwait(false)
            : await issuer.IssueMfaAsync(user, tenantId, device, proof, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (result is null || !result.Success || result.RequiresMfa || result.RequiresStepUp || result.UserId != user.Id ||
            result.TenantId != tenantId || result.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(result.AccessToken) ||
            string.IsNullOrWhiteSpace(result.RefreshToken))
        {
            throw new InvalidOperationException("Authenticated credentials could not be issued for the verified binding.");
        }
        return result;
    }

    private Task RecordAsync(string action, SignInMfaChallenge challenge, bool success, string method,
        DeviceInfo device, Guid? sessionId, CancellationToken cancellationToken) =>
        audit.RecordAsync(new AuthenticationAuditEvent(action, challenge.SubjectId, success, method,
            device.IpAddress, device.UserAgent, sessionId, challenge.TenantId,
            Metadata: new { ChallengeId = challenge.Id, challenge.Purpose }), cancellationToken);

    private bool IsLocked(SignInMfaSubjectState current) => current.LockedOutUntil is { } until && until > _time.GetUtcNow().UtcDateTime;

    private static bool AccountMatches(SignInMfaSubjectState? current, Guid subjectId, int version) =>
        current is not null && current.User.Id == subjectId && version > 0 && !current.User.IsDeleted &&
        current.User.ValidateForAuthentication(version).IsSuccess;

    private static void RequireAccount(SignInMfaSubjectState? current, Guid subjectId, int version)
    {
        if (!AccountMatches(current, subjectId, version)) { throw new AuthenticationRequiredException("Verified first-factor evidence is no longer valid."); }
    }

    private static void RequireDecision(MfaRequirementDecision decision, Guid subjectId, Guid? requestedTenantId)
    {
        if (decision is null || decision.SubjectId != subjectId || decision.TenantId == Guid.Empty ||
            requestedTenantId.HasValue && decision.TenantId != requestedTenantId.Value ||
            !SignInMfaChallengeToken.IsDigest(decision.PolicyFingerprint))
        {
            throw new InvalidOperationException("Current subject policy could not establish an authorized tenant binding.");
        }
    }

    private static SignInResponse Denied() => new() { Success = false, Message = "Additional verification failed" };
}

/// <summary>Refresh-time reevaluation outcome: the stored proof (if any) and the reevaluated compliance marker.</summary>
/// <param name="Proof">Verified second-factor evidence for the session; null for an ordinary refresh.</param>
/// <param name="MfaCompliancePending">True when the session refreshes under a role-only MFA requirement with no enrolled factor.</param>
public sealed record SignInMfaSessionProof(SignInMfaProof? Proof, bool MfaCompliancePending = false);

/// <summary>Server-only transaction outcome; the bearer is not an ordinary access or refresh credential.</summary>
internal sealed class SignInMfaPendingResponse : SignInResponse, ICommitOnFailureOutcome
{
    public SignInMfaPendingResponse(string bearer, DateTimeOffset expiresAt, SignInMfaPurpose purpose, bool requiresRiskStepUp,
        User verifiedSubject, Guid tenantId, SignInMfaEnrollmentSetup? enrollment = null)
    {
        ArgumentNullException.ThrowIfNull(verifiedSubject);
        if (verifiedSubject.Id == Guid.Empty || tenantId == Guid.Empty)
        {
            throw new ArgumentException("A pending MFA response requires a verified subject and resolved tenant.");
        }
        Success = false;
        Message = purpose == SignInMfaPurpose.EnrollFactor ? "MFA enrollment required" : "Additional verification required";
        RequiresMfa = true;
        MfaToken = bearer;
        RequiresStepUp = requiresRiskStepUp;
        StepUpToken = requiresRiskStepUp ? bearer : null;
        StepUpExpiresAt = expiresAt.UtcDateTime;
        AvailableMethods = purpose == SignInMfaPurpose.VerifyFactor ? ["TOTP", "BackupCode"] : ["TOTP"];
        // Only a sign-in-driven enrollment carries the provisioning payload.
        MfaEnrollment = enrollment is null ? null : new SignInMfaEnrollmentOffer(enrollment.SecretKey,
            enrollment.QrCodeUri, enrollment.SetupExpiresAt);
        UserId = verifiedSubject.Id;
        Email = verifiedSubject.Email;
        TenantId = tenantId;
        // Preserve the first-factor response contract without granting a session or disclosing the stored phone.
        User = AuthenticationMappings.CreateUserProfile(verifiedSubject, verifiedSubject.Id, verifiedSubject.Email,
            authenticationComplete: false, fallbackCreatedAt: verifiedSubject.CreatedAt);
    }
}

/// <summary>One-time TOTP provisioning offered at sign-in when a required-MFA subject has no enrolled factor.</summary>
public sealed record SignInMfaEnrollmentOffer(string SecretKey, string QrCodeUri, DateTimeOffset ExpiresAt);

internal sealed class SignInMfaCommittedDenial : SignInResponse, ICommitOnFailureOutcome
{
    public SignInMfaCommittedDenial()
    {
        Success = false;
        Message = "Additional verification failed";
    }
}
