using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

internal static class PayoutRequestPersistenceSql
{
    internal static void Install(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.create_payout_request_v1(
                p_id uuid, p_idempotency_key text, p_request_hash text, p_payee_id uuid,
                p_wallet_id uuid, p_amount_units bigint, p_state integer, p_version bigint,
                p_created_at timestamptz, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_id IS NULL OR p_payee_id IS NULL OR p_wallet_id IS NULL
                   OR p_amount_units <= 0 OR p_state <> 1 OR p_version <> 1
                   OR p_created_at IS NULL OR p_updated_at <> p_created_at
                   OR pg_catalog.length(pg_catalog.btrim(p_idempotency_key)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_request_hash)) <> 64 THEN
                    RAISE EXCEPTION 'payout request creation violates immutable request rules'
                        USING ERRCODE = '23514';
                END IF;

                PERFORM 1
                FROM public.economy_wallets
                WHERE "Id" = p_wallet_id AND "OwnerId" = p_payee_id AND "State" = 1;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout request wallet does not belong to the requesting payee'
                        USING ERRCODE = '42501';
                END IF;

                INSERT INTO public.economy_payout_requests (
                    "Id", "IdempotencyKey", "RequestHash", "PayeeId", "WalletId", "AmountUnits",
                    "State", "Version", "CreatedAt", "UpdatedAt")
                VALUES (
                    p_id, pg_catalog.btrim(p_idempotency_key), pg_catalog.btrim(p_request_hash),
                    p_payee_id, p_wallet_id, p_amount_units, p_state, p_version, p_created_at, p_updated_at);
            EXCEPTION WHEN unique_violation THEN
                RAISE EXCEPTION 'payout request overlaps an active wallet request or reuses an idempotency key'
                    USING ERRCODE = '23505';
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.transition_payout_request_v1(
                p_id uuid, p_payee_id uuid, p_expected_version bigint, p_state integer,
                p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                current public.economy_payout_requests%ROWTYPE;
            BEGIN
                SELECT * INTO current
                FROM public.economy_payout_requests
                WHERE "Id" = p_id AND "PayeeId" = p_payee_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout request was not found for the requesting payee'
                        USING ERRCODE = 'P0002';
                END IF;
                IF current."Version" <> p_expected_version OR p_updated_at < current."UpdatedAt" THEN
                    RAISE EXCEPTION 'payout request version is stale' USING ERRCODE = '40001';
                END IF;
                IF current."State" <> 1 OR p_state <> 2 THEN
                    RAISE EXCEPTION 'payout request transition is invalid' USING ERRCODE = '23514';
                END IF;

                UPDATE public.economy_payout_requests
                SET "State" = p_state,
                    "Version" = current."Version" + 1,
                    "UpdatedAt" = p_updated_at
                WHERE "Id" = p_id;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_request_by_idempotency_v1(
                p_payee_id uuid, p_idempotency_key text)
            RETURNS SETOF public.economy_payout_requests
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_requests
                WHERE "PayeeId" = p_payee_id
                  AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_request_by_id_for_payee_v1(p_id uuid, p_payee_id uuid)
            RETURNS SETOF public.economy_payout_requests
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_requests
                WHERE "Id" = p_id AND "PayeeId" = p_payee_id
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_requests_by_payee_v1(p_payee_id uuid, p_take integer)
            RETURNS SETOF public.economy_payout_requests
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_requests
                WHERE "PayeeId" = p_payee_id
                ORDER BY "CreatedAt" DESC, "Id" DESC
                LIMIT GREATEST(1, LEAST(p_take, 100))
            $function$;
            """);

        migrationBuilder.Sql(
            """
            ALTER FUNCTION economy_private.create_payout_request_v1(
                uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.transition_payout_request_v1(uuid,uuid,bigint,integer,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_request_by_idempotency_v1(uuid,text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_request_by_id_for_payee_v1(uuid,uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_requests_by_payee_v1(uuid,integer)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE ALL ON TABLE public.economy_payout_requests FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_payout_requests FROM gameguild_economy_writer;
            REVOKE ALL ON TABLE public.economy_payout_requests FROM gameguild_economy_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_payout_requests TO gameguild_economy_procedure_owner;
            GRANT ALL ON TABLE public.economy_payout_requests TO gameguild_economy_migration;

            REVOKE ALL ON FUNCTION economy_private.create_payout_request_v1(
                uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.transition_payout_request_v1(uuid,uuid,bigint,integer,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_request_by_idempotency_v1(uuid,text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_request_by_id_for_payee_v1(uuid,uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_requests_by_payee_v1(uuid,integer) FROM PUBLIC;

            GRANT EXECUTE ON FUNCTION economy_private.create_payout_request_v1(
                uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz),
                economy_private.transition_payout_request_v1(uuid,uuid,bigint,integer,timestamptz),
                economy_private.read_payout_request_by_idempotency_v1(uuid,text),
                economy_private.read_payout_request_by_id_for_payee_v1(uuid,uuid),
                economy_private.read_payout_requests_by_payee_v1(uuid,integer)
                TO gameguild_economy_writer;
            """);
    }

    internal static void Remove(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.create_payout_request_v1(
                uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.transition_payout_request_v1(uuid,uuid,bigint,integer,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.read_payout_request_by_idempotency_v1(uuid,text);
            DROP FUNCTION IF EXISTS economy_private.read_payout_request_by_id_for_payee_v1(uuid,uuid);
            DROP FUNCTION IF EXISTS economy_private.read_payout_requests_by_payee_v1(uuid,integer);
            """);
    }
}
