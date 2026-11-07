using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

public sealed class PostgreSqlStepUpChallengeStore(IApplicationDbContext context) : IStepUpChallengeStore
{
    private DbSet<StepUpChallenge> Challenges => context.Set<StepUpChallenge>();

    public async Task AddAsync(StepUpChallenge challenge, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        await Challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<StepUpChallenge?> FindActiveAsync(
        Guid challengeId,
        StepUpSubject subject,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Challenges.AsNoTracking().SingleOrDefaultAsync(challenge =>
            challenge.Id == challengeId &&
            challenge.TenantId == subject.TenantId &&
            challenge.ActorId == subject.ActorId &&
            challenge.SessionId == subject.SessionId &&
            challenge.ExpiresAt > now &&
            challenge.VerifiedAt == null &&
            challenge.ConsumedAt == null,
            cancellationToken);
    }

    public Task<bool> MarkVerifiedAsync(
        Guid challengeId,
        StepUpSubject subject,
        string receiptHash,
        MfaMethod method,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken)
    {
        // WebAuthn evidence must also match the exact nonce bound by options issuance.
        return method == MfaMethod.WebAuthn
            ? Task.FromResult(false)
            : MarkVerifiedCoreAsync(challengeId, subject, receiptHash, method, null, verifiedAt, cancellationToken);
    }

    public async Task<bool> BindWebAuthnChallengeAsync(
        Guid challengeId,
        StepUpSubject subject,
        string challengeHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        RequireDigest(challengeHash);
        var changed = await Challenges.Where(challenge =>
                challenge.Id == challengeId && challenge.TenantId == subject.TenantId &&
                challenge.ActorId == subject.ActorId && challenge.SessionId == subject.SessionId &&
                challenge.ExpiresAt > now && challenge.VerifiedAt == null && challenge.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(challenge => challenge.WebAuthnChallengeHash, challengeHash),
                cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    public Task<bool> MarkWebAuthnVerifiedAsync(
        Guid challengeId,
        StepUpSubject subject,
        string challengeHash,
        string receiptHash,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken)
    {
        RequireDigest(challengeHash);
        return MarkVerifiedCoreAsync(challengeId, subject, receiptHash, MfaMethod.WebAuthn, challengeHash,
            verifiedAt, cancellationToken);
    }

    private async Task<bool> MarkVerifiedCoreAsync(
        Guid challengeId,
        StepUpSubject subject,
        string receiptHash,
        MfaMethod method,
        string? webAuthnChallengeHash,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptHash);
        var changed = await Challenges
            .Where(challenge =>
                challenge.Id == challengeId &&
                challenge.TenantId == subject.TenantId &&
                challenge.ActorId == subject.ActorId &&
                challenge.SessionId == subject.SessionId &&
                challenge.ExpiresAt > verifiedAt &&
                challenge.VerifiedAt == null &&
                challenge.ConsumedAt == null &&
                (webAuthnChallengeHash == null || challenge.WebAuthnChallengeHash == webAuthnChallengeHash))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(challenge => challenge.VerifiedAt, verifiedAt)
                    .SetProperty(challenge => challenge.VerificationMethod, method)
                    .SetProperty(challenge => challenge.ReceiptHash, receiptHash),
                cancellationToken)
            .ConfigureAwait(false);
        return changed == 1;
    }

    public async Task<bool> ConsumeAsync(
        StepUpSubject subject,
        StepUpOperationBinding binding,
        string receiptHash,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptHash);
        var changed = await Challenges
            .Where(challenge =>
                challenge.TenantId == subject.TenantId &&
                challenge.ActorId == subject.ActorId &&
                challenge.SessionId == subject.SessionId &&
                challenge.OperationType == binding.OperationType &&
                challenge.TargetReference == binding.TargetReference &&
                challenge.PayloadHash == binding.PayloadHash &&
                challenge.ReceiptHash == receiptHash &&
                challenge.ExpiresAt > consumedAt &&
                challenge.VerifiedAt != null &&
                challenge.ConsumedAt == null &&
                (challenge.VerificationMethod != MfaMethod.WebAuthn || challenge.WebAuthnChallengeHash != null))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(challenge => challenge.ConsumedAt, consumedAt),
                cancellationToken)
            .ConfigureAwait(false);
        return changed == 1;
    }

    private static void RequireDigest(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length != 64 || value.Any(character => !char.IsAsciiHexDigitLower(character)))
        {
            throw new ArgumentException("A lowercase SHA-256 digest is required.", nameof(value));
        }
    }
}
