using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     <see cref="ISubscriptionEventStateProjection" /> implementation (issue #396 state
///     reconstruction): a pure, deterministic fold over the durable billing event log.
///     Payloads are interpreted by stable event name through <see cref="JsonDocument" />
///     (not by CLR type), so a payload written by an older schema version still replays
///     as long as the folded fields are present, and unknown event names are collected
///     instead of failing the replay.
/// </summary>
public sealed class SubscriptionEventStateProjectionService : ISubscriptionEventStateProjection
{
    private const string InvoicePaidEvent = "commerce.billing.invoice-paid.v1";
    private const string SubscriptionRenewedEvent = "commerce.billing.subscription-renewed.v1";
    private const string SubscriptionCancelledEvent = "commerce.billing.subscription-cancelled.v1";
    private const string ChangeStepEvent = "commerce.billing.subscription-change-step.v1";
    private const string ChangeCompletedEvent = "commerce.billing.subscription-change-completed.v1";
    private const string ChangeFailedEvent = "commerce.billing.subscription-change-failed.v1";

    /// <inheritdoc />
    public SubscriptionEventStateProjection Replay(Guid subscriptionId, IEnumerable<LoggedBillingEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Deterministic replay order: occurrence time, ties broken by event id, so the
        // same durable log always folds to the same projection regardless of insertion
        // or query order.
        var ordered = events
            .OrderBy(@event => @event.OccurredAtUtc)
            .ThenBy(@event => @event.EventId)
            .ToArray();

        Guid? tenantId = null;
        Guid? currentPlanId = null;
        var planChangeCount = 0;
        var paymentCount = 0;
        var renewalCount = 0;
        decimal totalPaid = 0m;
        decimal? lastPaymentAmount = null;
        string? lastPaymentCurrency = null;
        DateTimeOffset? lastPaymentAtUtc = null;
        string? lastProvider = null;
        DateTimeOffset? cancelledAtUtc = null;
        Guid? lastChangeSagaId = null;
        string? lastChangeOutcome = null;
        var skipped = new List<string>();
        var applied = 0;

        foreach (var @event in ordered)
        {
            tenantId ??= @event.TenantId;
            switch (@event.EventName)
            {
                case InvoicePaidEvent:
                    using (var document = Parse(@event))
                    {
                        var root = document.RootElement;
                        paymentCount++;
                        if (root.TryGetProperty("amount", out var amountElement)
                            && amountElement.ValueKind == JsonValueKind.Number
                            && amountElement.TryGetDecimal(out var amount))
                        {
                            totalPaid += amount;
                            lastPaymentAmount = amount;
                        }

                        if (root.TryGetProperty("currency", out var currencyElement)
                            && currencyElement.ValueKind == JsonValueKind.String)
                        {
                            lastPaymentCurrency = currencyElement.GetString();
                        }

                        lastProvider = ReadString(root, "provider") ?? lastProvider;
                    }

                    lastPaymentAtUtc = @event.OccurredAtUtc;
                    applied++;
                    break;

                case SubscriptionRenewedEvent:
                    using (var document = Parse(@event))
                    {
                        lastProvider = ReadString(document.RootElement, "provider") ?? lastProvider;
                    }

                    renewalCount++;
                    applied++;
                    break;

                case SubscriptionCancelledEvent:
                    using (var document = Parse(@event))
                    {
                        lastProvider = ReadString(document.RootElement, "provider") ?? lastProvider;
                    }

                    cancelledAtUtc = @event.OccurredAtUtc;
                    applied++;
                    break;

                case ChangeStepEvent:
                    // Step transitions are observability records: they witness saga
                    // progress but never move subscription state on their own.
                    applied++;
                    break;

                case ChangeCompletedEvent:
                    using (var document = Parse(@event))
                    {
                        var root = document.RootElement;
                        if (Guid.TryParse(ReadString(root, "toPlanId"), out var toPlanId))
                        {
                            currentPlanId = toPlanId;
                        }

                        planChangeCount++;
                        if (Guid.TryParse(ReadString(root, "sagaId"), out var sagaId))
                        {
                            lastChangeSagaId = sagaId;
                        }
                    }

                    lastChangeOutcome = "Completed";
                    applied++;
                    break;

                case ChangeFailedEvent:
                    using (var document = Parse(@event))
                    {
                        var root = document.RootElement;
                        if (Guid.TryParse(ReadString(root, "sagaId"), out var sagaId))
                        {
                            lastChangeSagaId = sagaId;
                        }
                    }

                    lastChangeOutcome = "Failed";
                    applied++;
                    break;

                default:
                    // Forward compatibility: events the fold does not interpret are
                    // reported, never silently dropped and never fatal.
                    if (!skipped.Contains(@event.EventName))
                    {
                        skipped.Add(@event.EventName);
                    }

                    break;
            }
        }

        var status = cancelledAtUtc.HasValue
            ? SubscriptionProjectedStatus.Cancelled
            : applied > 0
                ? SubscriptionProjectedStatus.BillingActive
                : SubscriptionProjectedStatus.NoBillingEvents;

        return new SubscriptionEventStateProjection(
            subscriptionId,
            applied,
            skipped,
            new SubscriptionProjectedState(
                tenantId,
                status,
                currentPlanId,
                planChangeCount,
                paymentCount,
                renewalCount,
                totalPaid,
                lastPaymentAmount,
                lastPaymentCurrency,
                lastPaymentAtUtc,
                lastProvider,
                cancelledAtUtc,
                lastChangeSagaId,
                lastChangeOutcome));
    }

    /// <inheritdoc />
    public SubscriptionStateVerification Verify(
        SubscriptionEventStateProjection projection,
        SubscriptionStateFacts liveFacts)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(liveFacts);

        var drifts = new List<SubscriptionStateDrift>();
        var image = projection.ProjectedImage;

        // A cancellation event in the durable log must be reflected by the live row.
        // The reverse is deliberately not drift: subscriptions can be cancelled through
        // the direct lifecycle command, which does not write a billing event.
        var projectedCancelled = image.ProjectedStatus == SubscriptionProjectedStatus.Cancelled;
        if (projectedCancelled && !liveFacts.IsCancelled)
        {
            drifts.Add(new SubscriptionStateDrift(
                "IsCancelled",
                projectedCancelled.ToString(),
                liveFacts.IsCancelled.ToString()));
        }

        // Plan identity is only checkable once the log contains at least one completed
        // plan change; subscriptions never migrated through the saga keep their
        // originating plan id outside the billing log.
        if (image.CurrentPlanId is { } projectedPlanId && projectedPlanId != liveFacts.PlanId)
        {
            drifts.Add(new SubscriptionStateDrift("PlanId", projectedPlanId.ToString(), liveFacts.PlanId.ToString()));
        }

        // Last payment time is evidenced by invoice-paid events; never-paid
        // subscriptions cannot drift on a fact the log never observed.
        if (image.LastPaymentAtUtc is { } projectedLastPayment && liveFacts.LastPaymentAtUtc is { } liveLastPayment)
        {
            if (Math.Abs((projectedLastPayment - liveLastPayment).TotalSeconds) > 1)
            {
                drifts.Add(new SubscriptionStateDrift(
                    "LastPaymentAtUtc",
                    projectedLastPayment.ToString("O"),
                    liveLastPayment.ToString("O")));
            }
        }

        if (image.TenantId is { } projectedTenantId && projectedTenantId != liveFacts.TenantId)
        {
            drifts.Add(new SubscriptionStateDrift(
                "TenantId",
                projectedTenantId.ToString(),
                liveFacts.TenantId.ToString()));
        }

        return drifts.Count == 0
            ? SubscriptionStateVerification.Consistent
            : SubscriptionStateVerification.Inconsistent(drifts);
    }

    private static JsonDocument Parse(LoggedBillingEvent @event) =>
        JsonDocument.Parse(@event.PayloadJson);

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
