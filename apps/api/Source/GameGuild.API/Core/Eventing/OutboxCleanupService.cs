using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Eventing;

/// <summary>Retires successful deliveries atomically, without deleting pending work or dead letters.</summary>
public sealed class OutboxCleanupService(ApplicationDbContext db, TimeProvider timeProvider)
{
    internal const int BatchSize = 1000;

    public Task<int> CleanupCompletedAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return CleanupCoreAsync(cancellationToken);
        }

        return db.Database.CreateExecutionStrategy().ExecuteAsync(() => CleanupCoreAsync(cancellationToken));
    }

    private async Task<int> CleanupCoreAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            // A single server transaction reserves identities before deleting child receipts
            // and payloads. The same bounded function supports an operational backlog drain.
            return await db.Database.SqlQuery<int>($$"""
                SELECT "gameguild.integration".retire_completed_outbox_messages(
                    {{BatchSize}}, {{now}}, NULL) AS "Value"
                """).SingleAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var transaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        var messages = await db.Set<OutboxMessage>().AsNoTracking()
                .Where(message => message.CompletedAtUtc != null && message.CompletedAtUtc <= now
                    && message.DeadLetteredAtUtc == null
                    && (message.ClaimedUntilUtc == null || message.ClaimedUntilUtc <= now)
                    && !db.Set<InboxReceipt>().Any(receipt => receipt.EventId == message.EventId
                        && (receipt.CompletedAtUtc == null || receipt.DeadLetteredAtUtc != null)))
                .OrderBy(message => message.CompletedAtUtc).ThenBy(message => message.EventId)
                .Take(BatchSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return 0;
        }

        var ids = messages.Select(message => message.EventId).ToArray();
        var remembered = await db.Set<OutboxDeliveryReceipt>().Where(receipt => ids.Contains(receipt.EventId))
            .ToDictionaryAsync(receipt => receipt.EventId, cancellationToken).ConfigureAwait(false);
        foreach (var message in messages)
        {
            var completed = OutboxDeliveryReceipt.FromCompleted(message);
            if (remembered.TryGetValue(message.EventId, out var reserved))
            {
                db.Entry(reserved).CurrentValues.SetValues(completed);
            }
            else
            {
                db.Add(completed);
            }
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        int removed;
        if (db.Database.IsRelational())
        {
            await db.Set<InboxReceipt>().Where(receipt => ids.Contains(receipt.EventId))
                .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            removed = await db.Set<OutboxMessage>().Where(message => ids.Contains(message.EventId))
                .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            db.RemoveRange(await db.Set<InboxReceipt>().Where(receipt => ids.Contains(receipt.EventId))
                .ToListAsync(cancellationToken).ConfigureAwait(false));
            db.RemoveRange(await db.Set<OutboxMessage>().Where(message => ids.Contains(message.EventId))
                .ToListAsync(cancellationToken).ConfigureAwait(false));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            removed = messages.Count;
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return removed;
    }
}
