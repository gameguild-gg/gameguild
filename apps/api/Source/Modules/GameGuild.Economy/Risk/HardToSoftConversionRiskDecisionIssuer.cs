using System.Text.Json;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Economy.Risk;

public sealed class SelfServiceHardToSoftRiskDecisionOptions
{
    public const string SectionName = "Modules:Economy.Risk:HardToSoft";

    public long MaxHardCoinUnitsPerDay { get; set; }

    public int DecisionLifetimeSeconds { get; set; } = 300;
}

public sealed record HardToSoftConversionRiskDecisionRequest(
    Guid ActorId,
    Guid TenantId,
    WalletId WalletId,
    Guid ReservationOperationId,
    IdempotencyKey IdempotencyKey,
    long FeeHardCoinUnits,
    long TotalHardCoinUnits,
    IReadOnlyList<ExternalRiskEvidence> ExternalEvidence,
    DateTimeOffset RequestedAt);

public sealed record HardToSoftConversionRiskDecision(
    Guid Id,
    IReadOnlyList<Guid> SourceRoots);

public interface IHardToSoftConversionRiskDecisionIssuer
{
    Task<HardToSoftConversionRiskDecision> IssueAsync(
        HardToSoftConversionRiskDecisionRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Issues a short-lived conversion decision by calling the restricted database writer.
/// The writer selects and reserves exact FIFO fragments, persists the risk counter
/// reservation, and records the independent evidence as one atomic operation.
/// </summary>
public sealed class PostgreSqlHardToSoftConversionRiskDecisionIssuer(
    IApplicationDbContext context,
    IOptions<SelfServiceHardToSoftRiskDecisionOptions> options) : IHardToSoftConversionRiskDecisionIssuer
{
    public async Task<HardToSoftConversionRiskDecision> IssueAsync(
        HardToSoftConversionRiskDecisionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ExternalEvidence);
        if (request.ActorId == Guid.Empty || request.TenantId == Guid.Empty || request.ReservationOperationId == Guid.Empty)
            throw new ArgumentException("The actor, tenant, and reservation operation are required.", nameof(request));
        ArgumentOutOfRangeException.ThrowIfNegative(request.FeeHardCoinUnits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.TotalHardCoinUnits);

        var configuration = options.Value;
        if (configuration.MaxHardCoinUnitsPerDay <= 0)
            throw new EconomySelfServiceCommandRejectedException(
                "HardCoin conversion requires a positive server-side daily risk limit before rollout.");
        if (configuration.DecisionLifetimeSeconds is < 30 or > 900)
            throw new EconomySelfServiceCommandRejectedException(
                "HardCoin conversion decision lifetime must be between 30 and 900 seconds.");
        if (request.TotalHardCoinUnits > configuration.MaxHardCoinUnitsPerDay)
            throw new EconomySelfServiceCommandRejectedException(
                "The requested conversion exceeds the configured daily HardCoin risk limit.");

        var evidence = ExternalRiskEvidenceValidator.RequireFreshAllow(request.ExternalEvidence, request.RequestedAt);
        var evidencePayload = JsonSerializer.Serialize(evidence.Select(item => new
        {
            source = (int)item.Source,
            version = item.Version,
            issuedAt = item.IssuedAt,
            expiresAt = item.ExpiresAt,
            outcome = (int)item.Outcome,
            evidenceHash = item.EvidenceHash,
            isAuditable = item.IsAuditable
        }));

        var receipt = await context.Set<HardToSoftConversionRiskDecisionReceiptRow>()
            .FromSqlInterpolated($"""
                SELECT *
                FROM economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                    {request.ActorId},
                    {request.TenantId},
                    {request.WalletId.Value},
                    {request.ReservationOperationId},
                    {request.IdempotencyKey.Value},
                    {request.FeeHardCoinUnits},
                    {request.TotalHardCoinUnits},
                    {configuration.MaxHardCoinUnitsPerDay},
                    {evidencePayload},
                    {request.RequestedAt},
                    {request.RequestedAt.AddSeconds(configuration.DecisionLifetimeSeconds)})
                """)
            .AsNoTracking()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        var roots = PostgreSqlHardToSoftConversionWorkflow.ParseRootIds(receipt.SourceRoots);
        return new HardToSoftConversionRiskDecision(receipt.RiskDecisionId, roots);
    }
}
