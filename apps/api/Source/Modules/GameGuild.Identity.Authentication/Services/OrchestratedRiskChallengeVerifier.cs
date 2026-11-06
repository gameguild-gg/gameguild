using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.DataProtection;

namespace GameGuild.Identity.Authentication;

/// <summary>Private flow state. Neither the code nor this protected value belongs in a client response.</summary>
public sealed record OrchestratedRiskChallengeState(string ProtectedDigest, DateTimeOffset ExpiresAt,
    int FailedAttempts, DateTimeOffset? VerifiedAt);

public sealed record OrchestratedRiskVerification(bool Accepted, OrchestratedRiskChallengeState State);

/// <summary>Verifies only an out-of-band code bound to this flow, subject and tenant.</summary>
public sealed class OrchestratedRiskChallengeVerifier(IDataProtectionProvider protection, IPublisher publisher)
{
    public static int MaximumFailedAttempts { get; } = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly IDataProtector _protector = protection.CreateProtector("Authentication.Orchestration.RiskCode.v1");

    public async Task<OrchestratedRiskChallengeState> CreateAsync(Guid flowId, User subject, Guid tenantId,
        DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(subject);
        if (flowId == Guid.Empty || subject.Id == Guid.Empty || tenantId == Guid.Empty || !subject.IsEmailVerified ||
            string.IsNullOrWhiteSpace(subject.Email) || now.Offset != TimeSpan.Zero || expiresAt.Offset != TimeSpan.Zero ||
            expiresAt <= now || expiresAt > now.Add(Lifetime))
        {
            throw new AuthenticationRequiredException("A verified email address is required for this sign-in challenge.");
        }
        var code = RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        var protectedDigest = _protector.Protect(Convert.ToBase64String(Digest(flowId, subject.Id, tenantId, code)));
        await publisher.Publish(new OrchestratedRiskCodeRequestedNotification(subject.Id, subject.Email, code,
            checked((int)Math.Ceiling((expiresAt - now).TotalSeconds)), expiresAt), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return new OrchestratedRiskChallengeState(protectedDigest, expiresAt, 0, null);
    }

    public OrchestratedRiskVerification Verify(OrchestratedRiskChallengeState state, Guid flowId, Guid subjectId,
        Guid tenantId, string code, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (flowId == Guid.Empty || subjectId == Guid.Empty || tenantId == Guid.Empty || now.Offset != TimeSpan.Zero ||
            state.VerifiedAt is not null || state.ExpiresAt.Offset != TimeSpan.Zero || now >= state.ExpiresAt ||
            state.FailedAttempts < 0 || state.FailedAttempts >= MaximumFailedAttempts || string.IsNullOrWhiteSpace(state.ProtectedDigest))
        {
            return new OrchestratedRiskVerification(false, state);
        }
        var wellFormed = code is { Length: 8 } && code.All(character => character is >= '0' and <= '9');
        var expected = new byte[SHA256.HashSizeInBytes];
        var readable = false;
        try
        {
            var decoded = Convert.FromBase64String(_protector.Unprotect(state.ProtectedDigest));
            if (decoded.Length == expected.Length) { decoded.CopyTo(expected, 0); readable = true; }
        }
        catch (CryptographicException) { readable = false; }
        catch (FormatException) { readable = false; }
        var actual = Digest(flowId, subjectId, tenantId, wellFormed ? code : "00000000");
        var digestMatches = CryptographicOperations.FixedTimeEquals(expected, actual);
        var accepted = readable && wellFormed && digestMatches;
        return new OrchestratedRiskVerification(accepted, accepted
            ? state with { ProtectedDigest = string.Empty, VerifiedAt = now }
            : state with { FailedAttempts = state.FailedAttempts + 1 });
    }

    private static byte[] Digest(Guid flowId, Guid subjectId, Guid tenantId, string code) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"{flowId:D}|{subjectId:D}|{tenantId:D}|{code}"));
}
