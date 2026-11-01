using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     One deferred background re-scoring job (issue #409: asynchronous scoring for
///     background checks). Carries the exact signal context of the synchronous pass, so
///     the background re-score evaluates the same attempt — including the providers the
///     synchronous budget deferred — without reconstructing (or trusting) client input.
/// </summary>
/// <param name="PaymentId">Payment the synchronous assessment allowed.</param>
/// <param name="Context">Signal context snapshot of that assessment.</param>
/// <param name="DeferredProviders">Provider names deferred by the synchronous budget.</param>
public sealed record FraudBackgroundScoringJob(
    Guid PaymentId,
    FraudSignalContext Context,
    IReadOnlyList<string> DeferredProviders);

/// <summary>
///     Bounded in-process queue feeding the fraud background re-scoring worker (issue
///     #409). Enqueue is called from the synchronous payment path AFTER its own decision
///     is persisted, so queue behavior can never block or reject the payment: a full
///     queue drops the job with a warning and the synchronous decision stands.
/// </summary>
public interface IFraudBackgroundScoringQueue
{
    /// <summary>
    ///     Tries to enqueue a deferred re-scoring job. Returns false when the bounded
    ///     queue is full — the caller treats that as best-effort miss, never an error.
    /// </summary>
    bool TryEnqueue(FraudBackgroundScoringJob job);

    /// <summary>Reads the next job, waiting for one to arrive. Returns null on shutdown.</summary>
    Task<FraudBackgroundScoringJob?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>Approximate pending job count (observability).</summary>
    int PendingCount { get; }
}

/// <inheritdoc />
public sealed class FraudBackgroundScoringQueue(
    IOptionsMonitor<FraudAssessmentOptions> optionsMonitor,
    ILogger<FraudBackgroundScoringQueue> logger) : IFraudBackgroundScoringQueue
{
    private readonly Channel<FraudBackgroundScoringJob> _channel =
        Channel.CreateBounded<FraudBackgroundScoringJob>(
            new BoundedChannelOptions(Math.Max(16, optionsMonitor.CurrentValue.BackgroundRescoringQueueCapacity))
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true
            });

    /// <inheritdoc />
    public int PendingCount => _channel.Reader.Count;

    /// <inheritdoc />
    public bool TryEnqueue(FraudBackgroundScoringJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (!_channel.Writer.TryWrite(job))
        {
            logger.LogWarning(
                "Fraud background re-scoring queue is full (capacity {Capacity}); deferred evaluation for payment {PaymentId} was dropped and the synchronous decision stands",
                optionsMonitor.CurrentValue.BackgroundRescoringQueueCapacity,
                job.PaymentId);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<FraudBackgroundScoringJob?> DequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
}
