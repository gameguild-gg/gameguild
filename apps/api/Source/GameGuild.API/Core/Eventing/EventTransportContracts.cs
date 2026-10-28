namespace GameGuild.API.Eventing;

public interface IOutboxDispatcher
{
    Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default);
}

public interface IInboxStore
{
    Task<InboxReceipt?> TryLockAsync(
        Guid eventId,
        string consumerName,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(InboxReceipt receipt, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<InboxReceipt> RecordFailureAsync(
        Guid eventId,
        string consumerName,
        Exception exception,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public interface IEventReplayService
{
    Task<EventTransportStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeadLetterEvent>> GetDeadLettersAsync(CancellationToken cancellationToken = default);
    Task<bool> ReplayAsync(Guid eventId, string? consumerName = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads the durable event log of one aggregate in deterministic replay order
    ///     (issue #396 state reconstruction): occurrence time, ties broken by event id.
    ///     Raw payloads are returned so callers can fold state without the emitting code.
    /// </summary>
    Task<IReadOnlyList<DurableEventLogEntry>> GetAggregateEventLogAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken = default);
}

/// <summary>One raw entry of the durable event log for an aggregate.</summary>
public sealed record DurableEventLogEntry(
    Guid EventId,
    string EventName,
    int SchemaVersion,
    Guid TenantId,
    Guid CorrelationId,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);

public sealed record EventTransportStatus(long Pending, long Completed, long DeadLettered, long IncompleteConsumers);

public sealed record DeadLetterEvent(
    Guid EventId,
    string EventName,
    string ConsumerName,
    int AttemptCount,
    string? LastError,
    DateTimeOffset DeadLetteredAtUtc);
