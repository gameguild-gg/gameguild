using GameGuild.API.Eventing;
using GameGuild.Commerce.Billing;

namespace GameGuild.API.Core.Integration;

/// <summary>
///     Connects the Commerce.Billing module's <see cref="IBillingSubscriptionEventLog" /> port
///     to the extended event replay service (issue #396 state reconstruction): the platform
///     outbox is the durable event log, and subscriptions project their state from the raw
///     aggregate-scoped entries it already persists.
/// </summary>
public sealed class BillingSubscriptionEventLog(IEventReplayService replayService) : IBillingSubscriptionEventLog
{
    private const string SubscriptionAggregateType = "Subscription";

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoggedBillingEvent>> ReadAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var entries = await replayService.GetAggregateEventLogAsync(
                SubscriptionAggregateType,
                subscriptionId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);

        return entries
            .Select(entry => new LoggedBillingEvent(
                entry.EventId,
                entry.EventName,
                entry.SchemaVersion,
                entry.TenantId,
                entry.CorrelationId,
                entry.OccurredAtUtc,
                entry.PayloadJson))
            .ToArray();
    }
}
