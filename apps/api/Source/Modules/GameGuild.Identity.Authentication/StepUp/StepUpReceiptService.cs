using System.Security.Cryptography;
using System.Text;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

public sealed class StepUpReceiptService(
    IActorContextAccessor actorContextAccessor,
    IStepUpChallengeStore challengeStore,
    IMfaService mfaService,
    IWebAuthnAuthenticationService webAuthnService,
    TimeProvider timeProvider) : IStepUpReceiptService
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public async Task<StepUpChallengeResponse> CreateChallengeAsync(
        StepUpOperationBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        var subject = RequireSubject();
        var now = timeProvider.GetUtcNow();
        var challenge = new StepUpChallenge
        {
            Id = Guid.NewGuid(),
            TenantId = subject.TenantId,
            ActorId = subject.ActorId,
            SessionId = subject.SessionId,
            OperationType = binding.OperationType,
            TargetReference = binding.TargetReference,
            PayloadHash = binding.PayloadHash,
            CreatedAt = now,
            ExpiresAt = now.Add(ChallengeLifetime)
        };

        await challengeStore.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        return new StepUpChallengeResponse(challenge.Id, challenge.ExpiresAt);
    }

    public async Task<WebAuthnAuthenticationOptionsResult> BeginWebAuthnAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default)
    {
        var subject = RequireSubject();
        await RequireActiveChallengeAsync(challengeId, subject, cancellationToken).ConfigureAwait(false);
        var result = await webAuthnService.BeginAuthenticationAsync(
            userId: subject.ActorId,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new StepUpVerificationFailedException(result.Error ?? "WebAuthn challenge could not be created.");
        }

        return result;
    }

    public async Task<StepUpReceiptResponse> VerifyAsync(
        Guid challengeId,
        StepUpVerification verification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verification);
        var subject = RequireSubject();
        var challenge = await RequireActiveChallengeAsync(challengeId, subject, cancellationToken).ConfigureAwait(false);

        var verified = verification.Method switch
        {
            MfaMethod.Totp or MfaMethod.BackupCode => await VerifyMfaAsync(
                subject.ActorId,
                verification,
                cancellationToken).ConfigureAwait(false),
            MfaMethod.WebAuthn => await VerifyWebAuthnAsync(
                subject.ActorId,
                verification.Evidence,
                cancellationToken).ConfigureAwait(false),
            _ => throw new StepUpVerificationFailedException("The requested MFA method cannot authorize step-up operations.")
        };

        if (!verified)
        {
            throw new StepUpVerificationFailedException("MFA evidence is invalid.");
        }

        var receipt = CreateOpaqueReceipt();
        var now = timeProvider.GetUtcNow();
        var persisted = await challengeStore.MarkVerifiedAsync(
            challengeId,
            subject,
            Hash(receipt),
            verification.Method,
            now,
            cancellationToken).ConfigureAwait(false);
        if (!persisted)
        {
            throw new StepUpChallengeUnavailableException("Step-up challenge is expired or already verified.");
        }

        return new StepUpReceiptResponse(receipt, challenge.ExpiresAt);
    }

    public async Task ConsumeAsync(
        StepUpOperationBinding binding,
        string receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt);
        var consumed = await challengeStore.ConsumeAsync(
            RequireSubject(),
            binding,
            Hash(receipt),
            timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);
        if (!consumed)
        {
            throw new StepUpReceiptInvalidException("Step-up receipt is invalid, expired, mismatched, or already consumed.");
        }
    }

    private async Task<StepUpChallenge> RequireActiveChallengeAsync(
        Guid challengeId,
        StepUpSubject subject,
        CancellationToken cancellationToken)
    {
        if (challengeId == Guid.Empty)
        {
            throw new StepUpChallengeUnavailableException("Step-up challenge is unavailable.");
        }

        return await challengeStore.FindActiveAsync(
                challengeId,
                subject,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false)
            ?? throw new StepUpChallengeUnavailableException("Step-up challenge is unavailable.");
    }

    private StepUpSubject RequireSubject()
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated ||
            actor.SubjectIdAsGuid is not { } actorId ||
            actor.TenantId is not { } tenantId ||
            !Guid.TryParse(actor.TypedAttributes.SessionId, out var sessionId))
        {
            throw new StepUpContextUnavailableException("An authenticated tenant and session context is required.");
        }

        return new StepUpSubject(tenantId, actorId, sessionId);
    }

    private async Task<bool> VerifyMfaAsync(
        Guid actorId,
        StepUpVerification verification,
        CancellationToken cancellationToken)
    {
        var result = await mfaService.VerifyMfaAsync(
            actorId,
            verification.Evidence,
            verification.Method,
            cancellationToken).ConfigureAwait(false);
        return result.Success;
    }

    private async Task<bool> VerifyWebAuthnAsync(
        Guid actorId,
        string assertion,
        CancellationToken cancellationToken)
    {
        var result = await webAuthnService.CompleteAuthenticationAsync(
            assertion,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.Success && result.UserId == actorId;
    }

    private static string CreateOpaqueReceipt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
