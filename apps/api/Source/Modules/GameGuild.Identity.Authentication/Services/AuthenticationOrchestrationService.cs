using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Full authentication orchestration over the module's existing authentication services.
///     Validates the primary credential, evaluates risk with the existing anomaly analysis,
///     persists flow state with a bounded lifetime, delegates every challenge verification to
///     the existing MFA/WebAuthn services, and issues tokens through <see cref="IAuthenticatedSessionIssuer"/>
///     only when every required step is satisfied on an unexpired flow.
/// </summary>
public sealed class AuthenticationOrchestrationService(
    IAuthenticationFlowStateRepository flowStateRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuthAttemptService authAttemptService,
    IUserEnumerationProtectionService enumerationProtectionService,
    IAuthenticationAnomalyDetectionService anomalyDetectionService,
    ITrustedDeviceRepository trustedDeviceRepository,
    ITotpMfaService totpMfaService,
    IBackupCodeMfaService backupCodeMfaService,
    IWebAuthnAuthenticationService webAuthnAuthenticationService,
    IMfaAttemptTrackingService mfaAttemptTrackingService,
    IUserMfaConfigurationRepository userMfaConfigurationRepository,
    IAuthenticatedSessionIssuer sessionIssuer,
    ISignInMfaSubjectReader currentSubjects,
    IMfaSubjectRequirementPolicy mfaPolicy,
    OrchestratedRiskChallengeVerifier riskChallenges,
    ILogger<AuthenticationOrchestrationService> logger,
    ISuspiciousLoginAlertPublisher? suspiciousLoginAlerts = null,
    IPublisher? eventPublisher = null) : IAuthenticationOrchestrationService
{
    /// <summary>Ordinary flow lifetime; MFA-bound flows expire after five minutes.</summary>
    public static readonly TimeSpan FlowLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Risk score at or above which the strongest challenge set is required.</summary>
    public const double HighRiskChallengeThreshold = 0.7;

    /// <summary>Risk score at or above which MFA verification is required.</summary>
    public const double MfaChallengeThreshold = 0.5;

    private const string LocalAuthMethod = "Local";
    private const string PasswordContextKey = "password";
    private const string TotpCodeKey = "totpCode";
    private const string BackupCodeKey = "backupCode";
    private const string WebAuthnAssertionKey = "webAuthnAssertion";

    private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.General);

    public Task<AuthenticationFlowState> InitiateAuthenticationAsync(InitiateAuthenticationRequest request) =>
        InitiateAuthenticationAsync(request, CancellationToken.None);

    public async Task<AuthenticationFlowState> InitiateAuthenticationAsync(InitiateAuthenticationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var identifier = request.Identifier.Trim();
        if (identifier.Length == 0)
        {
            throw new ArgumentException("An identifier is required to initiate an authentication flow.", nameof(request));
        }

        var user = await ValidatePrimaryCredentialAsync(request, identifier, cancellationToken).ConfigureAwait(false);

        var attemptContext = BuildAttemptContext(request, user.Id);
        var riskScore = await AssessRiskScoreAsync(user.Id, attemptContext, cancellationToken).ConfigureAwait(false);

        // Sign-in risk coverage (issue #1199): the orchestrated family previously never reached
        // the suspicious-login containment. Confirmed signals on the initiating attempt are
        // forwarded, and a high-risk flow (which forces the strongest challenge set) is reported
        // as a step-up signal — mirroring the local password path. Best-effort: the publisher
        // gates on the configured severity and swallows its own failures.
        await AlertInitiationRisksAsync(user.Id, request.TenantId, attemptContext, riskScore, cancellationToken).ConfigureAwait(false);

        var requiredSteps = new List<AuthenticationStep> { AuthenticationStep.PrimaryCredential };
        requiredSteps.AddRange(await DetermineRequiredChallengesAsync(user.Id, riskScore, attemptContext, cancellationToken).ConfigureAwait(false));

        var subject = await currentSubjects.ReadCurrentAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (subject is null || subject.User.IsDeleted || !subject.User.ValidateForAuthentication(user.TokenVersion).IsSuccess ||
            subject.LockedOutUntil is { } lockedUntil && lockedUntil > SystemClock.UtcNow)
        {
            throw new AuthenticationRequiredException("Verified first-factor evidence is no longer valid.");
        }
        var decision = await mfaPolicy.EvaluateAsync(user.Id, request.TenantId, cancellationToken).ConfigureAwait(false);
        RequireDecision(decision, user.Id, request.TenantId);
        var requiresMfa = subject.HasEnrolledMfa || decision.RequiresMfa || requiredSteps.Contains(AuthenticationStep.MfaVerification);
        if (requiresMfa && !subject.HasEnrolledMfa)
        {
            throw new AuthenticationRequiredException("MFA setup is required before this sign-in can continue.");
        }
        if (requiresMfa && !requiredSteps.Contains(AuthenticationStep.MfaVerification))
        {
            requiredSteps.Add(AuthenticationStep.MfaVerification);
            requiredSteps = requiredSteps.OrderBy(step => (int)step).ToList();
        }
        var binding = new FlowBinding(decision.TenantId, user.TokenVersion, decision.PolicyFingerprint, subject.HasEnrolledMfa);
        var privateData = new Dictionary<string, object> { [BindingKey] = JsonSerializer.SerializeToElement(binding, SerializationOptions) };

        var completedSteps = new List<AuthenticationStep> { AuthenticationStep.PrimaryCredential };
        var now = SystemClock.UtcNow;

        var record = new AuthenticationFlowStateRecord
        {
            FlowId = Guid.NewGuid(),
            UserId = user.Id,
            CurrentStep = requiredSteps.Except(completedSteps).DefaultIfEmpty(AuthenticationStep.PrimaryCredential).First(),
            RequiredStepsJson = SerializeSteps(requiredSteps),
            CompletedStepsJson = SerializeSteps(completedSteps),
            IsComplete = false,
            RiskScore = riskScore,
            InitiatedAt = now,
            ExpiresAt = now.Add(requiresMfa ? TimeSpan.FromMinutes(5) : FlowLifetime),
            StepDataJson = JsonSerializer.Serialize(privateData, SerializationOptions),
            IpAddress = request.IpAddress,
            DeviceFingerprint = request.DeviceFingerprint
        };

        if (requiredSteps.Contains(AuthenticationStep.RiskChallenge))
        {
            var challenge = await riskChallenges.CreateAsync(record.FlowId, user, binding.TenantId,
                new DateTimeOffset(now), new DateTimeOffset(record.ExpiresAt), cancellationToken).ConfigureAwait(false);
            privateData[RiskChallengeKey] = JsonSerializer.SerializeToElement(challenge, SerializationOptions);
            record.StepDataJson = JsonSerializer.Serialize(privateData, SerializationOptions);
        }

        await flowStateRepository.CreateAsync(record, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Authentication flow {FlowId} initiated for user {UserId} with risk score {RiskScore} and steps {RequiredSteps}",
            record.FlowId, user.Id, riskScore, string.Join(",", requiredSteps));

        return ToState(record);
    }

    public Task<List<AuthenticationStep>> DetermineRequiredChallengesAsync(Guid userId, double riskScore, AuthenticationAttemptContext context) =>
        DetermineRequiredChallengesAsync(userId, riskScore, context, CancellationToken.None);

    public async Task<List<AuthenticationStep>> DetermineRequiredChallengesAsync(Guid userId, double riskScore, AuthenticationAttemptContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        var challenges = new List<AuthenticationStep>();
        if (riskScore >= HighRiskChallengeThreshold)
        {
            challenges.Add(AuthenticationStep.MfaVerification);
            challenges.Add(AuthenticationStep.RiskChallenge);
        }
        else if (riskScore >= MfaChallengeThreshold)
        {
            challenges.Add(AuthenticationStep.MfaVerification);
        }

        // An unrecognized device (missing or untrusted fingerprint) requires device trust.
        var fingerprint = context.DeviceFingerprint;
        var deviceRecognized = !string.IsNullOrWhiteSpace(fingerprint)
            && await trustedDeviceRepository.IsDeviceTrustedAsync(userId, fingerprint, cancellationToken).ConfigureAwait(false);
        if (!deviceRecognized)
        {
            challenges.Add(AuthenticationStep.DeviceTrust);
        }

        return [.. challenges.Distinct().OrderBy(step => (int)step)];
    }

    public Task<AuthenticationFlowState> ProcessAuthenticationStepAsync(Guid flowId, AuthenticationStep step, object stepData) =>
        ProcessAuthenticationStepAsync(flowId, step, stepData, CancellationToken.None);

    public async Task<AuthenticationFlowState> ProcessAuthenticationStepAsync(Guid flowId, AuthenticationStep step, object stepData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await LoadActiveFlowAsync(flowId, cancellationToken).ConfigureAwait(false);
        var state = ToState(record);

        if (step == AuthenticationStep.PrimaryCredential)
        {
            // The primary credential is validated during initiation and can never be re-processed.
            throw new InvalidOperationException("The primary credential step is satisfied by flow initiation and cannot be processed.");
        }

        if (record.UserId is not { } userId)
        {
            throw new AuthenticationRequiredException($"Authentication flow '{flowId}' has no identified user.");
        }

        EnforceStepOrder(state, step);

        if (await mfaAttemptTrackingService.IsUserLockedOutAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            throw new AuthenticationRequiredException("MFA verification is temporarily locked out for this account.");
        }

        var (_, binding) = await ReadBoundSubjectAsync(record, cancellationToken).ConfigureAwait(false);
        MfaMethod method;
        if (step == AuthenticationStep.RiskChallenge)
        {
            if (!await VerifyRiskChallengeAsync(record, binding, stepData, cancellationToken).ConfigureAwait(false))
            {
                return new OrchestrationChallengeDeniedState(ToState(record));
            }
            method = MfaMethod.Email;
        }
        else
        {
            method = await VerifyChallengeAsync(record, step, stepData, cancellationToken).ConfigureAwait(false);
        }
        await ReadBoundSubjectAsync(record, cancellationToken).ConfigureAwait(false);

        var completedSteps = DeserializeSteps(record.CompletedStepsJson);
        completedSteps.Add(step);
        var requiredSteps = DeserializeSteps(record.RequiredStepsJson);
        var isComplete = requiredSteps.All(completedSteps.Contains);

        var stepDataMap = DeserializeStepData(record.StepDataJson) ?? [];
        stepDataMap[step.ToString()] = method.ToString();
        if (step == AuthenticationStep.MfaVerification)
        {
            stepDataMap[MfaVerifiedAtKey] = SystemClock.UtcNow;
        }

        record.CompletedStepsJson = SerializeSteps(completedSteps);
        record.CurrentStep = requiredSteps.Except(completedSteps).DefaultIfEmpty(step).First();
        record.IsComplete = isComplete;
        record.StepDataJson = JsonSerializer.Serialize(stepDataMap, SerializationOptions);

        await flowStateRepository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Authentication flow {FlowId} step {Step} satisfied via {Method}; completed {CompletedCount}/{RequiredCount}",
            flowId, step, method, completedSteps.Count, requiredSteps.Count);

        return ToState(record);
    }

    public Task<AuthenticationResult> CompleteAuthenticationAsync(Guid flowId) =>
        CompleteAuthenticationAsync(flowId, CancellationToken.None);

    public async Task<AuthenticationResult> CompleteAuthenticationAsync(Guid flowId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await LoadActiveFlowAsync(flowId, cancellationToken).ConfigureAwait(false);
        var state = ToState(record);

        var pending = state.RequiredSteps.Where(required => !state.CompletedSteps.Contains(required)).ToList();
        if (pending.Count > 0)
        {
            throw new InvalidOperationException(
                $"Authentication flow '{flowId}' cannot complete while steps [{string.Join(", ", pending)}] are pending.");
        }

        if (record.UserId is not { } userId)
        {
            throw new AuthenticationRequiredException($"Authentication flow '{flowId}' has no identified user.");
        }

        var (subject, binding) = await ReadBoundSubjectAsync(record, cancellationToken).ConfigureAwait(false);
        var user = subject.User;
        var device = new DeviceInfo
        {
            Fingerprint = string.IsNullOrWhiteSpace(record.DeviceFingerprint) ? Guid.NewGuid().ToString() : record.DeviceFingerprint,
            IpAddress = string.IsNullOrWhiteSpace(record.IpAddress) ? "unknown" : record.IpAddress,
            UserAgent = "AuthenticationOrchestration"
        };
        SignInResponse issued;
        if (state.RequiredSteps.Contains(AuthenticationStep.MfaVerification) || subject.HasEnrolledMfa)
        {
            var data = DeserializeStepData(record.StepDataJson);
            if (!state.CompletedSteps.Contains(AuthenticationStep.MfaVerification) || data is null ||
                !data.TryGetValue(AuthenticationStep.MfaVerification.ToString(), out var methodValue) ||
                !Enum.TryParse<MfaMethod>(methodValue.ToString(), out var method) ||
                !data.TryGetValue(MfaVerifiedAtKey, out var timestamp) ||
                !DateTimeOffset.TryParse(timestamp.ToString(), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var verifiedAt))
            {
                throw new AuthenticationRequiredException("Verified MFA evidence is required.");
            }
            var proof = new SignInMfaProof(record.FlowId, user.Id, binding.TenantId, binding.TokenVersion,
                binding.PolicyFingerprint, SignInFirstFactor.Password, new DateTimeOffset(record.InitiatedAt), verifiedAt, method);
            issued = await sessionIssuer.IssueMfaAsync(user, binding.TenantId, device, proof, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            issued = await sessionIssuer.IssueAsync(user, binding.TenantId, device, cancellationToken).ConfigureAwait(false);
        }
        if (issued is null || !issued.Success || issued.RequiresMfa || issued.RequiresStepUp || issued.UserId != user.Id ||
            issued.TenantId != binding.TenantId || issued.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(issued.AccessToken) ||
            string.IsNullOrWhiteSpace(issued.RefreshToken) || issued.AccessTokenExpiresAt <= SystemClock.UtcNow)
        {
            throw new InvalidOperationException("Authenticated credentials could not be issued for the verified binding.");
        }

        record.IsComplete = true;
        record.CompletedAt = SystemClock.UtcNow;
        record.CurrentStep = state.RequiredSteps.DefaultIfEmpty(AuthenticationStep.PrimaryCredential).Last();
        await flowStateRepository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Authentication flow {FlowId} completed; tokens issued for user {UserId}", flowId, userId);

        return new OrchestrationAuthenticationResult
        {
            IssuedCredentials = issued,
            IsSuccess = true,
            UserId = userId,
            AccessToken = issued.AccessToken,
            RefreshToken = issued.RefreshToken,
            TokenExpiresAt = issued.AccessTokenExpiresAt,
            SessionId = issued.SessionId,
            MfaEnabled = state.RequiredSteps.Contains(AuthenticationStep.MfaVerification),
            DeviceTrusted = !state.RequiredSteps.Contains(AuthenticationStep.DeviceTrust),
            RiskScore = state.RiskScore,
            Metadata = new Dictionary<string, object>
            {
                ["flowId"] = flowId,
                ["requiredSteps"] = string.Join(",", state.RequiredSteps)
            }
        };
    }

    public Task AbandonAuthenticationFlowAsync(Guid flowId) =>
        AbandonAuthenticationFlowAsync(flowId, CancellationToken.None);

    public async Task AbandonAuthenticationFlowAsync(Guid flowId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await flowStateRepository.GetForUpdateAsync(flowId, cancellationToken).ConfigureAwait(false);
        if (record is null || record.CompletedAt is not null || record.AbandonedAt is not null) { return; }
        await flowStateRepository.AbandonAsync(flowId, SystemClock.UtcNow, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Authentication flow {FlowId} abandoned", flowId);
    }

    public Task<AuthenticationFlowState?> GetFlowStateAsync(Guid flowId) =>
        GetFlowStateAsync(flowId, CancellationToken.None);

    public async Task<AuthenticationFlowState?> GetFlowStateAsync(Guid flowId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await flowStateRepository.GetByFlowIdAsync(flowId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        // Fail closed: abandoned and expired flows are not observable.
        if (record.AbandonedAt is not null)
        {
            return null;
        }

        if (SystemClock.UtcNow >= record.ExpiresAt)
        {
            return null;
        }

        return ToState(record);
    }

    private const string BindingKey = "__serverBinding";
    private const string MfaVerifiedAtKey = "__mfaVerifiedAt";
    private const string RiskChallengeKey = "__riskChallenge";
    private const string RiskCodeKey = "riskCode";
    private sealed record FlowBinding(Guid TenantId, int TokenVersion, string PolicyFingerprint, bool HadMfa);

    private static void RequireDecision(MfaRequirementDecision decision, Guid userId, Guid? tenantId)
    {
        if (decision is null || decision.SubjectId != userId || decision.TenantId == Guid.Empty ||
            tenantId.HasValue && decision.TenantId != tenantId || !SignInMfaChallengeToken.IsDigest(decision.PolicyFingerprint))
        {
            throw new AuthenticationRequiredException("No authorized tenant is available for authentication.");
        }
    }

    private async Task<(SignInMfaSubjectState Subject, FlowBinding Binding)> ReadBoundSubjectAsync(AuthenticationFlowStateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var data = DeserializeStepData(record.StepDataJson);
        if (record.UserId is not { } userId || data is null || !data.TryGetValue(BindingKey, out var raw))
        {
            throw new AuthenticationRequiredException("This sign-in must be restarted.");
        }
        FlowBinding? binding;
        try { binding = JsonSerializer.Deserialize<FlowBinding>(raw.ToString() ?? string.Empty, SerializationOptions); }
        catch (JsonException) { throw new AuthenticationRequiredException("This sign-in must be restarted."); }
        var subject = await currentSubjects.ReadCurrentAsync(userId, cancellationToken).ConfigureAwait(false);
        if (binding is null || subject is null || subject.User.IsDeleted ||
            !subject.User.ValidateForAuthentication(binding.TokenVersion).IsSuccess || subject.User.Id != userId ||
            subject.HasEnrolledMfa != binding.HadMfa || subject.LockedOutUntil is { } lockedUntil && lockedUntil > SystemClock.UtcNow)
        {
            throw new AuthenticationRequiredException("Verified first-factor evidence is no longer valid.");
        }
        var decision = await mfaPolicy.EvaluateAsync(userId, binding.TenantId, cancellationToken).ConfigureAwait(false);
        RequireDecision(decision, userId, binding.TenantId);
        if (!string.Equals(binding.PolicyFingerprint, decision.PolicyFingerprint, StringComparison.Ordinal) ||
            (subject.HasEnrolledMfa || decision.RequiresMfa) && !DeserializeSteps(record.RequiredStepsJson).Contains(AuthenticationStep.MfaVerification))
        {
            throw new AuthenticationRequiredException("The sign-in policy changed; restart authentication.");
        }
        return (subject, binding);
    }

    private async Task<bool> VerifyRiskChallengeAsync(AuthenticationFlowStateRecord record, FlowBinding binding, object stepData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var data = DeserializeStepData(record.StepDataJson);
        if (data is null || !data.TryGetValue(RiskChallengeKey, out var raw))
        {
            throw new AuthenticationRequiredException("This sign-in must be restarted.");
        }
        OrchestratedRiskChallengeState? challenge;
        try { challenge = JsonSerializer.Deserialize<OrchestratedRiskChallengeState>(raw.ToString() ?? string.Empty, SerializationOptions); }
        catch (JsonException) { throw new AuthenticationRequiredException("This sign-in must be restarted."); }
        if (challenge is null) { throw new AuthenticationRequiredException("This sign-in must be restarted."); }
        var code = string.Empty;
        if (stepData is IReadOnlyDictionary<string, object> payload && payload.Count == 1)
        {
            TryReadPayload(payload, RiskCodeKey, out code);
        }
        var result = riskChallenges.Verify(challenge, record.FlowId, record.UserId!.Value, binding.TenantId,
            code, new DateTimeOffset(SystemClock.UtcNow));
        data[RiskChallengeKey] = JsonSerializer.SerializeToElement(result.State, SerializationOptions);
        record.StepDataJson = JsonSerializer.Serialize(data, SerializationOptions);
        if (!result.Accepted)
        {
            if (result.State.FailedAttempts >= OrchestratedRiskChallengeVerifier.MaximumFailedAttempts ||
                result.State.ExpiresAt <= new DateTimeOffset(SystemClock.UtcNow))
            {
                record.AbandonedAt = SystemClock.UtcNow;
            }
            await flowStateRepository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
        }
        return result.Accepted;
    }

    private static Dictionary<string, object>? PublicStepData(string? json)
    {
        var data = DeserializeStepData(json);
        if (data is null) { return null; }
        data.Remove(BindingKey);
        data.Remove(MfaVerifiedAtKey);
        data.Remove(RiskChallengeKey);
        return data.Count == 0 ? null : data;
    }

    private async Task<GameGuild.Identity.Users.User> ValidatePrimaryCredentialAsync(InitiateAuthenticationRequest request, string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(request.AuthMethod, LocalAuthMethod, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Authentication method '{request.AuthMethod}' is not orchestrated; only '{LocalAuthMethod}' primary credentials are supported.");
        }

        if (!TryReadPayload(request.Context, PasswordContextKey, out var password))
        {
            throw new AuthenticationRequiredException("A primary credential is required to initiate an authentication flow.");
        }

        // Server-owned monotonic origin: captured BEFORE account resolution so the lookup and
        // credential verification stay inside the compensated window.
        var timingScope = enumerationProtectionService.BeginAuthenticationTiming();
        var user = await userRepository.GetByEmailAsync(identifier.ToLowerInvariant(), cancellationToken).ConfigureAwait(false);

        // Work classification, not account existence: passwordless accounts and credentials the
        // hasher rejects before expensive verification performed no cryptographic work.
        var verification = user is { IsActive: true, HasPassword: true }
            ? passwordHasher.VerifyPasswordWithWorkClassification(user.PasswordHash!, password)
            : PasswordVerificationResult.RejectedWithoutWork;

        if (!verification.IsValid)
        {
            await enumerationProtectionService.AddTimingProtectionDelayAsync(
                timingScope,
                verification.PerformedCryptographicWork ? CredentialWorkClassification.Completed : CredentialWorkClassification.None).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await authAttemptService.RecordFailedAttemptAsync(
                identifier,
                user?.Id,
                request.IpAddress,
                request.UserAgent,
                "InvalidCredentials",
                TimeSpan.Zero,
                request.AuthMethod).ConfigureAwait(false);

            // In-process failure telemetry (issue #1199): the event carries the failure reason
            // and request context; the thrown response stays the generic enumeration-protected
            // message below. Known-account brute force on the failed attempt also reaches the
            // suspicious-login containment, mirroring the local password path.
            await AuthenticationEventDispatch.PublishAsync(
                eventPublisher,
                new AuthenticationFailedEvent(identifier, "InvalidCredentials", request.IpAddress, request.UserAgent, SystemClock.UtcNow),
                logger,
                cancellationToken).ConfigureAwait(false);
            await AlertFailedAttemptRisksAsync(user?.Id, request, cancellationToken).ConfigureAwait(false);

            logger.LogWarning("Primary credential validation failed for identifier {Identifier} from {IpAddress}", identifier, request.IpAddress);
            throw new AuthenticationRequiredException(enumerationProtectionService.GetGenericErrorMessage("login"));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await authAttemptService.RecordSuccessfulAttemptAsync(
            user!.Email,
            user.Id,
            request.IpAddress,
            request.UserAgent,
            TimeSpan.Zero,
            request.AuthMethod).ConfigureAwait(false);

        return user;
    }

    /// <summary>
    ///     Forwards the confirmed risk signals of an initiating orchestrated flow to the durable
    ///     suspicious-login alert publisher (issue #1199). A flow at or above the high-risk
    ///     threshold is forced into the strongest challenge set, which is the step-up signal.
    /// </summary>
    private async Task AlertInitiationRisksAsync(Guid userId, Guid? tenantId, AuthenticationAttemptContext context, double riskScore, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (suspiciousLoginAlerts is null)
        {
            return;
        }

        try
        {
            var analysis = await anomalyDetectionService.AnalyzeLoginAttemptAsync(context).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await AuthenticationEventDispatch.AlertConfirmedSignInRisksAsync(
                suspiciousLoginAlerts, userId, tenantId, analysis, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Defense-in-depth: alert evaluation must never fail flow initiation.
            logger.LogError(exception, "Could not evaluate sign-in risk alerts for user {UserId}", userId);
        }

        if (riskScore >= HighRiskChallengeThreshold)
        {
            await suspiciousLoginAlerts.RecordAsync(
                userId, tenantId, SecurityAlertKinds.LoginStepUpRequired,
                RiskLevel.High, (int)Math.Round(riskScore * 100), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Evaluates a failed orchestrated credential attempt for the brute-force signal and
    ///     forwards it to the suspicious-login containment (issue #1199). Best-effort.
    /// </summary>
    private async Task AlertFailedAttemptRisksAsync(Guid? userId, InitiateAuthenticationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (suspiciousLoginAlerts is null || userId is not { } alertUserId)
        {
            return;
        }

        try
        {
            var analysis = await anomalyDetectionService.AnalyzeLoginAttemptAsync(BuildAttemptContext(request, alertUserId)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await AuthenticationEventDispatch.AlertConfirmedSignInRisksAsync(
                suspiciousLoginAlerts, alertUserId, request.TenantId, analysis, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not evaluate failed-attempt risk alerts for user {UserId}", userId);
        }
    }

    private async Task<double> AssessRiskScoreAsync(Guid userId, AuthenticationAttemptContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var analysis = await anomalyDetectionService.AnalyzeLoginAttemptAsync(context).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return Math.Clamp(analysis.RiskScore / 100.0, 0.0, 1.0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Fail closed: when risk analysis is unavailable the flow must assume maximum risk.
            logger.LogError(exception, "Risk analysis failed for user {UserId}; assuming maximum risk score", userId);
            return 1.0;
        }
    }

    private async Task<AuthenticationFlowStateRecord> LoadActiveFlowAsync(Guid flowId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await flowStateRepository.GetForUpdateAsync(flowId, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationRequiredException($"Authentication flow '{flowId}' was not found.");

        if (record.AbandonedAt is not null)
        {
            throw new AuthenticationRequiredException($"Authentication flow '{flowId}' was abandoned.");
        }

        if (record.CompletedAt is not null)
        {
            throw new AuthenticationRequiredException($"Authentication flow '{flowId}' was already completed.");
        }

        if (SystemClock.UtcNow >= record.ExpiresAt)
        {
            throw new AuthenticationRequiredException($"Authentication flow '{flowId}' has expired.");
        }

        return record;
    }

    private async Task<MfaMethod> VerifyChallengeAsync(AuthenticationFlowStateRecord record, AuthenticationStep step, object stepData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (stepData is not IReadOnlyDictionary<string, object> payload)
        {
            logger.LogWarning("Step {Step} for flow {FlowId} received no verification payload", step, record.FlowId);
            throw new AuthenticationRequiredException($"Step '{step}' requires a verification payload.");
        }

        var userId = record.UserId!.Value;
        var deviceId = record.DeviceFingerprint;

        if (TryReadPayload(payload, TotpCodeKey, out var totpCode))
        {
            var verified = await totpMfaService.VerifyTotpAsync(userId, totpCode, deviceId, cancellationToken).ConfigureAwait(false);
            if (!verified)
            {
                throw await FailChallengeAsync(record, MfaMethod.Totp, "invalid TOTP code", cancellationToken).ConfigureAwait(false);
            }

            await RecordSuccessAttemptAsync(userId, MfaMethod.Totp, deviceId, cancellationToken).ConfigureAwait(false);
            return MfaMethod.Totp;
        }

        if (TryReadPayload(payload, BackupCodeKey, out var backupCode))
        {
            var verified = await backupCodeMfaService.VerifyBackupCodeAsync(userId, backupCode, deviceId, cancellationToken).ConfigureAwait(false);
            if (!verified)
            {
                throw await FailChallengeAsync(record, MfaMethod.BackupCode, "invalid backup code", cancellationToken).ConfigureAwait(false);
            }

            await RecordSuccessAttemptAsync(userId, MfaMethod.BackupCode, deviceId, cancellationToken).ConfigureAwait(false);
            return MfaMethod.BackupCode;
        }

        if (TryReadPayload(payload, WebAuthnAssertionKey, out var assertion))
        {
            var result = await webAuthnAuthenticationService
                .CompleteAuthenticationAsync(assertion, record.IpAddress, userAgent: null, cancellationToken)
                .ConfigureAwait(false);
            if (result is not { Success: true } || result.UserId != userId)
            {
                throw await FailChallengeAsync(record, MfaMethod.WebAuthn, "invalid WebAuthn assertion", cancellationToken).ConfigureAwait(false);
            }

            await RecordSuccessAttemptAsync(userId, MfaMethod.WebAuthn, deviceId, cancellationToken).ConfigureAwait(false);
            return MfaMethod.WebAuthn;
        }

        logger.LogWarning("Step {Step} for flow {FlowId} received no supported verification payload", step, record.FlowId);
        throw new AuthenticationRequiredException($"Step '{step}' received no supported verification payload.");
    }

    /// <summary>
    ///     Records a failed challenge attempt through the existing MFA attempt tracking (which applies
    ///     the existing lockout policy) and returns the exception to throw.
    /// </summary>
    private async Task<AuthenticationRequiredException> FailChallengeAsync(
        AuthenticationFlowStateRecord record,
        MfaMethod method,
        string failureReason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = record.UserId!.Value;
        logger.LogWarning(
            "Challenge {Step} failed for flow {FlowId} via {Method}: {Reason}",
            record.CurrentStep, record.FlowId, method, failureReason);

        try
        {
            var configuration = await userMfaConfigurationRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (configuration is not null)
            {
                await mfaAttemptTrackingService
                    .RecordFailedMfaAttemptAsync(configuration, method, failureReason, record.DeviceFingerprint, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await mfaAttemptTrackingService
                    .RecordMfaAttemptAsync(userId, method, success: false, failureReason, record.DeviceFingerprint, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not record failed challenge attempt for flow {FlowId}", record.FlowId);
        }

        return new AuthenticationRequiredException(failureReason);
    }

    private async Task RecordSuccessAttemptAsync(Guid userId, MfaMethod method, string? deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await mfaAttemptTrackingService
                .RecordMfaAttemptAsync(userId, method, success: true, failureReason: null, deviceId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not record successful challenge attempt for user {UserId}", userId);
        }
    }

    private static void EnforceStepOrder(AuthenticationFlowState state, AuthenticationStep step)
    {
        if (!state.RequiredSteps.Contains(step))
        {
            throw new InvalidOperationException($"Step '{step}' is not required for this authentication flow.");
        }

        if (state.CompletedSteps.Contains(step))
        {
            throw new InvalidOperationException($"Step '{step}' has already been completed.");
        }

        if (state.NextStep is { } next && next != step)
        {
            throw new InvalidOperationException($"Step '{step}' cannot be processed before '{next}'.");
        }
    }

    private static AuthenticationAttemptContext BuildAttemptContext(InitiateAuthenticationRequest request, Guid userId) => new()
    {
        UserId = userId,
        Identifier = request.Identifier.ToLowerInvariant(),
        AuthenticationMethod = request.AuthMethod,
        IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress,
        UserAgent = string.IsNullOrWhiteSpace(request.UserAgent) ? "Unknown" : request.UserAgent,
        DeviceFingerprint = request.DeviceFingerprint,
        TenantId = request.TenantId,
        AttemptedAt = SystemClock.UtcNow
    };

    private static bool TryReadPayload(IReadOnlyDictionary<string, object>? payload, string key, out string value)
    {
        value = string.Empty;
        if (payload is null || !payload.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        var text = raw.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text.Trim();
        return true;
    }

    private static AuthenticationFlowState ToState(AuthenticationFlowStateRecord record) => new OrchestrationAuthenticationFlowState
    {
        FlowId = record.FlowId,
        UserId = record.UserId,
        CurrentStep = record.CurrentStep,
        RequiredSteps = DeserializeSteps(record.RequiredStepsJson),
        CompletedSteps = DeserializeSteps(record.CompletedStepsJson),
        RiskScore = record.RiskScore,
        InitiatedAt = record.InitiatedAt,
        ExpiresAt = record.ExpiresAt,
        IpAddress = record.IpAddress,
        DeviceFingerprint = record.DeviceFingerprint,
        StepData = PublicStepData(record.StepDataJson)
    };

    private static string SerializeSteps(IEnumerable<AuthenticationStep> steps) =>
        JsonSerializer.Serialize(steps.Distinct().OrderBy(step => (int)step).ToList(), SerializationOptions);

    private static List<AuthenticationStep> DeserializeSteps(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<AuthenticationStep>>(json, SerializationOptions) ?? [];

    private static Dictionary<string, object>? DeserializeStepData(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(json, SerializationOptions);
}
