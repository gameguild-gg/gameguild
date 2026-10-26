using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.Commerce.Billing;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Core.Integration;

/// <summary>
///     Connects the Commerce.Billing module's <see cref="IBillingOutboxEventReader" /> port to the
///     platform outbox read model. Only events published by the Commerce.Billing module are
///     returned, including compact completion metadata after payload retirement.
/// </summary>
public sealed class BillingOutboxEventReader(ApplicationDbContext context) : IBillingOutboxEventReader
{
    private const string BillingSourceModule = "Commerce.Billing";

    /// <inheritdoc />
    public async Task<IReadOnlyList<BillingOutboxEventDto>> ListAsync(
        int skip,
        int take,
        string? eventName,
        BillingOutboxEventStatus? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default)
    {
        var messages = await BuildQuery(eventName, status, fromUtc, toUtc)
            .OrderByDescending(message => message.OccurredAtUtc)
            .ThenBy(message => message.EventId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return messages
            .Select(message => ToDto(message))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        string? eventName,
        BillingOutboxEventStatus? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(eventName, status, fromUtc, toUtc)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<BillingOutboxEventDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var message = await BuildQuery(null, null, null, null)
            .Where(message => message.EventId == eventId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return message is null ? null : ToDto(message);
    }

    private IQueryable<EventRow> BuildQuery(
        string? eventName,
        BillingOutboxEventStatus? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        var current = context.Set<OutboxMessage>().AsNoTracking()
            .Where(message => message.SourceModule == BillingSourceModule)
            .Select(message => new EventRow
            {
                EventId = message.EventId, EventName = message.EventName, EventType = message.EventType,
                AggregateType = message.AggregateType, AggregateId = message.AggregateId,
                TenantId = message.TenantId, CorrelationId = message.CorrelationId,
                OccurredAtUtc = message.OccurredAtUtc, SchemaVersion = message.SchemaVersion,
                Status = message.DeadLetteredAtUtc != null ? BillingOutboxEventStatus.DeadLettered
                    : message.CompletedAtUtc != null ? BillingOutboxEventStatus.Completed : BillingOutboxEventStatus.Pending
            });
        var retired = context.Set<OutboxDeliveryReceipt>().AsNoTracking()
            .Where(receipt => receipt.SourceModule == BillingSourceModule && receipt.CompletedAtUtc != null
                && !context.Set<OutboxMessage>().Any(message => message.EventId == receipt.EventId))
            .Select(receipt => new EventRow
            {
                EventId = receipt.EventId, EventName = receipt.EventName!, EventType = receipt.EventType!,
                AggregateType = receipt.AggregateType!, AggregateId = receipt.AggregateId!,
                TenantId = receipt.TenantId!.Value, CorrelationId = receipt.CorrelationId!.Value,
                OccurredAtUtc = receipt.OccurredAtUtc!.Value, SchemaVersion = receipt.SchemaVersion!.Value,
                Status = BillingOutboxEventStatus.Completed
            });
        var query = current.Concat(retired);

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            var name = eventName;
            query = query.Where(message => message.EventName == name);
        }

        if (status.HasValue)
        {
            query = status.Value switch
            {
                BillingOutboxEventStatus.Completed => query.Where(message => message.Status == BillingOutboxEventStatus.Completed),
                BillingOutboxEventStatus.DeadLettered => query.Where(message => message.Status == BillingOutboxEventStatus.DeadLettered),
                BillingOutboxEventStatus.Pending => query.Where(message => message.Status == BillingOutboxEventStatus.Pending),
                _ => query
            };
        }

        if (fromUtc.HasValue)
        {
            var from = fromUtc.Value;
            query = query.Where(message => message.OccurredAtUtc >= from);
        }

        if (toUtc.HasValue)
        {
            var to = toUtc.Value;
            query = query.Where(message => message.OccurredAtUtc <= to);
        }

        return query;
    }

    private static BillingOutboxEventDto ToDto(EventRow message)
    {
        return new BillingOutboxEventDto(
            message.EventId,
            message.EventName,
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            message.TenantId,
            message.CorrelationId,
            message.Status,
            message.OccurredAtUtc,
            message.SchemaVersion);
    }

    private sealed class EventRow
    {
        public Guid EventId { get; init; }
        public string EventName { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public string AggregateType { get; init; } = string.Empty;
        public string AggregateId { get; init; } = string.Empty;
        public Guid TenantId { get; init; }
        public Guid CorrelationId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public int SchemaVersion { get; init; }
        public BillingOutboxEventStatus Status { get; init; }
    }
}
