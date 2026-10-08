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
    TimeProvider? timeProvider = null) : ISignInMfaService
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

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
        return await IssueAsync(subject, decision.TenantId, deviceInfo, cancellationToken).ConfigureAwait(false);
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

        var token = SignInMfaChallengeToken.Create();
        if (!SignInMfaChallengeToken.TryHash(token, out var hash)) { throw new InvalidOperationException("Challenge creation failed."); }
        var now = _time.GetUtcNow();
        var challenge = new SignInMfaChallenge
        {
            Id = Guid.NewGuid(), SubjectId = verifiedSubjectId, SubjectTokenVersion = verifiedTokenVersion,
            TenantId = decision.TenantId, PolicyFingerprint = decision.PolicyFingerprint, TokenHash = hash,
            FirstFactor = firstFactor, Purpose = current!.HasEnrolledMfa ? SignInMfaPurpose.VerifyFactor : SignInMfaPurpose.EnrollFactor,
            CreatedAt = now, ExpiresAt = now.AddMinutes(5)
        };
        await challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await RecordAsync("Authentication.MfaSignInRequired", challenge, false, firstFactor.ToString(), deviceInfo,
            null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // This marker is created only after challenge persistence and audit both succeed.
        return SignInMfaPreparation.WithOutcome(new SignInMfaPendingResponse(token, challenge.ExpiresAt, challenge.Purpose, requiresRiskStepUp));
    }

    public async Task<SignInResponse> CompleteCodeAsync(string bearer, string code, MfaMethod method,
        DeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(deviceInfo);
        if (method is not (MfaMethod.Totp or MfaMethod.BackupCode) || string.IsNullOrWhiteSpace(code) || code.Length > 64 ||
            !SignInMfaChallengeToken.TryHash(bearer, out var hash)) { return Denied(); }
        var challenge = await challenges.FindActiveAsync(hash, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (challenge is null || challenge.Purpose != SignInMfaPurpose.VerifyFactor) { return Denied(); }
        var current = await ReadBoundSubjectAsync(challenge, cancellationToken).ConfigureAwait(false);
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
        if (current is null || IsLocked(current)) { return Denied(); }
        var binding = new SignInMfaChallengeBinding(current.User.Id, challenge.TenantId, current.User.TokenVersion,
            challenge.PolicyFingerprint, SignInMfaPurpose.VerifyFactor);
        if (!await challenges.TryConsumeAsync(hash, binding, method, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false))
        {
            return Denied();
        }
        var result = await IssueAsync(current.User, challenge.TenantId, deviceInfo, cancellationToken).ConfigureAwait(false);
        await RecordAsync("Authentication.MfaSignInVerified", challenge, true, method.ToString(), deviceInfo,
            result.SessionId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private async Task<SignInMfaSubjectState?> ReadBoundSubjectAsync(SignInMfaChallenge challenge, CancellationToken cancellationToken)
    {
        var current = await subjects.ReadCurrentAsync(challenge.SubjectId, cancellationToken).ConfigureAwait(false);
        if (!AccountMatches(current, challenge.SubjectId, challenge.SubjectTokenVersion) || !current!.HasEnrolledMfa) { return null; }
        var decision = await policy.EvaluateAsync(challenge.SubjectId, challenge.TenantId, cancellationToken).ConfigureAwait(false);
        RequireDecision(decision, challenge.SubjectId, challenge.TenantId);
        return string.Equals(decision.PolicyFingerprint, challenge.PolicyFingerprint, StringComparison.Ordinal) ? current : null;
    }

    private async Task<SignInResponse> IssueAsync(User user, Guid tenantId, DeviceInfo device, CancellationToken cancellationToken)
    {
        var result = await issuer.IssueAsync(user, tenantId, device, cancellationToken).ConfigureAwait(false);
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

/// <summary>Server-only transaction outcome; the bearer is not an ordinary access or refresh credential.</summary>
internal sealed class SignInMfaPendingResponse : SignInResponse, ICommitOnFailureOutcome
{
    public SignInMfaPendingResponse(string bearer, DateTimeOffset expiresAt, SignInMfaPurpose purpose, bool requiresRiskStepUp)
    {
        Success = false;
        Message = purpose == SignInMfaPurpose.EnrollFactor ? "MFA enrollment required" : "Additional verification required";
        RequiresMfa = true;
        MfaToken = bearer;
        RequiresStepUp = requiresRiskStepUp;
        StepUpToken = requiresRiskStepUp ? bearer : null;
        StepUpExpiresAt = expiresAt.UtcDateTime;
        AvailableMethods = purpose == SignInMfaPurpose.VerifyFactor ? ["TOTP", "BackupCode"] : ["TOTP"];
    }
}

internal sealed class SignInMfaCommittedDenial : SignInResponse, ICommitOnFailureOutcome
{
    public SignInMfaCommittedDenial()
    {
        Success = false;
        Message = "Additional verification failed";
    }
}
