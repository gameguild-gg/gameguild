using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class SecureBountyEscrowPersistence
{
    private static void InstallBountyEscrowPersistenceSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.create_bounty_escrow_v1(
                p_bounty_id uuid,
                p_poster_id uuid,
                p_poster_wallet_id uuid,
                p_escrow_wallet_id uuid,
                p_currency integer,
                p_amount_units bigint,
                p_reclaim_fee_ppm integer,
                p_requires_prerequisite boolean,
                p_minimum_reputation integer,
                p_requires_instructor_verification boolean,
                p_idempotency_key text,
                p_request_hash text,
                p_posted_at timestamptz,
                p_expires_at timestamptz,
                p_fragments jsonb)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                existing_bounty public.economy_bounties%ROWTYPE;
            BEGIN
                IF p_bounty_id IS NULL OR p_poster_id IS NULL OR p_poster_wallet_id IS NULL
                   OR p_escrow_wallet_id IS NULL OR p_poster_wallet_id = p_escrow_wallet_id
                   OR p_currency NOT IN (1, 2) OR p_amount_units <= 0
                   OR p_reclaim_fee_ppm < 0 OR p_reclaim_fee_ppm >= 1000000
                   OR p_minimum_reputation < 0 OR p_posted_at IS NULL OR p_expires_at <= p_posted_at
                   OR p_idempotency_key IS NULL OR btrim(p_idempotency_key) = '' OR length(p_idempotency_key) > 256
                   OR p_request_hash IS NULL OR btrim(p_request_hash) = '' OR length(p_request_hash) > 128
                   OR p_fragments IS NULL OR jsonb_typeof(p_fragments) <> 'array'
                   OR jsonb_array_length(p_fragments) = 0 THEN
                    RAISE EXCEPTION 'bounty escrow arguments are invalid' USING ERRCODE = '22023';
                END IF;

                SELECT * INTO existing_bounty
                FROM public.economy_bounties
                WHERE "IdempotencyKey" = btrim(p_idempotency_key)
                FOR UPDATE;

                IF FOUND THEN
                    IF existing_bounty."Id" = p_bounty_id
                       AND existing_bounty."RequestHash" = btrim(p_request_hash) THEN
                        RETURN;
                    END IF;

                    RAISE EXCEPTION 'bounty idempotency key is bound to another request' USING ERRCODE = '23505';
                END IF;

                IF EXISTS (SELECT 1 FROM public.economy_bounties WHERE "Id" = p_bounty_id) THEN
                    RAISE EXCEPTION 'bounty id already exists' USING ERRCODE = '23505';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_to_recordset(p_fragments) AS fragment(
                        "ParentLotId" uuid,
                        "Currency" integer,
                        "AmountUnits" bigint,
                        "TraceUnitsPerCoinUnit" bigint,
                        "SelectedRootRanges" jsonb)
                    WHERE "ParentLotId" IS NULL
                       OR "Currency" <> p_currency
                       OR "AmountUnits" <= 0
                       OR "TraceUnitsPerCoinUnit" <> CASE WHEN p_currency = 1 THEN 1000 ELSE 1 END
                       OR "SelectedRootRanges" IS NULL
                       OR jsonb_typeof("SelectedRootRanges") <> 'array'
                       OR jsonb_array_length("SelectedRootRanges") = 0) THEN
                    RAISE EXCEPTION 'bounty escrow fragment payload is invalid' USING ERRCODE = '22023';
                END IF;

                IF (SELECT COALESCE(sum(fragment."AmountUnits"), 0)
                    FROM jsonb_to_recordset(p_fragments) AS fragment(
                        "ParentLotId" uuid,
                        "Currency" integer,
                        "AmountUnits" bigint,
                        "TraceUnitsPerCoinUnit" bigint,
                        "SelectedRootRanges" jsonb)) <> p_amount_units THEN
                    RAISE EXCEPTION 'bounty escrow fragments must conserve the bounty amount' USING ERRCODE = '22023';
                END IF;

                IF (SELECT count(*) FROM jsonb_to_recordset(p_fragments) AS fragment("ParentLotId" uuid)) <>
                   (SELECT count(DISTINCT fragment."ParentLotId") FROM jsonb_to_recordset(p_fragments) AS fragment("ParentLotId" uuid)) THEN
                    RAISE EXCEPTION 'bounty escrow fragments must have distinct parent lots' USING ERRCODE = '22023';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_to_recordset(p_fragments) AS fragment(
                        "ParentLotId" uuid,
                        "AmountUnits" bigint,
                        "SelectedRootRanges" jsonb)
                    CROSS JOIN LATERAL jsonb_to_recordset(fragment."SelectedRootRanges") AS range(
                        "RootSourceStampId" uuid,
                        "StartInclusive" bigint,
                        "EndExclusive" bigint,
                        "ReversalEpoch" bigint)
                    WHERE range."RootSourceStampId" IS NULL
                       OR range."StartInclusive" < 0
                       OR range."EndExclusive" <= range."StartInclusive"
                       OR range."ReversalEpoch" < 0
                       OR NOT EXISTS (
                           SELECT 1
                           FROM public.economy_fragment_reservations reservation
                           WHERE reservation."OperationId" = p_bounty_id
                             AND reservation."Purpose" = 6
                             AND reservation."Status" = 1
                             AND reservation."WalletId" = p_poster_wallet_id
                             AND reservation."Currency" = p_currency
                             AND reservation."ParentLotId" = fragment."ParentLotId"
                             AND reservation."RootSourceStampId" = range."RootSourceStampId"
                             AND reservation."StartInclusive" = range."StartInclusive"
                             AND reservation."EndExclusive" = range."EndExclusive"
                             AND reservation."ReversalEpoch" = range."ReversalEpoch")) THEN
                    RAISE EXCEPTION 'bounty escrow fragments are not bound to FIFO reservations' USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_to_recordset(p_fragments) AS fragment(
                        "AmountUnits" bigint,
                        "TraceUnitsPerCoinUnit" bigint,
                        "SelectedRootRanges" jsonb)
                    CROSS JOIN LATERAL (
                        SELECT COALESCE(sum(range."EndExclusive" - range."StartInclusive"), 0) AS trace_units
                        FROM jsonb_to_recordset(fragment."SelectedRootRanges") AS range(
                            "RootSourceStampId" uuid,
                            "StartInclusive" bigint,
                            "EndExclusive" bigint,
                            "ReversalEpoch" bigint)) AS ranges
                    WHERE ranges.trace_units <> fragment."AmountUnits" * fragment."TraceUnitsPerCoinUnit") THEN
                    RAISE EXCEPTION 'bounty escrow root ranges must conserve each fragment amount' USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM public.economy_fragment_reservations reservation
                    WHERE reservation."OperationId" = p_bounty_id
                      AND reservation."Purpose" = 6
                      AND reservation."Status" = 1
                      AND NOT EXISTS (
                          SELECT 1
                          FROM jsonb_to_recordset(p_fragments) AS fragment(
                              "ParentLotId" uuid,
                              "SelectedRootRanges" jsonb)
                          CROSS JOIN LATERAL jsonb_to_recordset(fragment."SelectedRootRanges") AS range(
                              "RootSourceStampId" uuid,
                              "StartInclusive" bigint,
                              "EndExclusive" bigint,
                              "ReversalEpoch" bigint)
                          WHERE fragment."ParentLotId" = reservation."ParentLotId"
                            AND range."RootSourceStampId" = reservation."RootSourceStampId"
                            AND range."StartInclusive" = reservation."StartInclusive"
                            AND range."EndExclusive" = reservation."EndExclusive"
                            AND range."ReversalEpoch" = reservation."ReversalEpoch")) THEN
                    RAISE EXCEPTION 'every bounty FIFO reservation must be persisted' USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_bounties (
                    "Id", "PosterId", "PosterWalletId", "EscrowWalletId", "Currency", "AmountUnits",
                    "ReclaimFeePpm", "RequiresPrerequisite", "MinimumReputation", "RequiresInstructorVerification",
                    "Status", "IdempotencyKey", "RequestHash", "PostedAt", "ExpiresAt", "Version")
                VALUES (
                    p_bounty_id, p_poster_id, p_poster_wallet_id, p_escrow_wallet_id, p_currency, p_amount_units,
                    p_reclaim_fee_ppm, p_requires_prerequisite, p_minimum_reputation, p_requires_instructor_verification,
                    1, btrim(p_idempotency_key), btrim(p_request_hash), p_posted_at, p_expires_at, 1);

                INSERT INTO public.economy_bounty_escrow_fragments (
                    "Id", "BountyId", "ParentLotId", "Currency", "AmountUnits", "TraceUnitsPerCoinUnit", "SelectedRootRanges")
                SELECT gen_random_uuid(), p_bounty_id, fragment."ParentLotId", fragment."Currency", fragment."AmountUnits",
                       fragment."TraceUnitsPerCoinUnit", fragment."SelectedRootRanges"
                FROM jsonb_to_recordset(p_fragments) AS fragment(
                    "ParentLotId" uuid,
                    "Currency" integer,
                    "AmountUnits" bigint,
                    "TraceUnitsPerCoinUnit" bigint,
                    "SelectedRootRanges" jsonb);
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_bounty_escrow_by_id_v1(p_bounty_id uuid)
            RETURNS TABLE(
                "Id" uuid,
                "PosterId" uuid,
                "PosterWalletId" uuid,
                "EscrowWalletId" uuid,
                "Currency" integer,
                "AmountUnits" bigint,
                "ReclaimFeePpm" integer,
                "RequiresPrerequisite" boolean,
                "MinimumReputation" integer,
                "RequiresInstructorVerification" boolean,
                "Status" integer,
                "IdempotencyKey" text,
                "RequestHash" text,
                "PostedAt" timestamptz,
                "ExpiresAt" timestamptz,
                "Version" bigint)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT bounty."Id", bounty."PosterId", bounty."PosterWalletId", bounty."EscrowWalletId",
                       bounty."Currency", bounty."AmountUnits", bounty."ReclaimFeePpm", bounty."RequiresPrerequisite",
                       bounty."MinimumReputation", bounty."RequiresInstructorVerification", bounty."Status",
                       bounty."IdempotencyKey", bounty."RequestHash", bounty."PostedAt", bounty."ExpiresAt", bounty."Version"
                FROM public.economy_bounties bounty
                WHERE bounty."Id" = p_bounty_id
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_bounty_escrow_by_idempotency_v1(p_idempotency_key text)
            RETURNS TABLE(
                "Id" uuid,
                "PosterId" uuid,
                "PosterWalletId" uuid,
                "EscrowWalletId" uuid,
                "Currency" integer,
                "AmountUnits" bigint,
                "ReclaimFeePpm" integer,
                "RequiresPrerequisite" boolean,
                "MinimumReputation" integer,
                "RequiresInstructorVerification" boolean,
                "Status" integer,
                "IdempotencyKey" text,
                "RequestHash" text,
                "PostedAt" timestamptz,
                "ExpiresAt" timestamptz,
                "Version" bigint)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT bounty."Id", bounty."PosterId", bounty."PosterWalletId", bounty."EscrowWalletId",
                       bounty."Currency", bounty."AmountUnits", bounty."ReclaimFeePpm", bounty."RequiresPrerequisite",
                       bounty."MinimumReputation", bounty."RequiresInstructorVerification", bounty."Status",
                       bounty."IdempotencyKey", bounty."RequestHash", bounty."PostedAt", bounty."ExpiresAt", bounty."Version"
                FROM public.economy_bounties bounty
                WHERE bounty."IdempotencyKey" = btrim(p_idempotency_key)
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_bounty_escrow_fragments_v1(p_bounty_id uuid)
            RETURNS TABLE(
                "ParentLotId" uuid,
                "Currency" integer,
                "AmountUnits" bigint,
                "TraceUnitsPerCoinUnit" bigint,
                "SelectedRootRanges" jsonb)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT fragment."ParentLotId", fragment."Currency", fragment."AmountUnits",
                       fragment."TraceUnitsPerCoinUnit", fragment."SelectedRootRanges"
                FROM public.economy_bounty_escrow_fragments fragment
                WHERE fragment."BountyId" = p_bounty_id
                ORDER BY fragment."ParentLotId"
            $function$;

            ALTER FUNCTION economy_private.create_bounty_escrow_v1(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_bounty_escrow_by_id_v1(uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_bounty_escrow_by_idempotency_v1(text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_bounty_escrow_fragments_v1(uuid)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE ALL ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events FROM gameguild_economy_writer;
            REVOKE ALL ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events FROM gameguild_economy_runtime;
            GRANT SELECT ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events TO gameguild_economy_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events TO gameguild_economy_procedure_owner;
            GRANT ALL ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events TO gameguild_economy_migration;

            REVOKE ALL ON FUNCTION economy_private.create_bounty_escrow_v1(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_bounty_escrow_by_id_v1(uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_bounty_escrow_by_idempotency_v1(text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_bounty_escrow_fragments_v1(uuid) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.create_bounty_escrow_v1(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb),
                economy_private.read_bounty_escrow_by_id_v1(uuid),
                economy_private.read_bounty_escrow_by_idempotency_v1(text),
                economy_private.read_bounty_escrow_fragments_v1(uuid)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveBountyEscrowPersistenceSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.create_bounty_escrow_v1(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb);
            DROP FUNCTION IF EXISTS economy_private.read_bounty_escrow_by_id_v1(uuid);
            DROP FUNCTION IF EXISTS economy_private.read_bounty_escrow_by_idempotency_v1(text);
            DROP FUNCTION IF EXISTS economy_private.read_bounty_escrow_fragments_v1(uuid);

            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events TO gameguild_economy_writer;
            """);
    }
}
