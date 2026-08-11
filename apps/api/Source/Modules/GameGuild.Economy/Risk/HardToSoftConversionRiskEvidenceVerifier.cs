using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Economy.Risk;

/// <summary>
/// Requires independent, current fraud-control evidence before a self-service
/// HardCoin to SoftCoin conversion can consume value.
/// </summary>
public interface IHardToSoftConversionRiskEvidenceVerifier
{
    Task VerifyAsync(Guid actorId, Guid tenantId, CancellationToken cancellationToken);
}

public sealed class HardToSoftConversionRiskEvidenceVerifier(
    IFinancialCrimeRiskInputSource financialCrime,
    ITrustSafetyRiskInputSource trustSafety) : IHardToSoftConversionRiskEvidenceVerifier
{
    public async Task VerifyAsync(Guid actorId, Guid tenantId, CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("An actor is required.", nameof(actorId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A tenant is required.", nameof(tenantId));

        cancellationToken.ThrowIfCancellationRequested();
        var observedAt = DateTimeOffset.UtcNow;
        var subjectReference = CreateOpaqueSubjectReference(actorId, tenantId);
        var financialCrimeEvidence = await financialCrime
            .ReadAsync(subjectReference, observedAt, cancellationToken)
            .ConfigureAwait(false);
        var trustSafetyEvidence = await trustSafety
            .ReadAsync(subjectReference, observedAt, cancellationToken)
            .ConfigureAwait(false);

        ExternalRiskEvidenceValidator.RequireFreshAllow(
            [financialCrimeEvidence.ToEvidence(), trustSafetyEvidence.ToEvidence()],
            observedAt);
    }

    internal static string CreateOpaqueSubjectReference(Guid actorId, Guid tenantId)
    {
        if (actorId == Guid.Empty || tenantId == Guid.Empty)
            throw new ArgumentException("Both actor and tenant are required for an opaque subject reference.");

        var canonical = $"economy:hard-to-soft:{actorId:N}:{tenantId:N}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
