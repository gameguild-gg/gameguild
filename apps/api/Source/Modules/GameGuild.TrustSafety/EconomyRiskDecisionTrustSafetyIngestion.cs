using System.Security.Cryptography;
using System.Text;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.Finance.Economy.Risk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.TrustSafety;

/// <summary>
///     First producer for the Trust/Safety control plane (issue #1319 T1): decorates the
///     Economy protected-operation risk-decision issuer so every SUBJECT-AFFECTING Economy risk
///     decision (allow / review / deny) is forwarded to <see cref="ITrustSafetyControlPlane.IngestAsync"/>
///     as a signed <see cref="TrustSafetyEvent"/> of kind <see cref="TrustSafetyEventKind.AccountRestriction"/>.
///     The evidence rail then feeds the Trust/Safety subject state that the Economy capability
///     evaluator reads back (closing the previously write-less control loop).
/// </summary>
/// <remarks>
///     <para>
///     <b>Never fails the Economy operation:</b> ingestion problems (signing unavailable — the
///     default when no KMS key is configured, out-of-order versions under concurrency, store
///     errors) are logged and swallowed; the decorated risk decision is returned unchanged.
///     Out-of-order rows stay in the inbox for the future re-driver (T8).
///     </para>
///     <para>
///     <b>Volume control:</b> only OUTCOME CHANGES are forwarded (a subject whose mapped outcome
///     already matches the control plane's state produces no event), so steady-state allowed
///     traffic does not flood the evidence rail.
///     </para>
/// </remarks>
public sealed class EconomyRiskDecisionTrustSafetyIngestion(
    IEconomyProtectedOperationRiskDecisionIssuer inner,
    ITrustSafetyControlPlane trustSafety,
    ICapabilityPolicySigner signer,
    IApplicationDbContext context,
    ILogger<EconomyRiskDecisionTrustSafetyIngestion> logger) : IEconomyProtectedOperationRiskDecisionIssuer
{
    /// <summary>Policy version stamped on Economy-sourced Trust/Safety events (metadata only).</summary>
    public const long EconomyRiskPolicyVersion = 1;

    private const string EventIdPrefix = "economy-risk:";

    private readonly IEconomyProtectedOperationRiskDecisionIssuer _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly ITrustSafetyControlPlane _trustSafety = trustSafety ?? throw new ArgumentNullException(nameof(trustSafety));
    private readonly ICapabilityPolicySigner _signer = signer ?? throw new ArgumentNullException(nameof(signer));
    private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<EconomyRiskDecisionTrustSafetyIngestion> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<EconomyProtectedRiskDecision> IssueAsync(
        EconomyProtectedRiskDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var decision = await _inner.IssueAsync(request, cancellationToken).ConfigureAwait(false);
        await TryIngestAsync(request, decision, cancellationToken).ConfigureAwait(false);
        return decision;
    }

    private async Task TryIngestAsync(
        EconomyProtectedRiskDecisionRequest request,
        EconomyProtectedRiskDecision decision,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = MapOutcome(decision.State);
            if (outcome is null)
            {
                // Not a subject verdict (compliance-unavailable / stale / invalid-policy / reserve
                // states describe the pipeline, not the subject).
                return;
            }

            var subjectHash = request.SubjectReference?.Trim();
            if (string.IsNullOrWhiteSpace(subjectHash))
            {
                return;
            }

            var dbContext = _context as DbContext
                ?? throw new InvalidOperationException("Trust/Safety ingestion requires the relational application DbContext.");
            var state = await dbContext.Set<TrustSafetySubjectStateRow>().AsNoTracking()
                .SingleOrDefaultAsync(row => row.TenantId == request.TenantId && row.SubjectHash == subjectHash, cancellationToken)
                .ConfigureAwait(false);
            if (state is not null && state.Outcome == outcome.Value)
            {
                // Steady state: the control plane already records this outcome for the subject.
                return;
            }

            var issuedAt = DateTimeOffset.UtcNow;
            var evidenceSeed = $"{request.OperationFingerprint}|{request.TenantId:N}|{subjectHash}|{(int)decision.State}|{decision.Id:N}";
            var payloadHash = Hash(evidenceSeed);
            var evidence = new TrustSafetyEvent(
                Id: Guid.NewGuid(),
                EventId: $"{EventIdPrefix}{decision.Id:N}",
                TenantId: request.TenantId,
                SubjectHash: subjectHash,
                Kind: TrustSafetyEventKind.AccountRestriction,
                Version: (state?.Version ?? 0) + 1,
                Outcome: outcome.Value,
                PolicyVersion: EconomyRiskPolicyVersion,
                PayloadHash: payloadHash,
                EvidenceHash: Hash(payloadHash),
                RawObjectReference: $"economy:risk-decisions:{decision.Id:N}",
                KeyId: string.Empty,
                Signature: string.Empty,
                IssuedAt: issuedAt,
                ExpiresAt: issuedAt.AddYears(1),
                ReceivedAt: issuedAt);

            var canonical = PostgreSqlTrustSafetyControlPlane.Canonicalize(evidence);
            var signature = await _signer.SignAsync(canonical, cancellationToken).ConfigureAwait(false);
            evidence = evidence with { KeyId = signature.KeyId, Signature = signature.Signature };

            var ingestion = await _trustSafety.IngestAsync(evidence, cancellationToken).ConfigureAwait(false);
            if (ingestion.Status is ComplianceEvidenceIngestionStatus.Published or ComplianceEvidenceIngestionStatus.Duplicate)
            {
                _logger.LogInformation(
                    "Ingested Economy risk decision {DecisionId} (state {State}) for tenant {TenantId} into the Trust/Safety control plane as version {Version}",
                    decision.Id, decision.State, request.TenantId, evidence.Version);
            }
            else
            {
                _logger.LogWarning(
                    "Economy risk decision {DecisionId} for tenant {TenantId} was not ingested into the Trust/Safety control plane: {Status}",
                    decision.Id, request.TenantId, ingestion.Status);
            }
        }
        catch (Exception exception)
        {
            // The evidence rail must never fail the protected Economy operation. Signing is
            // unavailable by default (no KMS key configured) — Debug for that case, Warn otherwise.
            if (exception is EconomyCryptographyUnavailableException)
            {
                _logger.LogDebug(
                    "Trust/Safety ingestion skipped for Economy risk decision {DecisionId}: asymmetric signing is not configured",
                    decision.Id);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Trust/Safety ingestion failed for Economy risk decision {DecisionId}; the decision itself was unaffected",
                    decision.Id);
            }
        }
    }

    private static TrustSafetyOutcome? MapOutcome(EconomyProtectedOperationState state) => state switch
    {
        EconomyProtectedOperationState.Ready => TrustSafetyOutcome.Approved,
        EconomyProtectedOperationState.ReviewRequired => TrustSafetyOutcome.NeedsReview,
        EconomyProtectedOperationState.Hold => TrustSafetyOutcome.NeedsReview,
        EconomyProtectedOperationState.Challenge => TrustSafetyOutcome.NeedsReview,
        EconomyProtectedOperationState.Denied => TrustSafetyOutcome.Rejected,
        _ => null
    };

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

