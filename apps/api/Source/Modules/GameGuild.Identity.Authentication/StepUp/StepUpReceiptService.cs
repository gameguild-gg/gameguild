using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        // Use the store's microsecond precision so the returned expiry matches its persisted value.
        var clock = timeProvider.GetUtcNow();
        var now = new DateTimeOffset(clock.UtcTicks - clock.UtcTicks % TimeSpan.TicksPerMicrosecond, TimeSpan.Zero);
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

        if (!result.Success || result.Options?.Challenge is not { Length: >= 16 and <= 1024 } nonce)
        {
            throw new StepUpVerificationFailedException(result.Error ?? "WebAuthn challenge could not be created.");
        }

        if (!await challengeStore.BindWebAuthnChallengeAsync(
                challengeId, subject, HashBytes(nonce), timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false))
        {
            throw new StepUpChallengeUnavailableException("Step-up challenge is expired or already verified.");
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
        if (verification.Method is not (MfaMethod.Totp or MfaMethod.BackupCode or MfaMethod.WebAuthn))
        {
            throw new StepUpVerificationFailedException("The requested MFA method cannot authorize step-up operations.");
        }

        // Reserve a durable slot before calling any factor, including when another
        // host is already verifying this challenge. A reissued nonce never resets it.
        if (!await challengeStore.TryReserveVerificationAttemptAsync(
                challengeId, subject, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false))
        {
            throw new StepUpChallengeUnavailableException("Step-up challenge is unavailable.");
        }

        var webAuthnChallengeHash = challenge.WebAuthnChallengeHash;

        var verifiedFactor = verification.Method switch
        {
            MfaMethod.Totp => await VerifyMfaAsync(
                subject.ActorId,
                verification.Evidence,
                MfaMethod.Totp,
                cancellationToken).ConfigureAwait(false),
            MfaMethod.BackupCode => await VerifyMfaAsync(
                subject.ActorId,
                verification.Evidence,
                MfaMethod.BackupCode,
                cancellationToken).ConfigureAwait(false),
            MfaMethod.WebAuthn => await VerifyWebAuthnAsync(
                subject.ActorId,
                webAuthnChallengeHash,
                verification.Evidence,
                cancellationToken).ConfigureAwait(false),
            _ => throw new StepUpVerificationFailedException("The requested MFA method cannot authorize step-up operations.")
        };

        if (verifiedFactor is null)
        {
            throw new StepUpVerificationFailedException("MFA evidence is invalid.");
        }

        var receipt = CreateOpaqueReceipt();
        var now = timeProvider.GetUtcNow();
        // The successful verifier supplies the persistence proof. The request's
        // method discriminator cannot independently select a weaker write path.
        var persisted = verifiedFactor.WebAuthnChallengeHash is { } verifiedChallengeHash
            ? await challengeStore.MarkWebAuthnVerifiedAsync(
                challengeId, subject, verifiedChallengeHash, Hash(receipt), now, cancellationToken).ConfigureAwait(false)
            : await challengeStore.MarkVerifiedAsync(
                challengeId, subject, Hash(receipt), verifiedFactor.Method, now, cancellationToken).ConfigureAwait(false);
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

    private async Task<VerifiedStepUpFactor?> VerifyMfaAsync(
        Guid actorId,
        string evidence,
        MfaMethod method,
        CancellationToken cancellationToken)
    {
        var result = await mfaService.VerifyMfaForStepUpAsync(
            actorId,
            evidence,
            method,
            cancellationToken).ConfigureAwait(false);
        return result.Success ? new VerifiedStepUpFactor(method, null) : null;
    }

    private async Task<VerifiedStepUpFactor?> VerifyWebAuthnAsync(
        Guid actorId,
        string? challengeHash,
        string assertion,
        CancellationToken cancellationToken)
    {
        if (challengeHash is null || !MatchesWebAuthnChallenge(challengeHash, assertion))
        {
            return null;
        }

        var result = await webAuthnService.CompleteAuthenticationForStepUpAsync(
            assertion,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.Success && result.UserId == actorId
            ? new VerifiedStepUpFactor(MfaMethod.WebAuthn, challengeHash)
            : null;
    }

    private sealed record VerifiedStepUpFactor(MfaMethod Method, string? WebAuthnChallengeHash);

    private static bool MatchesWebAuthnChallenge(string? challengeHash, string assertion)
    {
        if (challengeHash is not { Length: 64 } || assertion.Length > 65_536)
        {
            return false;
        }

        try
        {
            using var envelope = JsonDocument.Parse(assertion);
            if (envelope.RootElement.ValueKind != JsonValueKind.Object ||
                !envelope.RootElement.TryGetProperty("response", out var response) ||
                response.ValueKind != JsonValueKind.Object ||
                !response.TryGetProperty("clientDataJSON", out var clientData) ||
                clientData.ValueKind != JsonValueKind.String ||
                clientData.GetString() is not { Length: > 0 and <= 16_384 } encodedClientData)
            {
                return false;
            }

            using var client = JsonDocument.Parse(DecodeBase64Url(encodedClientData));
            if (client.RootElement.ValueKind != JsonValueKind.Object ||
                !client.RootElement.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String || type.GetString() != "webauthn.get" ||
                !client.RootElement.TryGetProperty("challenge", out var challenge) ||
                challenge.ValueKind != JsonValueKind.String ||
                challenge.GetString() is not { Length: > 0 and <= 1366 } encodedNonce)
            {
                return false;
            }

            var nonce = DecodeBase64Url(encodedNonce);
            return nonce.Length is >= 16 and <= 1024 && CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(challengeHash), SHA256.HashData(nonce));
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        if (value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
        {
            throw new FormatException("Invalid base64url encoding.");
        }
        var normalized = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(normalized.PadRight((normalized.Length + 3) / 4 * 4, '='));
    }

    private static string HashBytes(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

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
