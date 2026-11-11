using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.FinancialCrime;

/// <summary>
/// Kinds of profile signals the ingestion outbox carries (issue #1291 option C).
/// </summary>
public enum FinancialCrimeSignalKind
{
    /// <summary>A completed value-moving Economy protected operation (primary feed).</summary>
    EconomyOperation = 1,

    /// <summary>A published SumSub KYC decision (identity-enrichment feed).</summary>
    KycDecision = 2
}

/// <summary>
/// The durable signal an Economy protected operation leaves for asynchronous AML screening.
/// Carries the fields the economy package documents as compliance-relevant: user, operation
/// type, amount, currency, counterparty (when applicable) and timestamp — keyed by the same
/// opaque <see cref="SubjectHash"/> the Economy risk gate reads.
/// </summary>
public sealed record FinancialCrimeEconomyOperationSignal(
    Guid TenantId,
    Guid ActorId,
    string SubjectHash,
    string OperationType,
    long AmountUnits,
    string Currency,
    Guid? CounterpartyWalletId,
    string ProviderReferenceHash,
    string OperationFingerprint,
    string IdempotencyKey,
    DateTimeOffset ObservedAt)
{
    public string CanonicalPayload =>
        $"economy-operation-v1|{TenantId:N}|{ActorId:N}|{SubjectHash}|{OperationType}|" +
        $"{AmountUnits.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{Currency}|" +
        $"{CounterpartyWalletId?.ToString("N") ?? "none"}|{ProviderReferenceHash}|{OperationFingerprint}|" +
        $"{IdempotencyKey}|{ObservedAt.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}

/// <summary>
/// The durable signal a published SumSub KYC decision leaves for AML profile enrichment:
/// the decision state (risk tier) and the document-review answer, keyed by the opaque
/// <see cref="SubjectHash"/> shared with the Economy evidence reader.
/// </summary>
public sealed record FinancialCrimeKycDecisionSignal(
    Guid TenantId,
    string SubjectHash,
    string ApplicantId,
    string DecisionState,
    string? ReviewAnswer,
    string ProviderEventId,
    string PayloadHash,
    DateTimeOffset ObservedAt)
{
    public string CanonicalPayload =>
        $"kyc-decision-v1|{TenantId:N}|{SubjectHash}|{ApplicantId}|{DecisionState}|" +
        $"{ReviewAnswer ?? "none"}|{ProviderEventId}|{PayloadHash}|" +
        $"{ObservedAt.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Producer side of the module's own ingestion outbox (issue #1291 FC1). Enqueueing is the
/// ONLY thing callers on the money path do: a single durable row, never a screening call —
/// the outbox worker performs the actual <see cref="IFinancialCrimeControlPlane"/> ingestion
/// asynchronously so the payment path is never blocked by AML work.
/// </summary>
public interface IFinancialCrimeSignalQueue
{
    ValueTask EnqueueEconomyOperationAsync(
        FinancialCrimeEconomyOperationSignal signal,
        CancellationToken cancellationToken = default);

    ValueTask EnqueueKycDecisionAsync(
        FinancialCrimeKycDecisionSignal signal,
        CancellationToken cancellationToken = default);
}

/// <summary>A claimed, processable outbox row handed to the ingestion worker.</summary>
public sealed record FinancialCrimeSignalOutboxEntry(
    Guid Id,
    FinancialCrimeSignalKind Kind,
    string PayloadJson,
    int Attempts);

/// <summary>
/// Consumer side of the ingestion outbox: claim/complete/fail semantics for the worker.
/// Claims are taken inside a serializable transaction so concurrent workers never share a row.
/// </summary>
public interface IFinancialCrimeSignalOutboxStore
{
    ValueTask<IReadOnlyList<FinancialCrimeSignalOutboxEntry>> ClaimDueAsync(
        int limit,
        string workerId,
        DateTimeOffset now,
        TimeSpan reclaimAge,
        CancellationToken cancellationToken = default);

    ValueTask CompleteAsync(Guid id, DateTimeOffset completedAt, CancellationToken cancellationToken = default);

    ValueTask FailAsync(
        Guid id,
        string error,
        DateTimeOffset failedAt,
        int maxAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for the Economy hook (issue #1291 option C). The hook synthesizes the
/// internal screening provider behind <see cref="IFinancialCrimeControlPlane.IngestScreeningAsync"/>:
/// amounts at or above <see cref="AlertUnitsThreshold"/> record a transaction signal (which opens
/// a case and holds the subject), amounts at or above <see cref="ReviewUnitsThreshold"/> screen
/// NeedsReview, and everything below screens Approved so the risk gate reads current,
/// non-Unavailable evidence instead of denying by absence.
/// </summary>
public sealed class FinancialCrimeEconomyHookOptions
{
    public const string SectionName = "Modules:Compliance.FinancialCrime:EconomyHook";

    /// <summary>Master switch for the economy signal hook; false disables enqueueing only.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Environment stamp on synthesized screening evidence.</summary>
    public string Environment { get; set; } = "production";

    /// <summary>Policy version stamped on synthesized screening evidence.</summary>
    public long PolicyVersion { get; set; } = 1;

    /// <summary>Amount (coin units) at or above which a screening is synthesized as NeedsReview.</summary>
    public long ReviewUnitsThreshold { get; set; } = 100_000;

    /// <summary>Amount (coin units) at or above which a transaction signal is recorded (case + hold).</summary>
    public long AlertUnitsThreshold { get; set; } = 1_000_000;

    /// <summary>Lifetime of the compliance hold an alert-level signal activates.</summary>
    public TimeSpan AlertHoldLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Lifetime of a synthesized screening (evidence freshness window).</summary>
    public TimeSpan ScreeningLifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Interval until the next scheduled screening of the same subject.</summary>
    public TimeSpan ScreeningInterval { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>Configuration of the module's own ingestion worker.</summary>
public sealed class FinancialCrimeIngestionWorkerOptions
{
    public const string SectionName = "Modules:Compliance.FinancialCrime:Ingestion";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);

    public int BatchSize { get; set; } = 25;

    public int MaxAttempts { get; set; } = 5;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>A Processing row older than this is reclaimable after a worker crash.</summary>
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>Configuration of the periodic rescreening sweep.</summary>
public sealed class FinancialCrimeRescreeningWorkerOptions
{
    public const string SectionName = "Modules:Compliance.FinancialCrime:Rescreening";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);

    public int BatchSize { get; set; } = 50;
}

internal sealed class FinancialCrimeSignalOutboxRow
{
    public Guid Id { get; set; }
    public FinancialCrimeSignalKind Kind { get; set; }
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public FinancialCrimeSignalOutboxStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? LastError { get; set; }
}

internal enum FinancialCrimeSignalOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    DeadLettered = 4
}

/// <summary>
/// PostgreSQL-backed implementation of the module's own outbox. Every mutation commits in its
/// own serializable transaction so the producer (money path) and the consumer (worker) never
/// share a DbContext or a transaction.
/// </summary>
public sealed class PostgreSqlFinancialCrimeSignalQueue(IApplicationDbContext context) :
    IFinancialCrimeSignalQueue, IFinancialCrimeSignalOutboxStore
{
    private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    private DbContext Db =>
        _context as DbContext ??
        throw new InvalidOperationException(
            "Financial Crime signal outbox requires the relational application DbContext.");

    public async ValueTask EnqueueEconomyOperationAsync(
        FinancialCrimeEconomyOperationSignal signal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);
        await EnqueueAsync(FinancialCrimeSignalKind.EconomyOperation, signal.TenantId, signal.SubjectHash,
            JsonSerializer.Serialize(signal, FinancialCrimeSignalJson.Options), signal.CanonicalPayload,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask EnqueueKycDecisionAsync(
        FinancialCrimeKycDecisionSignal signal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);
        await EnqueueAsync(FinancialCrimeSignalKind.KycDecision, signal.TenantId, signal.SubjectHash,
            JsonSerializer.Serialize(signal, FinancialCrimeSignalJson.Options), signal.CanonicalPayload,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<FinancialCrimeSignalOutboxEntry>> ClaimDueAsync(
        int limit,
        string workerId,
        DateTimeOffset now,
        TimeSpan reclaimAge,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        return await PostgreSqlTransactionExecutor.ExecuteAsync(
            _context, IsolationLevel.Serializable, async ct =>
        {
            var reclaimBefore = now - reclaimAge;
            var rows = await Db.Set<FinancialCrimeSignalOutboxRow>()
                .Where(row => (row.Status == FinancialCrimeSignalOutboxStatus.Pending &&
                               (row.NextAttemptAt == null || row.NextAttemptAt <= now)) ||
                              (row.Status == FinancialCrimeSignalOutboxStatus.Processing && row.ClaimedAt < reclaimBefore))
                .OrderBy(row => row.CreatedAt).ThenBy(row => row.Id)
                .Take(limit)
                .ToArrayAsync(ct).ConfigureAwait(false);
            foreach (var row in rows)
            {
                row.Status = FinancialCrimeSignalOutboxStatus.Processing;
                row.Attempts++;
                row.ClaimedAt = now;
                row.ClaimedBy = workerId;
                row.NextAttemptAt = null;
            }

            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return (IReadOnlyList<FinancialCrimeSignalOutboxEntry>)rows.Select(row =>
                new FinancialCrimeSignalOutboxEntry(row.Id, row.Kind, row.PayloadJson, row.Attempts)).ToArray();
        }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask CompleteAsync(
        Guid id,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Outbox row ID is required.", nameof(id));
        }

        await PostgreSqlTransactionExecutor.ExecuteAsync(
            _context, IsolationLevel.Serializable, async ct =>
        {
            var row = await Db.Set<FinancialCrimeSignalOutboxRow>()
                .SingleAsync(row => row.Id == id, ct).ConfigureAwait(false);
            row.Status = FinancialCrimeSignalOutboxStatus.Processed;
            row.CompletedAt = completedAt;
            row.LastError = null;
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return 0;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask FailAsync(
        Guid id,
        string error,
        DateTimeOffset failedAt,
        int maxAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Outbox row ID is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        await PostgreSqlTransactionExecutor.ExecuteAsync(
            _context, IsolationLevel.Serializable, async ct =>
        {
            var row = await Db.Set<FinancialCrimeSignalOutboxRow>()
                .SingleAsync(row => row.Id == id, ct).ConfigureAwait(false);
            row.LastError = error;
            if (row.Attempts >= maxAttempts)
            {
                row.Status = FinancialCrimeSignalOutboxStatus.DeadLettered;
                row.CompletedAt = failedAt;
            }
            else
            {
                row.Status = FinancialCrimeSignalOutboxStatus.Pending;
                row.ClaimedAt = null;
                row.ClaimedBy = null;
                row.NextAttemptAt = failedAt + retryDelay;
            }

            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return 0;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask EnqueueAsync(
        FinancialCrimeSignalKind kind,
        Guid tenantId,
        string subjectHash,
        string payloadJson,
        string canonicalPayload,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(subjectHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        var payloadHash = FinancialCrimeSignalHash.Sha256(canonicalPayload);
        await PostgreSqlTransactionExecutor.ExecuteAsync(
            _context, IsolationLevel.Serializable, async ct =>
        {
            var replay = await Db.Set<FinancialCrimeSignalOutboxRow>().AsNoTracking()
                .SingleOrDefaultAsync(row => row.PayloadHash == payloadHash, ct).ConfigureAwait(false);
            if (replay is not null)
            {
                return 0;
            }

            Db.Set<FinancialCrimeSignalOutboxRow>().Add(new FinancialCrimeSignalOutboxRow
            {
                Id = Guid.NewGuid(), Kind = kind, TenantId = tenantId, SubjectHash = subjectHash.Trim(),
                PayloadJson = payloadJson, PayloadHash = payloadHash,
                Status = FinancialCrimeSignalOutboxStatus.Pending, Attempts = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return 0;
        }, cancellationToken).ConfigureAwait(false);
    }
}

public static class FinancialCrimeSignalHash
{
    public static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public static class FinancialCrimeSignalJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };
}
