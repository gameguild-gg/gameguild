using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

public sealed record StepUpSubject(Guid TenantId, Guid ActorId, Guid SessionId);

public sealed record StepUpOperationBinding
{
    public StepUpOperationBinding(string operationType, string targetReference, string payloadHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadHash);
        if (operationType.Length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(operationType));
        }

        if (targetReference.Length > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(targetReference));
        }

        if (payloadHash.Length != 64 || payloadHash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Payload hash must be a SHA-256 hexadecimal value.", nameof(payloadHash));
        }

        OperationType = operationType.Trim();
        TargetReference = targetReference.Trim();
        PayloadHash = payloadHash.ToLowerInvariant();
    }

    public string OperationType { get; }
    public string TargetReference { get; }
    public string PayloadHash { get; }
}

public sealed record StepUpVerification
{
    public StepUpVerification(MfaMethod method, string evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidence);
        Method = method;
        Evidence = evidence;
    }

    public MfaMethod Method { get; }
    public string Evidence { get; }
}

public sealed record StepUpChallengeResponse(Guid ChallengeId, DateTimeOffset ExpiresAt);

public sealed record StepUpReceiptResponse(string Receipt, DateTimeOffset ExpiresAt);

public sealed class StepUpChallenge
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorId { get; set; }
    public Guid SessionId { get; set; }

    [MaxLength(128)]
    public string OperationType { get; set; } = string.Empty;

    [MaxLength(256)]
    public string TargetReference { get; set; } = string.Empty;

    [MaxLength(64)]
    public string PayloadHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public MfaMethod? VerificationMethod { get; set; }

    [MaxLength(64)]
    public string? ReceiptHash { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }
}

public interface IStepUpChallengeStore
{
    Task AddAsync(StepUpChallenge challenge, CancellationToken cancellationToken);

    Task<StepUpChallenge?> FindActiveAsync(
        Guid challengeId,
        StepUpSubject subject,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> MarkVerifiedAsync(
        Guid challengeId,
        StepUpSubject subject,
        string receiptHash,
        MfaMethod method,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken);

    Task<bool> ConsumeAsync(
        StepUpSubject subject,
        StepUpOperationBinding binding,
        string receiptHash,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken);
}

public interface IStepUpReceiptService
{
    Task<StepUpChallengeResponse> CreateChallengeAsync(
        StepUpOperationBinding binding,
        CancellationToken cancellationToken = default);

    Task<WebAuthnAuthenticationOptionsResult> BeginWebAuthnAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);

    Task<StepUpReceiptResponse> VerifyAsync(
        Guid challengeId,
        StepUpVerification verification,
        CancellationToken cancellationToken = default);

    Task ConsumeAsync(
        StepUpOperationBinding binding,
        string receipt,
        CancellationToken cancellationToken = default);
}

public sealed class StepUpContextUnavailableException(string message) : InvalidOperationException(message);
public sealed class StepUpChallengeUnavailableException(string message) : InvalidOperationException(message);
public sealed class StepUpVerificationFailedException(string message) : InvalidOperationException(message);
public sealed class StepUpReceiptInvalidException(string message) : InvalidOperationException(message);
