using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025170000_RetireCompletedOutboxMessages")]
public sealed class RetireCompletedOutboxMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "gameguild.integration".outbox_delivery_receipts (
                "EventId" uuid PRIMARY KEY,
                "CompletedAtUtc" timestamptz NULL,
                "EventName" varchar(200) NULL,
                "EventType" varchar(1000) NULL,
                "SourceModule" varchar(200) NULL,
                "AggregateType" varchar(200) NULL,
                "AggregateId" varchar(200) NULL,
                "TenantId" uuid NULL,
                "CorrelationId" uuid NULL,
                "OccurredAtUtc" timestamptz NULL,
                "SchemaVersion" integer NULL
            );
            CREATE INDEX IF NOT EXISTS ix_outbox_delivery_source
                ON "gameguild.integration".outbox_delivery_receipts ("SourceModule", "OccurredAtUtc")
                WHERE "SourceModule" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_outbox_delivery_correlation
                ON "gameguild.integration".outbox_delivery_receipts ("CorrelationId")
                WHERE "CompletedAtUtc" IS NOT NULL AND "EventType" IS NOT NULL;

            CREATE INDEX IF NOT EXISTS ix_outbox_pending_order
                ON "gameguild.integration".outbox_messages ("OccurredAtUtc", "EventId")
                WHERE "CompletedAtUtc" IS NULL AND "DeadLetteredAtUtc" IS NULL;

            CREATE INDEX IF NOT EXISTS ix_inbox_unfinished_event
                ON "gameguild.integration".inbox_receipts ("EventId")
                WHERE "CompletedAtUtc" IS NULL OR "DeadLetteredAtUtc" IS NOT NULL;

            -- Reserve identity using a unique write, not a snapshot-only existence check.
            -- This also protects raw SQL publishers and transactions at repeatable read.
            CREATE OR REPLACE FUNCTION "gameguild.integration".reserve_outbox_event_identity()
            RETURNS trigger LANGUAGE plpgsql VOLATILE
            SET search_path = pg_catalog
            AS $function$
            BEGIN
                INSERT INTO "gameguild.integration".outbox_delivery_receipts ("EventId")
                VALUES (NEW."EventId") ON CONFLICT ("EventId") DO NOTHING;
                IF NOT FOUND THEN
                    RAISE EXCEPTION USING ERRCODE = '23505',
                        MESSAGE = 'Integration event identity has already been recorded.',
                        CONSTRAINT = 'outbox_delivery_receipts_pkey';
                END IF;
                RETURN NEW;
            END
            $function$;
            REVOKE ALL ON FUNCTION "gameguild.integration".reserve_outbox_event_identity() FROM PUBLIC;
            DROP TRIGGER IF EXISTS trg_reserve_outbox_event_identity
                ON "gameguild.integration".outbox_messages;
            CREATE TRIGGER trg_reserve_outbox_event_identity
                AFTER INSERT ON "gameguild.integration".outbox_messages
                FOR EACH ROW EXECUTE FUNCTION "gameguild.integration".reserve_outbox_event_identity();

            CREATE OR REPLACE FUNCTION "gameguild.integration".retire_completed_outbox_messages(
                p_batch_size integer, p_now timestamptz, p_source_module text)
            RETURNS integer LANGUAGE plpgsql VOLATILE
            SET search_path = pg_catalog
            AS $function$
            DECLARE
                retired_receipts "gameguild.integration".outbox_delivery_receipts[];
                parent_locations tid[];
                event_ids uuid[];
                removed integer;
            BEGIN
                IF p_batch_size IS NULL OR p_batch_size < 1 OR p_batch_size > 1000 OR p_now IS NULL THEN
                    RAISE EXCEPTION 'A cleanup batch must contain between 1 and 1000 events and a cutoff.';
                END IF;

                -- Carry the compact envelope from the locked rows. Re-reading 1000
                -- random UUIDs through the large outbox PK causes unnecessary disk I/O.
                SELECT array_agg(ROW(candidate."EventId", candidate."CompletedAtUtc",
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "EventName" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "EventType" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "SourceModule" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "AggregateType" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "AggregateId" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "TenantId" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "CorrelationId" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "OccurredAtUtc" END,
                    CASE WHEN "SourceModule" <> 'Platform.Http' THEN "SchemaVersion" END
                )::"gameguild.integration".outbox_delivery_receipts), array_agg(candidate."RowLocation")
                INTO retired_receipts, parent_locations
                FROM (
                    SELECT message.ctid AS "RowLocation", message."EventId", message."CompletedAtUtc", message."EventName", message."EventType",
                        message."SourceModule", message."AggregateType", message."AggregateId", message."TenantId",
                        message."CorrelationId", message."OccurredAtUtc", message."SchemaVersion"
                    FROM "gameguild.integration".outbox_messages message
                    WHERE message."CompletedAtUtc" IS NOT NULL
                      AND message."CompletedAtUtc" <= p_now
                      AND message."DeadLetteredAtUtc" IS NULL
                      AND (message."ClaimedUntilUtc" IS NULL OR message."ClaimedUntilUtc" <= p_now)
                      AND (p_source_module IS NULL OR message."SourceModule" = p_source_module)
                      AND NOT EXISTS (
                          SELECT 1 FROM "gameguild.integration".inbox_receipts receipt
                          WHERE receipt."EventId" = message."EventId"
                            AND (receipt."CompletedAtUtc" IS NULL OR receipt."DeadLetteredAtUtc" IS NOT NULL))
                    ORDER BY message."CompletedAtUtc"
                    LIMIT p_batch_size FOR UPDATE OF message SKIP LOCKED
                ) candidate;

                IF retired_receipts IS NULL THEN RETURN 0; END IF;
                event_ids := ARRAY(SELECT receipt."EventId" FROM unnest(retired_receipts) receipt);

                INSERT INTO "gameguild.integration".outbox_delivery_receipts (
                    "EventId", "CompletedAtUtc", "EventName", "EventType", "SourceModule",
                    "AggregateType", "AggregateId", "TenantId", "CorrelationId", "OccurredAtUtc", "SchemaVersion")
                SELECT * FROM unnest(retired_receipts)
                ON CONFLICT ("EventId") DO UPDATE SET
                    "CompletedAtUtc" = EXCLUDED."CompletedAtUtc",
                    "EventName" = EXCLUDED."EventName", "EventType" = EXCLUDED."EventType",
                    "SourceModule" = EXCLUDED."SourceModule", "AggregateType" = EXCLUDED."AggregateType",
                    "AggregateId" = EXCLUDED."AggregateId", "TenantId" = EXCLUDED."TenantId",
                    "CorrelationId" = EXCLUDED."CorrelationId", "OccurredAtUtc" = EXCLUDED."OccurredAtUtc",
                    "SchemaVersion" = EXCLUDED."SchemaVersion";

                DELETE FROM "gameguild.integration".inbox_receipts WHERE "EventId" = ANY(event_ids);
                -- The parent row locks keep these physical locations stable until deletion.
                DELETE FROM "gameguild.integration".outbox_messages WHERE ctid = ANY(parent_locations);
                GET DIAGNOSTICS removed = ROW_COUNT;
                RETURN removed;
            END
            $function$;
            REVOKE ALL ON FUNCTION
                "gameguild.integration".retire_completed_outbox_messages(integer, timestamptz, text) FROM PUBLIC;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This operational schema is an additive, permanent expansion. An application
        // or earlier feature migration may roll back, but identity reservation must
        // keep protecting retired events throughout that window. Up is idempotent
        // so a later upgrade reuses the retained receipts, trigger and function.
    }
}
