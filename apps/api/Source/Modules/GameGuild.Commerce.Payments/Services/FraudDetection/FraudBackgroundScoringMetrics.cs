using System.Diagnostics.Metrics;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     OpenTelemetry metrics for the durable fraud background re-scoring queue (issue #1218 G3),
///     following the payment retry queue metric conventions (<see cref="PaymentRetryQueueMetrics"/>).
///     <c>enqueue_abandoned_total</c> is the explicit overflow signal: a deferred job whose
///     durable persist failed — the synchronous decision stands, same contract as the old
///     drop-on-full channel, but now counted and logged.
/// </summary>
public static class FraudBackgroundScoringMetrics
{
    public const string MeterName = "GameGuild.Payments.FraudBackgroundScoring";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> Enqueued = Meter.CreateCounter<long>(
        "gameguild_payments_fraud_rescoring_enqueued_total",
        "jobs",
        "Deferred fraud re-scoring jobs durably enqueued after a synchronous Allow.");

    private static readonly Counter<long> EnqueueAbandoned = Meter.CreateCounter<long>(
        "gameguild_payments_fraud_rescoring_enqueue_abandoned_total",
        "jobs",
        "Deferred fraud re-scoring jobs whose durable enqueue failed (overflow policy: the synchronous decision stands).");

    private static readonly Counter<long> Completed = Meter.CreateCounter<long>(
        "gameguild_payments_fraud_rescoring_completed_total",
        "jobs",
        "Deferred fraud re-scoring jobs completed by the background worker.");

    private static readonly Counter<long> RetriesScheduled = Meter.CreateCounter<long>(
        "gameguild_payments_fraud_rescoring_retries_scheduled_total",
        "jobs",
        "Failed fraud re-scoring jobs re-pended with exponential backoff.");

    private static readonly Counter<long> DeadLettered = Meter.CreateCounter<long>(
        "gameguild_payments_fraud_rescoring_dead_lettered_total",
        "jobs",
        "Fraud re-scoring jobs dead-lettered after exhausting the attempt ceiling.");

    public static void RecordEnqueued() => Enqueued.Add(1);

    public static void RecordEnqueueAbandoned() => EnqueueAbandoned.Add(1);

    public static void RecordCompleted() => Completed.Add(1);

    public static void RecordRetryScheduled() => RetriesScheduled.Add(1);

    public static void RecordDeadLettered() => DeadLettered.Add(1);
}
