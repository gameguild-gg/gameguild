using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AddBountyTerminalEventWriter
{
    private static void InstallBountyTerminalEventWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.read_bounty_terminal_by_bounty_v1(p_bounty_id uuid)
            RETURNS TABLE(
                "Id" uuid,
                "BountyId" uuid,
                "Status" integer,
                "ActorId" uuid,
                "DestinationWalletId" uuid,
                "IdempotencyKey" character varying,
                "RiskDecisionId" uuid,
                "ProceedsSourceStampId" uuid,
                "ProceedsLotId" uuid,
                "ReturnedUnits" bigint,
                "FeeUnits" bigint,
                "FirstJournalSequence" bigint,
                "OutputLots" jsonb,
                "OccurredAt" timestamptz)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT terminal."Id", terminal."BountyId", terminal."Status", terminal."ActorId",
                       terminal."DestinationWalletId", terminal."IdempotencyKey", terminal."RiskDecisionId",
                       terminal."ProceedsSourceStampId", terminal."ProceedsLotId", terminal."ReturnedUnits",
                       terminal."FeeUnits", terminal."FirstJournalSequence", terminal."OutputLots", terminal."OccurredAt"
                FROM public.economy_bounty_terminal_events terminal
                WHERE terminal."BountyId" = p_bounty_id
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_bounty_terminal_by_idempotency_v1(p_idempotency_key text)
            RETURNS TABLE(
                "Id" uuid,
                "BountyId" uuid,
                "Status" integer,
                "ActorId" uuid,
                "DestinationWalletId" uuid,
                "IdempotencyKey" character varying,
                "RiskDecisionId" uuid,
                "ProceedsSourceStampId" uuid,
                "ProceedsLotId" uuid,
                "ReturnedUnits" bigint,
                "FeeUnits" bigint,
                "FirstJournalSequence" bigint,
                "OutputLots" jsonb,
                "OccurredAt" timestamptz)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT terminal."Id", terminal."BountyId", terminal."Status", terminal."ActorId",
                       terminal."DestinationWalletId", terminal."IdempotencyKey", terminal."RiskDecisionId",
                       terminal."ProceedsSourceStampId", terminal."ProceedsLotId", terminal."ReturnedUnits",
                       terminal."FeeUnits", terminal."FirstJournalSequence", terminal."OutputLots", terminal."OccurredAt"
                FROM public.economy_bounty_terminal_events terminal
                WHERE terminal."IdempotencyKey" = p_idempotency_key
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.complete_bounty_terminal_v1(
                p_id uuid,
                p_bounty_id uuid,
                p_status integer,
                p_actor_id uuid,
                p_destination_wallet_id uuid,
                p_idempotency_key text,
                p_risk_decision_id uuid,
                p_proceeds_source_stamp_id uuid,
                p_proceeds_lot_id uuid,
                p_returned_units bigint,
                p_fee_units bigint,
                p_first_journal_sequence bigint,
                p_output_lots jsonb,
                p_occurred_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                bounty record;
                existing record;
            BEGIN
                IF p_id IS NULL OR p_bounty_id IS NULL OR p_actor_id IS NULL OR p_destination_wallet_id IS NULL
                   OR p_idempotency_key IS NULL OR length(btrim(p_idempotency_key)) = 0
                   OR p_status NOT IN (3, 4)
                   OR p_returned_units < 0 OR p_fee_units < 0 OR p_first_journal_sequence <= 0
                   OR jsonb_typeof(p_output_lots) <> 'array' THEN
                    RAISE EXCEPTION 'invalid bounty terminal arguments' USING ERRCODE = '22023';
                END IF;

                IF p_status = 3 AND (p_risk_decision_id IS NULL OR p_proceeds_source_stamp_id IS NULL OR p_proceeds_lot_id IS NULL) THEN
                    RAISE EXCEPTION 'bounty claim requires risk and proceeds bindings' USING ERRCODE = '23514';
                END IF;
                IF p_status = 4 AND (p_risk_decision_id IS NOT NULL OR p_proceeds_source_stamp_id IS NOT NULL OR p_proceeds_lot_id IS NOT NULL) THEN
                    RAISE EXCEPTION 'bounty reclaim cannot carry claim bindings' USING ERRCODE = '23514';
                END IF;

                SELECT * INTO bounty
                FROM public.economy_bounties
                WHERE "Id" = p_bounty_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'bounty escrow was not found' USING ERRCODE = 'P0002';
                END IF;

                SELECT * INTO existing
                FROM public.economy_bounty_terminal_events
                WHERE "BountyId" = p_bounty_id OR "IdempotencyKey" = p_idempotency_key
                FOR UPDATE;
                IF FOUND THEN
                    IF existing."BountyId" <> p_bounty_id
                       OR existing."IdempotencyKey" <> p_idempotency_key
                       OR existing."Id" <> p_id
                       OR existing."Status" <> p_status
                       OR existing."ActorId" <> p_actor_id
                       OR existing."DestinationWalletId" <> p_destination_wallet_id
                       OR existing."FirstJournalSequence" <> p_first_journal_sequence THEN
                        RAISE EXCEPTION 'bounty terminal command conflicts with immutable terminal outcome' USING ERRCODE = '23505';
                    END IF;
                    RETURN;
                END IF;

                IF bounty."Status" <> 1 THEN
                    RAISE EXCEPTION 'bounty already has a terminal outcome' USING ERRCODE = '23514';
                END IF;
                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_journal_entries entry
                    WHERE entry."Sequence" = p_first_journal_sequence) THEN
                    RAISE EXCEPTION 'bounty terminal event requires an accepted journal entry' USING ERRCODE = '23514';
                END IF;

                IF p_status = 3 THEN
                    IF p_actor_id = bounty."PosterId"
                       OR p_destination_wallet_id IN (bounty."PosterWalletId", bounty."EscrowWalletId") THEN
                        RAISE EXCEPTION 'a bounty poster cannot claim their own bounty' USING ERRCODE = '42501';
                    END IF;
                    IF p_returned_units <> 0 OR p_fee_units <> 0 THEN
                        RAISE EXCEPTION 'bounty claim cannot report reclaim amounts' USING ERRCODE = '23514';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1
                        FROM public.economy_risk_decisions decision
                        WHERE decision."Id" = p_risk_decision_id
                          AND decision."Outcome" = 1) THEN
                        RAISE EXCEPTION 'bounty claim risk decision is absent or denied' USING ERRCODE = '42501';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1
                        FROM public.economy_source_stamps source
                        JOIN public.economy_credit_lots lot ON lot."RootSourceStampId" = source."Id"
                        WHERE source."Id" = p_proceeds_source_stamp_id
                          AND lot."Id" = p_proceeds_lot_id
                          AND lot."WalletId" = p_destination_wallet_id) THEN
                        RAISE EXCEPTION 'bounty claim proceeds are not immutable or bound to the claimant wallet' USING ERRCODE = '23514';
                    END IF;
                ELSE
                    IF p_actor_id <> bounty."PosterId" OR p_destination_wallet_id <> bounty."PosterWalletId" THEN
                        RAISE EXCEPTION 'only the bounty poster can reclaim the escrow' USING ERRCODE = '42501';
                    END IF;
                    IF p_returned_units + p_fee_units <> bounty."AmountUnits" THEN
                        RAISE EXCEPTION 'bounty reclaim must account for the full escrow amount' USING ERRCODE = '23514';
                    END IF;
                END IF;

                UPDATE public.economy_bounties
                SET "Status" = p_status,
                    "Version" = "Version" + 1
                WHERE "Id" = p_bounty_id;

                INSERT INTO public.economy_bounty_terminal_events (
                    "Id", "BountyId", "Status", "ActorId", "DestinationWalletId", "IdempotencyKey",
                    "RiskDecisionId", "ProceedsSourceStampId", "ProceedsLotId", "ReturnedUnits", "FeeUnits",
                    "FirstJournalSequence", "OutputLots", "OccurredAt")
                VALUES (
                    p_id, p_bounty_id, p_status, p_actor_id, p_destination_wallet_id, btrim(p_idempotency_key),
                    p_risk_decision_id, p_proceeds_source_stamp_id, p_proceeds_lot_id, p_returned_units, p_fee_units,
                    p_first_journal_sequence, p_output_lots, p_occurred_at);
            END
            $function$;

            ALTER FUNCTION economy_private.read_bounty_terminal_by_bounty_v1(uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_bounty_terminal_by_idempotency_v1(text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.complete_bounty_terminal_v1(uuid,uuid,integer,uuid,uuid,text,uuid,uuid,uuid,bigint,bigint,bigint,jsonb,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE ALL ON FUNCTION economy_private.read_bounty_terminal_by_bounty_v1(uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_bounty_terminal_by_idempotency_v1(text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.complete_bounty_terminal_v1(uuid,uuid,integer,uuid,uuid,text,uuid,uuid,uuid,bigint,bigint,bigint,jsonb,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.read_bounty_terminal_by_bounty_v1(uuid),
                economy_private.read_bounty_terminal_by_idempotency_v1(text),
                economy_private.complete_bounty_terminal_v1(uuid,uuid,integer,uuid,uuid,text,uuid,uuid,uuid,bigint,bigint,bigint,jsonb,timestamptz)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveBountyTerminalEventWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.complete_bounty_terminal_v1(uuid,uuid,integer,uuid,uuid,text,uuid,uuid,uuid,bigint,bigint,bigint,jsonb,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.read_bounty_terminal_by_bounty_v1(uuid);
            DROP FUNCTION IF EXISTS economy_private.read_bounty_terminal_by_idempotency_v1(text);
            """);
    }
}
