namespace GameGuild.Commerce.Billing;

/// <summary>
///     One raw entry of the durable billing event log for a subscription aggregate
///     (issue #396 state reconstruction). The payload is the exact JSON persisted in the
///     platform outbox row, so a projection can be rebuilt without the emitting code.
/// </summary>
/// <param name="EventId">Durable event identifier.</param>
/// <param name="EventName">Stable event name, e.g. commerce.billing.invoice-paid.v1.</param>
/// <param name="SchemaVersion">Schema version of the persisted payload.</param>
/// <param name="TenantId">Tenant scope of the event.</param>
/// <param name="CorrelationId">Correlation identifier of the emitting operation.</param>
/// <param name="OccurredAtUtc">When the event occurred.</param>
/// <param name="PayloadJson">Raw persisted payload JSON.</param>
public sealed record LoggedBillingEvent(
    Guid EventId,
    string EventName,
    int SchemaVersion,
    Guid TenantId,
    Guid CorrelationId,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);

/// <summary>
///     Subscription state rebuilt exclusively from the durable billing event log.
///     Every field is derived by folding <see cref="LoggedBillingEvent" /> entries in
///     deterministic order; nothing is read from the live subscription row.
/// </summary>
/// <param name="SubscriptionId">Aggregate identifier the state was rebuilt for.</param>
/// <param name="AppliedEventCount">Number of events the fold interpreted.</param>
/// <param name="SkippedEvents">Event names the fold does not interpret (forward compatibility).</param>
/// <param name="ProjectedImage">Snapshot of the subscription image after replay.</param>
public sealed record SubscriptionEventStateProjection(
    Guid SubscriptionId,
    int AppliedEventCount,
    IReadOnlyList<string> SkippedEvents,
    SubscriptionProjectedState ProjectedImage)
{
    /// <summary>Empty projection for a subscription with no durable billing events.</summary>
    public static SubscriptionEventStateProjection Empty(Guid subscriptionId) => new(
        subscriptionId,
        AppliedEventCount: 0,
        SkippedEvents: [],
        ProjectedImage: new SubscriptionProjectedState(
            TenantId: null,
            ProjectedStatus: SubscriptionProjectedStatus.NoBillingEvents,
            CurrentPlanId: null,
            PlanChangeCount: 0,
            PaymentCount: 0,
            RenewalCount: 0,
            TotalPaidAmount: 0m,
            LastPaymentAmount: null,
            LastPaymentCurrency: null,
            LastPaymentAtUtc: null,
            LastProvider: null,
            CancelledAtUtc: null,
            LastChangeSagaId: null,
            LastChangeOutcome: null));
}

/// <summary>
///     The projected subscription image produced by the deterministic fold. Status is the
///     status the durable billing log can evidence: cancellations and paid activity are
///     recorded as named events, while statuses only the subscription state machine knows
///     (trialing, suspended, past-due) are not billing events and project as
///     <see cref="SubscriptionProjectedStatus.BillingActive" /> when payment activity exists.
/// </summary>
public sealed record SubscriptionProjectedState(
    Guid? TenantId,
    SubscriptionProjectedStatus ProjectedStatus,
    Guid? CurrentPlanId,
    int PlanChangeCount,
    int PaymentCount,
    int RenewalCount,
    decimal TotalPaidAmount,
    decimal? LastPaymentAmount,
    string? LastPaymentCurrency,
    DateTimeOffset? LastPaymentAtUtc,
    string? LastProvider,
    DateTimeOffset? CancelledAtUtc,
    Guid? LastChangeSagaId,
    string? LastChangeOutcome);

/// <summary>Status derivable purely from billing events.</summary>
public enum SubscriptionProjectedStatus
{
    /// <summary>No durable billing events exist for the subscription.</summary>
    NoBillingEvents = 0,

    /// <summary>Payment activity exists and no cancellation event follows it.</summary>
    BillingActive = 1,

    /// <summary>A cancellation event is the latest lifecycle evidence in the log.</summary>
    Cancelled = 2
}

/// <summary>
///     Live snapshot facts taken from the authoritative <see cref="Subscription" /> row,
///     used as the verification baseline for the event-sourced projection.
/// </summary>
public sealed record SubscriptionStateFacts(
    Guid SubscriptionId,
    Guid TenantId,
    Guid PlanId,
    bool IsCancelled,
    DateTimeOffset? LastPaymentAtUtc);

/// <summary>One mismatch between the projected state and the live snapshot.</summary>
/// <param name="Fact">Name of the fact that drifted.</param>
/// <param name="Projected">Value derived from the durable event log.</param>
/// <param name="Live">Value observed on the live subscription row.</param>
public sealed record SubscriptionStateDrift(string Fact, string? Projected, string? Live);

/// <summary>
///     Snapshot verification result: the projection is consistent when every fact the
///     durable log can evidence matches the live subscription row.
/// </summary>
public sealed record SubscriptionStateVerification(
    bool IsConsistent,
    IReadOnlyList<SubscriptionStateDrift> Drifts)
{
    /// <summary>Consistent verification with no drifts.</summary>
    public static readonly SubscriptionStateVerification Consistent = new(true, []);

    /// <summary>Inconsistent verification carrying the observed drifts.</summary>
    public static SubscriptionStateVerification Inconsistent(IEnumerable<SubscriptionStateDrift> drifts) =>
        new(false, drifts.ToArray());
}

/// <summary>
///     Deterministic state reconstruction over the durable billing event log (issue #396):
///     folds <see cref="LoggedBillingEvent" /> entries into a projected subscription state
///     and verifies the projection against the live subscription snapshot.
/// </summary>
public interface ISubscriptionEventStateProjection
{
    /// <summary>
    ///     Rebuilds the subscription state from the durable event log. The fold is pure and
    ///     deterministic: events are ordered by (<see cref="LoggedBillingEvent.OccurredAtUtc" />,
    ///     <see cref="LoggedBillingEvent.EventId" />) before interpretation, so the same log
    ///     always produces the same projection regardless of insertion order.
    /// </summary>
    SubscriptionEventStateProjection Replay(Guid subscriptionId, IEnumerable<LoggedBillingEvent> events);

    /// <summary>
    ///     Verifies the rebuilt projection against the live subscription snapshot. Facts the
    ///     durable log cannot evidence (never-paid subscriptions, non-billing statuses) are
    ///     skipped rather than reported as drift.
    /// </summary>
    SubscriptionStateVerification Verify(SubscriptionEventStateProjection projection, SubscriptionStateFacts liveFacts);
}

/// <summary>
///     Port to the raw durable billing event log of one subscription aggregate. Implemented
///     by the host on the platform outbox (<c>AggregateType = Subscription</c>); the billing
///     module owns the contract so the projection stays transport-agnostic.
/// </summary>
public interface IBillingSubscriptionEventLog
{
    /// <summary>
    ///     Reads the durable billing events of one subscription, ordered by occurrence time
    ///     (ties broken by event id) — the deterministic replay order.
    /// </summary>
    Task<IReadOnlyList<LoggedBillingEvent>> ReadAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
