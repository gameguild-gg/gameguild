using System.Security.Cryptography;
using System.Text;
using GameGuild.Identity.Context.Actors;
using Moq;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

internal static class StepUpReceiptServiceTestHarness
{
    internal static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
    internal static readonly StepUpOperationBinding Binding = new(
        "economy.policy.approve",
        "policy/price-v4",
        new string('a', 64));

    internal static StepUpReceiptService CreateService(
        Guid tenantId,
        Guid actorId,
        Guid sessionId,
        RecordingStepUpChallengeStore store,
        IMfaService? mfa = null,
        IWebAuthnAuthenticationService? webAuthn = null)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForUser(actorId)
            .WithTenantId(tenantId)
            .WithAttribute("session_id", sessionId.ToString())
            .Build());
        return CreateService(accessor, store, mfa, webAuthn);
    }

    internal static StepUpReceiptService CreateService(
        IActorContextAccessor accessor,
        RecordingStepUpChallengeStore store,
        IMfaService? mfa = null,
        IWebAuthnAuthenticationService? webAuthn = null)
    {
        return new StepUpReceiptService(
            accessor,
            store,
            mfa ?? Mock.Of<IMfaService>(),
            webAuthn ?? Mock.Of<IWebAuthnAuthenticationService>(),
            new FixedTimeProvider(Now));
    }

    internal static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

internal sealed class RecordingStepUpChallengeStore : IStepUpChallengeStore
{
    public List<StepUpChallenge> Challenges { get; } = [];

    public Task<int> PruneExpiredAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
        => throw new NotSupportedException("Receipt-service tests do not implement SQL pruning.");

    public Task AddAsync(StepUpChallenge challenge, CancellationToken cancellationToken)
    {
        Challenges.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<StepUpChallenge?> FindActiveAsync(
        Guid challengeId,
        StepUpSubject subject,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Challenges.SingleOrDefault(challenge =>
            challenge.Id == challengeId &&
            challenge.TenantId == subject.TenantId &&
            challenge.ActorId == subject.ActorId &&
            challenge.SessionId == subject.SessionId &&
            challenge.ExpiresAt > now &&
            challenge.VerifiedAt is null &&
            challenge.VerificationAttempts < StepUpChallenge.MaximumVerificationAttempts &&
            challenge.ConsumedAt is null));
    }

    public Task<bool> TryReserveVerificationAttemptAsync(
        Guid challengeId, StepUpSubject subject, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Challenges)
        {
            var challenge = Challenges.SingleOrDefault(candidate => candidate.Id == challengeId &&
                candidate.TenantId == subject.TenantId && candidate.ActorId == subject.ActorId &&
                candidate.SessionId == subject.SessionId && candidate.VerifiedAt is null &&
                candidate.ConsumedAt is null && candidate.ExpiresAt > now &&
                candidate.VerificationAttempts < StepUpChallenge.MaximumVerificationAttempts);
            if (challenge is null)
            {
                return Task.FromResult(false);
            }
            challenge.VerificationAttempts++;
            return Task.FromResult(true);
        }
    }

    public Task<bool> MarkVerifiedAsync(
        Guid challengeId,
        StepUpSubject subject,
        string receiptHash,
        MfaMethod method,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken)
        => method == MfaMethod.WebAuthn
            ? Task.FromResult(false)
            : MarkVerified(challengeId, subject, receiptHash, method, null, verifiedAt);

    public Task<bool> BindWebAuthnChallengeAsync(
        Guid challengeId,
        StepUpSubject subject,
        string challengeHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var challenge = Challenges.SingleOrDefault(candidate => candidate.Id == challengeId &&
            candidate.TenantId == subject.TenantId && candidate.ActorId == subject.ActorId &&
            candidate.SessionId == subject.SessionId && candidate.VerifiedAt is null &&
            candidate.ConsumedAt is null && candidate.ExpiresAt > now);
        if (challenge is null)
        {
            return Task.FromResult(false);
        }
        challenge.WebAuthnChallengeHash = challengeHash;
        return Task.FromResult(true);
    }

    public Task<bool> MarkWebAuthnVerifiedAsync(
        Guid challengeId,
        StepUpSubject subject,
        string challengeHash,
        string receiptHash,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken)
        => MarkVerified(challengeId, subject, receiptHash, MfaMethod.WebAuthn, challengeHash, verifiedAt);

    private Task<bool> MarkVerified(
        Guid challengeId,
        StepUpSubject subject,
        string receiptHash,
        MfaMethod method,
        string? webAuthnChallengeHash,
        DateTimeOffset verifiedAt)
    {
        var challenge = Challenges.SingleOrDefault(candidate =>
            candidate.Id == challengeId &&
            candidate.TenantId == subject.TenantId &&
            candidate.ActorId == subject.ActorId &&
            candidate.SessionId == subject.SessionId &&
            candidate.VerifiedAt is null &&
            candidate.ConsumedAt is null &&
            (webAuthnChallengeHash is null || candidate.WebAuthnChallengeHash == webAuthnChallengeHash) &&
            candidate.ExpiresAt > verifiedAt);
        if (challenge is null)
            return Task.FromResult(false);

        challenge.VerifiedAt = verifiedAt;
        challenge.VerificationMethod = method;
        challenge.ReceiptHash = receiptHash;
        return Task.FromResult(true);
    }

    public Task<bool> ConsumeAsync(
        StepUpSubject subject,
        StepUpOperationBinding binding,
        string receiptHash,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken)
    {
        var challenge = Challenges.SingleOrDefault(candidate =>
            candidate.TenantId == subject.TenantId &&
            candidate.ActorId == subject.ActorId &&
            candidate.SessionId == subject.SessionId &&
            candidate.OperationType == binding.OperationType &&
            candidate.TargetReference == binding.TargetReference &&
            candidate.PayloadHash == binding.PayloadHash &&
            candidate.ReceiptHash == receiptHash &&
            candidate.VerifiedAt is not null &&
            candidate.ConsumedAt is null &&
            (candidate.VerificationMethod != MfaMethod.WebAuthn || candidate.WebAuthnChallengeHash is not null) &&
            candidate.ExpiresAt > consumedAt);
        if (challenge is null)
            return Task.FromResult(false);

        challenge.ConsumedAt = consumedAt;
        return Task.FromResult(true);
    }
}
