# Completed outbox deliveries

Apply `20261025170000_RetireCompletedOutboxMessages` before starting the updated
worker. Each dispatch cycle retires successful deliveries in batches of at most
1000, with a fresh context for each batch. A full dispatch batch is followed by
another dispatch cycle immediately; the two-second idle delay applies to a
partially filled or failed cycle.

Retirement requires a completed parent, no parent dead letter, no live lease,
and no unfinished or dead-lettered consumer receipt, including historical
consumers. The completion timestamp must be at or before the supplied cutoff.
Parent locks serialize replay, finalization and cleanup. Other workers skip locked
parents. The permanent identity is saved before inbox children and the payload
are deleted, in one transaction.

`outbox_delivery_receipts` reserves each new event identity with a unique insert.
Its completion timestamp remains null until retirement. Completed HTTP events
retain only their identity and completion time. Other events retain the envelope
metadata needed by administrative readers. Payloads and consumer error details
are never copied. Do not delete these identities: their unique constraint also
protects raw SQL publishers and older transaction snapshots against redelivery.

For an existing backlog, first inspect the primary, consumer states, replication
lag and free disk. On a large live database, create the two migration indexes
concurrently before applying the migration, so index construction does not block
application writers:

```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_outbox_pending_order
ON "gameguild.integration".outbox_messages ("OccurredAtUtc", "EventId")
WHERE "CompletedAtUtc" IS NULL AND "DeadLetteredAtUtc" IS NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_inbox_unfinished_event
ON "gameguild.integration".inbox_receipts ("EventId")
WHERE "CompletedAtUtc" IS NULL OR "DeadLetteredAtUtc" IS NOT NULL;
```

Check `pg_index.indisvalid` after concurrent construction; an interrupted build
can leave an invalid index that `IF NOT EXISTS` will skip. Refresh planner
statistics with `ANALYZE` after a large queue distribution change.

Use the same bounded cleanup function for an operational drain. Each call must
commit separately; check replication between batches and stop the drain if
application commits are waiting for replication. The third argument restricts
cleanup to one source module; pass `NULL` only after deploying the updated
administrative readers.

```sql
SELECT "gameguild.integration".retire_completed_outbox_messages(
    1000, clock_timestamp() - interval '5 minutes', 'Platform.Http');
```

The five-minute operational grace period protects finalizers from an older worker
version during rollout. The updated worker locks finalization against retirement
and can retire completed payloads immediately.

A return value of zero means no eligible unlocked deliveries were found in that
call. It does not prove that pending or leased work is absent. Inspect those
states separately. Never truncate the outbox/inbox or mark pending events
completed to empty the queue. Do not remove accounting ledgers or audit history
as part of transport cleanup.

Regular vacuum makes space from deleted tuples reusable. It may leave the
allocated file size unchanged. Reclaiming filesystem space with a table rewrite
requires separate downtime and disk-capacity planning; the worker does not do it.

Rollback retains the expanded operational schema: the identity table, reservation
trigger, cleanup function and indexes remain in place when EF rolls this migration
back. This lets earlier feature migrations and the application roll back without
reopening retired identities. Reapplying the migration reuses the retained schema
and data. Retired payloads are not reconstructed, and deduplication history must
never be dropped manually.
