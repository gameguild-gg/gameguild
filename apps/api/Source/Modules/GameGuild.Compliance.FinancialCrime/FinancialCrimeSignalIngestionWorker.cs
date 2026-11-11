using System.Text.Json;
using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.FinancialCrime;

/// <summary>
/// One pass of the module's own ingestion outbox (issue #1291 FC1, option C). The worker is
/// the only place queued signals reach the control plane: economy operations below the review
/// threshold synthesize an Approved screening (keeping the financial-crime evidence the risk
/// gate reads current instead of permanently Unavailable), operations at or above the review
/// threshold screen NeedsReview, operations at or above the alert threshold are recorded as
/// transaction signals — which open an AML case and hold the subject — and published KYC
/// decisions enrich the same profile: adverse decisions record an identity-risk signal,
/// everything else synthesizes a screening at the decision's tier.
/// </summary>
public interface IFinancialCrimeSignalIngestionWorker
{
    /// <summary>Processes one pass of due outbox rows. Returns the number of rows processed.</summary>
    Task<int> ProcessDueAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Internal screening providers synthesized by this module. The rescreening sweep only
/// re-synthesizes screenings from these providers — external provider screenings keep their
/// own schedule until a real provider ingress exists.
/// </summary>
public static class FinancialCrimeInternalScreeningProviders
{
    public const string EconomyOperation = "economy-protected-operation";
    public const string KycIdentityProfile = "kyc-identity-profile";

    public static bool IsInternal(string provider) =>
        provider == EconomyOperation || provider == KycIdentityProfile;
}

public sealed class FinancialCrimeSignalIngestionWorker(
    IFinancialCrimeSignalOutboxStore outbox,
    IFinancialCrimeControlPlane controlPlane,
    IApplicationDbContext context,
    IOptions<FinancialCrimeEconomyHookOptions> hookOptions,
    IOptions<FinancialCrimeIngestionWorkerOptions> workerOptions,
    TimeProvider timeProvider,
    ILogger<FinancialCrimeSignalIngestionWorker>? logger = null) : IFinancialCrimeSignalIngestionWorker
{
    private readonly FinancialCrimeEconomyHookOptions _hook = (hookOptions ?? throw new ArgumentNullException(nameof(hookOptions))).Value;
    private readonly FinancialCrimeIngestionWorkerOptions _worker =
        (workerOptions ?? throw new ArgumentNullException(nameof(workerOptions))).Value;

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var workerId = $"{Environment.MachineName}:{Environment.ProcessId}";
        var rows = await outbox.ClaimDueAsync(
            _worker.BatchSize, workerId, timeProvider.GetUtcNow(), _worker.ClaimLease, cancellationToken)
            .ConfigureAwait(false);
        var processed = 0;
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessOneAsync(row, cancellationToken).ConfigureAwait(false);
            processed++;
        }