/// <summary>
///     Composition for <see cref="EconomyRiskDecisionTrustSafetyIngestion"/>: replaces the last
/// registered <see cref="IEconomyProtectedOperationRiskDecisionIssuer"/> with the decorated
/// adapter. Must be called AFTER the Economy core module has registered the issuer.
/// </summary>
public static class EconomyRiskTrustSafetyIngestionComposition
{
    /// <summary>
    ///     Decorates the registered <see cref="IEconomyProtectedOperationRiskDecisionIssuer"/>
    ///     with <see cref="EconomyRiskDecisionTrustSafetyIngestion"/> (issue #1319 T1).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no issuer is registered — call this after the Economy core composition.
    /// </exception>
    public static IServiceCollection AddEconomyRiskTrustSafetyIngestion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var descriptor = services.Last(descriptor => descriptor.ServiceType == typeof(IEconomyProtectedOperationRiskDecisionIssuer));
        var lifetime = descriptor.Lifetime;
        services.Remove(descriptor);
        services.Add(ServiceDescriptor.Describe(
            typeof(IEconomyProtectedOperationRiskDecisionIssuer),
            provider => new EconomyRiskDecisionTrustSafetyIngestion(
                CreateInner(provider, descriptor),
                provider.GetRequiredService<ITrustSafetyControlPlane>(),
                provider.GetRequiredService<ICapabilityPolicySigner>(),
                provider.GetRequiredService<IApplicationDbContext>(),
                provider.GetRequiredService<ILogger<EconomyRiskDecisionTrustSafetyIngestion>>()),
            lifetime));
        return services;
    }

    private static IEconomyProtectedOperationRiskDecisionIssuer CreateInner(
        IServiceProvider provider,
        ServiceDescriptor descriptor) =>
        descriptor.ImplementationType is not null
            ? (IEconomyProtectedOperationRiskDecisionIssuer)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType)
            : descriptor.ImplementationFactory is not null
                ? (IEconomyProtectedOperationRiskDecisionIssuer)descriptor.ImplementationFactory(provider)
                : throw new InvalidOperationException(
                    "The Economy risk-decision issuer registration is neither a type nor a factory and cannot be decorated.");
}
