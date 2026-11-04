using System.Text.Json;
using GameGuild;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>Lifecycle of a durable background fraud re-scoring job (issue #1218 G3).</summary>
public enum FraudBackgroundScoringJobStatus
{
    /// <summary>Row persisted by the payment path, waiting for the worker's due sweep.</summary>
    Pending,

    /// <summary>Claimed by a worker; reclaimed through the claim lease after a crash.</summary>
    Processing,

    /// <summary>Terminal success — the deferred providers were re-evaluated.</summary>
    Completed,

    /// <summary>
    ///     Terminal poison state after the attempt ceiling: never re-picked by the sweep. The
    ///     synchronous decision recorded on the payment stands.
    /// </summary>
    DeadLettered,
}

/// <summary>
///     One durable background re-scoring job (issue #409: asynchronous scoring for background
///     checks; issue #1218 G3: durability). Replaces the original in-memory record + channel: the
///     row carries the exact signal context of the synchronous pass, so the background re-score
///     evaluates the same attempt — including the providers the synchronous budget deferred —
///     without reconstructing (or trusting) client input, and survives a process restart.
/// </summary>
public sealed class FraudBackgroundScoringJob
{
    private FraudBackgroundScoringJob() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PaymentId { get; private set; }

    /// <summary>Serialized <see cref="FraudSignalContext"/> snapshot of the synchronous assessment.</summary>
    public string ContextJson { get; private set; } = string.Empty;

    /// <summary>JSON array of provider names deferred by the synchronous budget.</summary>
    public string DeferredProvidersJson { get; private set; } = "[]";

    public FraudBackgroundScoringJobStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? ClaimedAt { get; private set; }
    public string? ClaimedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? LastError { get; private set; }

    public static FraudBackgroundScoringJob Create(
        FraudSignalContext context,
        IReadOnlyList<string> deferredProviders)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(deferredProviders);

        return new FraudBackgroundScoringJob
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            PaymentId = context.PaymentId,
            ContextJson = JsonSerializer.Serialize(context, FraudBackgroundScoringJson.Options),
            DeferredProvidersJson = JsonSerializer.Serialize(deferredProviders, FraudBackgroundScoringJson.Options),
            Status = FraudBackgroundScoringJobStatus.Pending,
            CreatedAt = SystemClock.UtcNow,
        };
    }

    /// <summary>Rehydrates the frozen signal context; false means the row is corrupt (poison).</summary>
    public bool TryRehydrateContext(out FraudSignalContext? context)
    {
        try
        {
            context = JsonSerializer.Deserialize<FraudSignalContext>(ContextJson, FraudBackgroundScoringJson.Options);
            return context is not null;
        }
        catch (JsonException)
        {
            context = null;
            return false;
        }
    }

    /// <summary>Provider names deferred by the synchronous budget; empty when the payload is corrupt.</summary>
    public IReadOnlyList<string> ReadDeferredProviders()
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(DeferredProvidersJson, FraudBackgroundScoringJson.Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    ///     Claims the job for a worker (attempt counted). Throws when the row is terminal; a
    ///     concurrent claim loses the <c>DbUpdateConcurrencyException</c> race and the row is skipped.
    /// </summary>
    public void Claim(string workerId, DateTime claimedAt)
    {
        if (Status is FraudBackgroundScoringJobStatus.Completed or FraudBackgroundScoringJobStatus.DeadLettered)
        {
            throw new InvalidOperationException("A terminal fraud re-scoring job cannot be claimed.");
        }

        if (string.IsNullOrWhiteSpace(workerId))
        {
            throw new ArgumentException("Worker ID is required.", nameof(workerId));
        }

        Status = FraudBackgroundScoringJobStatus.Processing;
        AttemptCount++;
        ClaimedAt = claimedAt.ToUniversalTime();
        ClaimedBy = workerId;
        NextAttemptAt = null;
        LastError = null;
    }

    public void Complete(DateTime completedAt)
    {
        if (Status == FraudBackgroundScoringJobStatus.Completed)
        {
            return;
        }

        Status = FraudBackgroundScoringJobStatus.Completed;
        CompletedAt = completedAt.ToUniversalTime();
        NextAttemptAt = null;
        ClaimedAt = null;
        ClaimedBy = null;
        LastError = null;
    }

    public void Fail(string error, DateTime nextAttemptAt)
    {
        if (Status is FraudBackgroundScoringJobStatus.Completed or FraudBackgroundScoringJobStatus.DeadLettered)
        {
            return;
        }

        Status = FraudBackgroundScoringJobStatus.Pending;
        LastError = Truncate(error, 4000);
        NextAttemptAt = nextAttemptAt.ToUniversalTime();
        ClaimedAt = null;
        ClaimedBy = null;
    }

    /// <summary>Terminal poison transition after the attempt ceiling (issue #1218 G3).</summary>
    public void DeadLetter(string error, DateTime deadLetteredAt)
    {
        if (Status is FraudBackgroundScoringJobStatus.Completed or FraudBackgroundScoringJobStatus.DeadLettered)
        {
            return;
        }

        Status = FraudBackgroundScoringJobStatus.DeadLettered;
        LastError = Truncate(error, 4000);
        CompletedAt = deadLetteredAt.ToUniversalTime();
        NextAttemptAt = null;
        ClaimedAt = null;
        ClaimedBy = null;
    }

    private static string Truncate(string value, int maximumLength) =>
        string.IsNullOrEmpty(value) ? string.Empty
            : value.Length <= maximumLength ? value : value[..maximumLength];
}