        return processed;
    }

    // Internal for the coverage-completion tests: fault injection per claimed row.
    internal async Task ProcessOneAsync(FinancialCrimeSignalOutboxEntry row, CancellationToken cancellationToken)
    {
        try
        {
            switch (row.Kind)
            {
                case FinancialCrimeSignalKind.EconomyOperation:
                    await ProcessEconomyOperationAsync(
                        JsonSerializer.Deserialize<FinancialCrimeEconomyOperationSignal>(
                            row.PayloadJson, FinancialCrimeSignalJson.Options) ??
                        throw new JsonException("Economy operation signal payload was null."),
                        cancellationToken).ConfigureAwait(false);
                    break;
                case FinancialCrimeSignalKind.KycDecision:
                    await ProcessKycDecisionAsync(
                        JsonSerializer.Deserialize<FinancialCrimeKycDecisionSignal>(
                            row.PayloadJson, FinancialCrimeSignalJson.Options) ??
                        throw new JsonException("KYC decision signal payload was null."),
                        cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown signal kind {row.Kind}.");
            }

            await outbox.CompleteAsync(row.Id, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception,
                "Financial Crime signal {SignalId} ({SignalKind}) could not be ingested; attempt {Attempt}.",
                row.Id, row.Kind, row.Attempts);
            await outbox.FailAsync(
                row.Id, exception.Message, timeProvider.GetUtcNow(),
                _worker.MaxAttempts, _worker.RetryDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessEconomyOperationAsync(
        FinancialCrimeEconomyOperationSignal signal,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (signal.AmountUnits >= _hook.AlertUnitsThreshold)
        {
            await controlPlane.RecordSignalAsync(new FinancialCrimeTransactionSignal(
                Guid.NewGuid(),
                signal.TenantId,
                signal.SubjectHash,
                signal.OperationFingerprint,
                "economy-operation-alert",
                (int)Math.Clamp(signal.AmountUnits, 0, 1_000_000),
                FinancialCrimeSignalHash.Sha256($"economy-signal|{signal.CanonicalPayload}"),
                $"economy:{signal.IdempotencyKey}",
                signal.ObservedAt,
                MaxOf(signal.ObservedAt, now) + _hook.AlertHoldLifetime), cancellationToken).ConfigureAwait(false);
            return;
        }

        var outcome = signal.AmountUnits >= _hook.ReviewUnitsThreshold
            ? FinancialCrimeOutcome.NeedsReview
            : FinancialCrimeOutcome.Approved;
        await IngestInternalScreeningAsync(
            FinancialCrimeInternalScreeningProviders.EconomyOperation,
            $"economy:{signal.IdempotencyKey}",
            signal.TenantId,
            signal.SubjectHash,
            outcome,
            signal.ObservedAt,
            now,
            $"internal://economy-operation/{signal.IdempotencyKey}",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessKycDecisionAsync(
        FinancialCrimeKycDecisionSignal signal,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (IsAdverse(signal))
        {
            await controlPlane.RecordSignalAsync(new FinancialCrimeTransactionSignal(
                Guid.NewGuid(),
                signal.TenantId,
                signal.SubjectHash,
                $"kyc:{signal.ProviderEventId}",
                "kyc-identity-risk",
                RiskTierScore(signal),
                FinancialCrimeSignalHash.Sha256($"kyc-signal|{signal.CanonicalPayload}"),
                $"kyc:{signal.ProviderEventId}",
                signal.ObservedAt,
                MaxOf(signal.ObservedAt, now) + _hook.AlertHoldLifetime), cancellationToken).ConfigureAwait(false);
            return;
        }

        var outcome = string.Equals(signal.DecisionState, "Approved", StringComparison.OrdinalIgnoreCase)
            ? FinancialCrimeOutcome.Approved
            : FinancialCrimeOutcome.NeedsReview;
        await IngestInternalScreeningAsync(
            FinancialCrimeInternalScreeningProviders.KycIdentityProfile,
            $"kyc:{signal.ProviderEventId}",
            signal.TenantId,
            signal.SubjectHash,
            outcome,
            signal.ObservedAt,
            now,
            $"internal://kyc-decision/{signal.ProviderEventId}",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-ingests a screening for a subject at the next contiguous version. Going through
    /// <see cref="IFinancialCrimeControlPlane.IngestScreeningAsync"/> keeps every deployment
    /// invariant: replay conflict detection, contiguous versions, transactional evidence
    /// publication and case opening on non-Approved outcomes.
    /// </summary>
    private async ValueTask IngestInternalScreeningAsync(
        string provider,
        string providerEventId,
        Guid tenantId,
        string subjectHash,
        FinancialCrimeOutcome outcome,
        DateTimeOffset issuedAt,
        DateTimeOffset now,
        string rawObjectReference,
        CancellationToken cancellationToken)
    {
        var db = context as DbContext ??
            throw new InvalidOperationException(
                "Financial Crime ingestion requires the relational application DbContext.");
        var latestVersion = await db.Set<FinancialCrimeScreeningRow>().AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.SubjectHash == subjectHash)
            .Select(row => (long?)row.Version).MaxAsync(cancellationToken).ConfigureAwait(false) ?? 0;
        var canonical = $"{provider}|{providerEventId}|{tenantId:N}|{subjectHash}|{latestVersion + 1}|{issuedAt.UtcTicks}";
        await controlPlane.IngestScreeningAsync(new FinancialCrimeScreening(
            Guid.NewGuid(),
            provider,
            _hook.Environment,
            providerEventId,
            tenantId,
            subjectHash,
            latestVersion + 1,
            outcome,
            SanctionsMatch: false,
            PepMatch: false,
            AdverseMediaMatch: false,
            _hook.PolicyVersion,
            FinancialCrimeSignalHash.Sha256(canonical),
            FinancialCrimeSignalHash.Sha256($"evidence|{canonical}"),
            rawObjectReference,
            SignatureVerified: true,
            issuedAt,
            issuedAt + _hook.ScreeningLifetime,
            issuedAt + _hook.ScreeningInterval,
            MaxOf(issuedAt, now)), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>RED answers are adverse (case + hold); GREEN and in-review states screen at their tier.</summary>
    private static bool IsAdverse(FinancialCrimeKycDecisionSignal signal) =>
        string.Equals(signal.DecisionState, "Rejected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(signal.ReviewAnswer, "RED", StringComparison.OrdinalIgnoreCase);

    private static int RiskTierScore(FinancialCrimeKycDecisionSignal signal) =>
        string.Equals(signal.DecisionState, "Rejected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(signal.ReviewAnswer, "RED", StringComparison.OrdinalIgnoreCase)
            ? 900_000
            : 400_000;

    private static DateTimeOffset MaxOf(DateTimeOffset left, DateTimeOffset right) => left > right ? left : right;
}
