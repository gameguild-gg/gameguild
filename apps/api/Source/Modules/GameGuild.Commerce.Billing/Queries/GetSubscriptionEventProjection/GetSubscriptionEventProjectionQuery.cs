using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Commerce.Subscriptions;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Read model returned by <see cref="GetSubscriptionEventProjectionQuery" /> (issue #396
///     state reconstruction): the state rebuilt from the durable billing event log, the
///     verification against the live subscription snapshot, and the replayed event trail.
/// </summary>
public sealed record SubscriptionEventProjectionDto(
    Guid SubscriptionId,
    Guid TenantId,
    int AppliedEventCount,
    IReadOnlyList<string> SkippedEvents,
    string ProjectedStatus,
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
    string? LastChangeOutcome,
    bool VerificationConsistent,
    IReadOnlyList<SubscriptionStateDriftDto> VerificationDrifts,
    IReadOnlyList<SubscriptionProjectionEventDto> Events);

/// <summary>One drift between the projected state and the live subscription snapshot.</summary>
public sealed record SubscriptionStateDriftDto(string Fact, string? Projected, string? Live);

/// <summary>One replayed durable billing event, in replay order.</summary>
public sealed record SubscriptionProjectionEventDto(
    Guid EventId,
    string EventName,
    int SchemaVersion,
    DateTimeOffset OccurredAtUtc);

/// <summary>
///     Query (issue #396 state reconstruction): rebuild one subscription's state from the
///     durable billing event log and verify it against the live subscription snapshot.
/// </summary>
public sealed record GetSubscriptionEventProjectionQuery(Guid SubscriptionId)
    : IQuery<SubscriptionEventProjectionDto?>;

/// <summary>Validator for <see cref="GetSubscriptionEventProjectionQuery" />.</summary>
public sealed class GetSubscriptionEventProjectionQueryValidator : AbstractValidator<GetSubscriptionEventProjectionQuery>
{
    public GetSubscriptionEventProjectionQueryValidator()
    {
        RuleFor(query => query.SubscriptionId).NotEmpty();
    }
}

/// <summary>
///     Handler for <see cref="GetSubscriptionEventProjectionQuery" />: reads the raw durable
///     event log of the subscription aggregate, folds it deterministically into a projected
///     state and verifies the projection against the live subscription row.
/// </summary>
public sealed class GetSubscriptionEventProjectionQueryHandler(
    IBillingSubscriptionEventLog eventLog,
    ISubscriptionEventStateProjection projection,
    ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetSubscriptionEventProjectionQuery, SubscriptionEventProjectionDto?>
{
    /// <inheritdoc />
    public async Task<SubscriptionEventProjectionDto?> Handle(
        GetSubscriptionEventProjectionQuery request,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository
            .GetByIdAsync(request.SubscriptionId, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return null;
        }

        var events = await eventLog
            .ReadAsync(request.SubscriptionId, cancellationToken)
            .ConfigureAwait(false);
        var replayed = projection.Replay(request.SubscriptionId, events);

        var liveFacts = new SubscriptionStateFacts(
            subscription.Id,
            subscription.TenantId ?? Guid.Empty,
            subscription.PlanId,
            subscription.IsCancelled,
            subscription.LastPaymentAt.HasValue
                ? new DateTimeOffset(subscription.LastPaymentAt.Value, TimeSpan.Zero)
                : null);
        var verification = projection.Verify(replayed, liveFacts);

        return new SubscriptionEventProjectionDto(
            replayed.SubscriptionId,
            replayed.ProjectedImage.TenantId ?? liveFacts.TenantId,
            replayed.AppliedEventCount,
            replayed.SkippedEvents,
            replayed.ProjectedImage.ProjectedStatus.ToString(),
            replayed.ProjectedImage.CurrentPlanId,
            replayed.ProjectedImage.PlanChangeCount,
            replayed.ProjectedImage.PaymentCount,
            replayed.ProjectedImage.RenewalCount,
            replayed.ProjectedImage.TotalPaidAmount,
            replayed.ProjectedImage.LastPaymentAmount,
            replayed.ProjectedImage.LastPaymentCurrency,
            replayed.ProjectedImage.LastPaymentAtUtc,
            replayed.ProjectedImage.LastProvider,
            replayed.ProjectedImage.CancelledAtUtc,
            replayed.ProjectedImage.LastChangeSagaId,
            replayed.ProjectedImage.LastChangeOutcome,
            verification.IsConsistent,
            verification.Drifts
                .Select(drift => new SubscriptionStateDriftDto(drift.Fact, drift.Projected, drift.Live))
                .ToArray(),
            events
                .Select(@event => new SubscriptionProjectionEventDto(
                    @event.EventId,
                    @event.EventName,
                    @event.SchemaVersion,
                    @event.OccurredAtUtc))
                .ToArray());
    }
}