internal static class FraudBackgroundScoringJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>
///     Durable handoff for deferred fraud re-scoring (issue #1218 G3). The original bounded
///     in-memory <c>DropWrite</c> channel lost every deferred job on a process restart and
///     silently dropped jobs when full — the enqueue path is post-decision, so nothing
///     re-enqueued them. This store persists a <see cref="FraudBackgroundScoringJob"/> row (the
///     module's outbox/inbox house pattern, cf. the payment retry queue due sweep) and the
///     worker claims due rows with a lease, so restarts and crashes lose nothing. Overflow
///     policy is unchanged in spirit: the enqueue can never block or reject the payment path —
///     a failed persist is logged, counted (<c>enqueue_abandoned</c>) and reported as false.
/// </summary>
public interface IFraudBackgroundScoringJobStore
{
    /// <summary>
    ///     Persists one deferred re-scoring job. Returns false when the row could not be
    ///     persisted — the caller treats that as a best-effort miss, never an error.
    /// </summary>
    Task<bool> TryEnqueueAsync(
        FraudSignalContext context,
        IReadOnlyList<string> deferredProviders,
        CancellationToken cancellationToken = default);

    /// <summary>Claims due jobs (Pending and due, or Processing past the claim lease) for a worker.</summary>
    Task<IReadOnlyList<FraudBackgroundScoringJob>> ClaimDueAsync(
        int maxBatch,
        string workerId,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a claimed job Completed.</summary>
    Task CompleteAsync(FraudBackgroundScoringJob job, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records a processing failure: exponential backoff re-pending, or the terminal
    ///     dead-letter once the attempt ceiling is reached.
    /// </summary>
    Task FailAsync(FraudBackgroundScoringJob job, string error, CancellationToken cancellationToken = default);

    /// <summary>Terminal poison transition for jobs that can never succeed (e.g. corrupt payload).</summary>
    Task DeadLetterAsync(FraudBackgroundScoringJob job, string error, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class FraudBackgroundScoringJobStore(
    IApplicationDbContext context,
    ILogger<FraudBackgroundScoringJobStore> logger) : IFraudBackgroundScoringJobStore
{
    /// <summary>
    ///     Attempt ceiling before a job is dead-lettered (issue #1218 G3). Three attempts match
    ///     the refund workflow's retry budget; a missed re-score is low-harm because the
    ///     synchronous decision stands.
    /// </summary>
    public const int MaxAttempts = 3;

    /// <summary>Claim lease: a Processing row past this window is reclaimed (crash recovery).</summary>
    public static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<bool> TryEnqueueAsync(
        FraudSignalContext signalContext,
        IReadOnlyList<string> deferredProviders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalContext);
        ArgumentNullException.ThrowIfNull(deferredProviders);

        try
        {
            context.Set<FraudBackgroundScoringJob>().Add(
                FraudBackgroundScoringJob.Create(signalContext, deferredProviders));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            FraudBackgroundScoringMetrics.RecordEnqueued();
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Overflow policy (issue #1218 G3): the durable queue can never block or reject the
            // payment path. A failed persist is the new "dropped write" — logged, counted, and
            // reported to the caller as a best-effort miss; the synchronous decision stands.
            logger.LogError(
                exception,
                "Persisting the deferred fraud re-scoring job for payment {PaymentId} failed; the synchronous decision stands",
                signalContext.PaymentId);
            FraudBackgroundScoringMetrics.RecordEnqueueAbandoned();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FraudBackgroundScoringJob>> ClaimDueAsync(
        int maxBatch,
        string workerId,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var leaseCutoff = now - ClaimLease;
        var dueIds = await context.Set<FraudBackgroundScoringJob>()
            .AsNoTracking()
            .Where(job => job.Status == FraudBackgroundScoringJobStatus.Pending
                    && (job.NextAttemptAt == null || job.NextAttemptAt <= now)
                || (job.Status == FraudBackgroundScoringJobStatus.Processing && job.ClaimedAt < leaseCutoff))
            .OrderBy(job => job.NextAttemptAt)
            .ThenBy(job => job.Id)
            .Take(Math.Max(1, maxBatch))
            .Select(job => job.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var claimed = new List<FraudBackgroundScoringJob>(dueIds.Length);
        foreach (var jobId in dueIds)
        {
            var job = await context.Set<FraudBackgroundScoringJob>()
                .FirstOrDefaultAsync(value => value.Id == jobId, cancellationToken)
                .ConfigureAwait(false);
            if (job is null
                || job.Status is FraudBackgroundScoringJobStatus.Completed
                    or FraudBackgroundScoringJobStatus.DeadLettered)
            {
                continue;
            }

            try
            {
                job.Claim(workerId, SystemClock.UtcNow);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                claimed.Add(job);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another worker won the claim; its cycle owns the job.
            }
        }

        return claimed;
    }

    /// <inheritdoc />
    public async Task CompleteAsync(FraudBackgroundScoringJob job, CancellationToken cancellationToken = default)
    {
        job.Complete(SystemClock.UtcNow);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        FraudBackgroundScoringMetrics.RecordCompleted();
    }

    /// <inheritdoc />
    public async Task FailAsync(FraudBackgroundScoringJob job, string error, CancellationToken cancellationToken = default)
    {
        if (job.AttemptCount >= MaxAttempts)
        {
            job.DeadLetter(error, SystemClock.UtcNow);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            FraudBackgroundScoringMetrics.RecordDeadLettered();
            return;
        }

        var delaySeconds = Math.Min(300, 1 << Math.Min(job.AttemptCount, 8));
        job.Fail(error, SystemClock.UtcNow.AddSeconds(delaySeconds));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        FraudBackgroundScoringMetrics.RecordRetryScheduled();
    }

    /// <inheritdoc />
    public async Task DeadLetterAsync(FraudBackgroundScoringJob job, string error, CancellationToken cancellationToken = default)
    {
        job.DeadLetter(error, SystemClock.UtcNow);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        FraudBackgroundScoringMetrics.RecordDeadLettered();
    }
}
