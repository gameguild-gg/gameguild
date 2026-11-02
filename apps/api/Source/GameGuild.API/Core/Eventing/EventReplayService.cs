using Microsoft.EntityFrameworkCore;
using GameGuild.API.Database;

namespace GameGuild.API.Eventing;

public sealed class EventReplayService(ApplicationDbContext db, TimeProvider timeProvider) : IEventReplayService
{
    public async Task<EventTransportStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var outbox = db.Set<OutboxMessage>().AsNoTracking();
        return new EventTransportStatus(
            await outbox.LongCountAsync(message => message.CompletedAtUtc == null && message.DeadLetteredAtUtc == null, cancellationToken),
            await outbox.Where(message => message.CompletedAtUtc != null).Select(message => message.EventId)
                // Retirement changes both tables atomically, so these committed sets are disjoint.
                .Concat(db.Set<OutboxDeliveryReceipt>().Where(receipt => receipt.CompletedAtUtc != null)
                    .Select(receipt => receipt.EventId)).LongCountAsync(cancellationToken),
            await outbox.LongCountAsync(message => message.DeadLetteredAtUtc != null, cancellationToken),
            await db.Set<InboxReceipt>().LongCountAsync(
                receipt => receipt.CompletedAtUtc == null && receipt.DeadLetteredAtUtc == null,
                cancellationToken));
    }

    public async Task<IReadOnlyList<DeadLetterEvent>> GetDeadLettersAsync(
        CancellationToken cancellationToken = default) =>
        await (from receipt in db.Set<InboxReceipt>().AsNoTracking()
               join message in db.Set<OutboxMessage>().AsNoTracking() on receipt.EventId equals message.EventId
               where receipt.DeadLetteredAtUtc != null
               orderby receipt.DeadLetteredAtUtc descending
               select new DeadLetterEvent(
                   receipt.EventId,
                   message.EventName,
                   receipt.ConsumerName,
                   receipt.AttemptCount,
                   receipt.LastError,
                   receipt.DeadLetteredAtUtc!.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ReplayAsync(
        Guid eventId,
        string? consumerName = null,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await ReplayCoreAsync(eventId, consumerName, cancellationToken).ConfigureAwait(false);
        }

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(() =>
            ReplayCoreAsync(eventId, consumerName, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> ReplayCoreAsync(Guid eventId, string? consumerName, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false) : null;
        // Match the cleanup lock order: parent first, then consumer receipts.
        var message = db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true
            ? (await db.Set<OutboxMessage>().FromSqlInterpolated($$"""
                SELECT * FROM "gameguild.integration".outbox_messages
                WHERE "EventId" = {{eventId}} FOR UPDATE
                """).ToListAsync(cancellationToken).ConfigureAwait(false)).SingleOrDefault()
            : await db.Set<OutboxMessage>().FindAsync([eventId], cancellationToken).ConfigureAwait(false);
        if (message is null)
        {
            return false;
        }

        var receipts = await db.Set<InboxReceipt>()
            .Where(receipt => receipt.EventId == eventId
                              && (consumerName == null
                                  ? receipt.DeadLetteredAtUtc != null
                                  : receipt.ConsumerName == consumerName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (receipts.Count == 0)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        foreach (var receipt in receipts)
        {
            receipt.AttemptCount = 0;
            receipt.NextAttemptAtUtc = now;
            receipt.CompletedAtUtc = null;
            receipt.DeadLetteredAtUtc = null;
            receipt.LastError = null;
            receipt.UpdatedAtUtc = now;
        }

        message.CompletedAtUtc = null;
        message.DeadLetteredAtUtc = null;
        message.ClaimedUntilUtc = null;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        return true;
    }

    public async Task<IReadOnlyList<DurableEventLogEntry>> GetAggregateEventLogAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateType))
        {
            throw new ArgumentException("Aggregate type is required.", nameof(aggregateType));
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate id is required.", nameof(aggregateId));
        }

        var messages = await db.Set<OutboxMessage>().AsNoTracking()
            .Where(message => message.AggregateType == aggregateType && message.AggregateId == aggregateId)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.EventId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return messages
            .Select(message => new DurableEventLogEntry(
                message.EventId,
                message.EventName,
                message.SchemaVersion,
                message.TenantId,
                message.CorrelationId,
                new DateTimeOffset(message.OccurredAtUtc, TimeSpan.Zero),
                message.Payload))
            .ToArray();
    }
}
