using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddTenantScopedPayoutRuntime
{
    private static void BackfillPayoutOperationTenantScope(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.economy_payout_operations AS operation
            SET "TenantId" = wallet."TenantId"
            FROM public.economy_wallets AS wallet
            WHERE wallet."Id" = operation."WalletId"
              AND operation."TenantId" IS NULL;

            DO $block$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM public.economy_payout_operations AS operation
                    LEFT JOIN public.economy_wallets AS wallet ON wallet."Id" = operation."WalletId"
                    WHERE operation."TenantId" IS NULL
                       OR wallet."Id" IS NULL
                       OR wallet."TenantId" IS DISTINCT FROM operation."TenantId"
                       OR wallet."OwnerId" IS DISTINCT FROM operation."PayeeId") THEN
                    RAISE EXCEPTION 'payout operation tenant backfill is incomplete or inconsistent'
                        USING ERRCODE = '23514';
                END IF;
            END
            $block$;
            """);
    }

    private static void InstallTenantScopedPayoutRuntime(MigrationBuilder migrationBuilder)
    {
        CreatePayoutFencingTokenAllocator(migrationBuilder);
        CreateTenantScopedPayoutOperationFunctions(migrationBuilder);
        CreateTenantScopedPayoutRequestFunctions(migrationBuilder);
        RegisterPayoutPostingCapabilities(migrationBuilder);
        RestrictLegacyPayoutFunctions(migrationBuilder);
    }

    private static void CreatePayoutFencingTokenAllocator(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SEQUENCE IF NOT EXISTS economy_private.payout_fencing_sequence
                AS bigint START WITH 1 INCREMENT BY 1 NO CYCLE;
            ALTER SEQUENCE economy_private.payout_fencing_sequence
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON SEQUENCE economy_private.payout_fencing_sequence FROM PUBLIC;

            CREATE OR REPLACE FUNCTION economy_private.next_payout_fencing_token_v1()
            RETURNS bigint
            LANGUAGE sql
            VOLATILE
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT pg_catalog.nextval('economy_private.payout_fencing_sequence');
            $function$;
            ALTER FUNCTION economy_private.next_payout_fencing_token_v1()
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.next_payout_fencing_token_v1() FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.next_payout_fencing_token_v1()
                TO gameguild_economy_writer;
            """);
    }

    private static void CreateTenantScopedPayoutOperationFunctions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.create_payout_operation_v2(
                p_id uuid, p_tenant_id uuid, p_idempotency_key text, p_request_hash text,
                p_actor_id uuid, p_payee_id uuid, p_wallet_id uuid, p_amount_units bigint,
                p_provider_account_id text, p_destination_hash text, p_provider_binding_hash text,
                p_eligibility_hash text, p_dispatch_snapshot_hash text, p_provider_payout_id text,
                p_state integer, p_version bigint, p_fencing_token bigint, p_kill_switch_epoch bigint,
                p_reserve_version bigint, p_reserve_authorization_epoch bigint, p_policy_version bigint,
                p_risk_decision_id uuid, p_created_at timestamptz, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_id IS NULL OR p_tenant_id IS NULL OR p_actor_id IS NULL OR p_payee_id IS NULL
                   OR p_wallet_id IS NULL OR p_risk_decision_id IS NULL OR p_amount_units <= 0
                   OR p_state <> 1 OR p_version <> 1 OR p_fencing_token <= 0
                   OR p_kill_switch_epoch < 0 OR p_reserve_version <= 0
                   OR p_reserve_authorization_epoch <= 0 OR p_policy_version <= 0
                   OR p_created_at IS NULL OR p_updated_at IS NULL OR p_updated_at < p_created_at
                   OR pg_catalog.length(pg_catalog.btrim(p_idempotency_key)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_request_hash)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_provider_account_id)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_destination_hash)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_provider_binding_hash)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_eligibility_hash)) = 0
                   OR p_dispatch_snapshot_hash IS NOT NULL OR p_provider_payout_id IS NOT NULL THEN
                    RAISE EXCEPTION 'payout operation is invalid' USING ERRCODE = '23514';
                END IF;

                PERFORM 1
                FROM public.economy_wallets
                WHERE "Id" = p_wallet_id
                  AND "TenantId" = p_tenant_id
                  AND "OwnerId" = p_payee_id
                  AND "State" = 1;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout operation wallet does not belong to the tenant payee'
                        USING ERRCODE = '42501';
                END IF;

                INSERT INTO public.economy_payout_operations (
                    "Id", "TenantId", "IdempotencyKey", "RequestHash", "ActorId", "PayeeId",
                    "WalletId", "AmountUnits", "ProviderAccountId", "DestinationHash",
                    "ProviderBindingHash", "EligibilityHash", "DispatchSnapshotHash",
                    "ProviderPayoutId", "State", "Version", "FencingToken", "KillSwitchEpoch",
                    "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion", "RiskDecisionId",
                    "CreatedAt", "UpdatedAt")
                VALUES (
                    p_id, p_tenant_id, pg_catalog.btrim(p_idempotency_key),
                    pg_catalog.btrim(p_request_hash), p_actor_id, p_payee_id, p_wallet_id,
                    p_amount_units, pg_catalog.btrim(p_provider_account_id),
                    pg_catalog.btrim(p_destination_hash), pg_catalog.btrim(p_provider_binding_hash),
                    pg_catalog.btrim(p_eligibility_hash), NULL, NULL, p_state, p_version,
                    p_fencing_token, p_kill_switch_epoch, p_reserve_version,
                    p_reserve_authorization_epoch, p_policy_version, p_risk_decision_id,
                    p_created_at, p_updated_at);
            EXCEPTION WHEN unique_violation THEN
                RAISE EXCEPTION 'payout idempotency key was already used for this tenant'
                    USING ERRCODE = '23505';
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_operation_for_tenant_v2(
                p_tenant_id uuid, p_id uuid)
            RETURNS SETOF public.economy_payout_operations
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_operations
                WHERE "TenantId" = p_tenant_id AND "Id" = p_id
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_operations_for_tenant_v2(
                p_tenant_id uuid, p_take integer)
            RETURNS SETOF public.economy_payout_operations
            LANGUAGE plpgsql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_tenant_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                    RAISE EXCEPTION 'payout tenant query is invalid' USING ERRCODE = '22023';
                END IF;
                RETURN QUERY
                SELECT * FROM public.economy_payout_operations
                WHERE "TenantId" = p_tenant_id
                ORDER BY "CreatedAt" DESC, "Id" DESC
                LIMIT p_take;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_operations_by_payee_v2(
                p_tenant_id uuid, p_payee_id uuid, p_take integer)
            RETURNS SETOF public.economy_payout_operations
            LANGUAGE plpgsql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_tenant_id IS NULL OR p_payee_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                    RAISE EXCEPTION 'payout payee query is invalid' USING ERRCODE = '22023';
                END IF;
                RETURN QUERY
                SELECT * FROM public.economy_payout_operations
                WHERE "TenantId" = p_tenant_id AND "PayeeId" = p_payee_id
                ORDER BY "CreatedAt" DESC, "Id" DESC
                LIMIT p_take;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_operation_by_idempotency_v2(
                p_tenant_id uuid, p_idempotency_key text)
            RETURNS SETOF public.economy_payout_operations
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_operations
                WHERE "TenantId" = p_tenant_id
                  AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
            $function$;
            """);
    }

    private static void CreateTenantScopedPayoutRequestFunctions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.create_payout_request_v3(
                p_id uuid, p_tenant_id uuid, p_idempotency_key text, p_request_hash text,
                p_payee_id uuid, p_wallet_id uuid, p_amount_units bigint, p_state integer,
                p_version bigint, p_created_at timestamptz, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_id IS NULL OR p_tenant_id IS NULL OR p_payee_id IS NULL OR p_wallet_id IS NULL
                   OR p_amount_units <= 0 OR p_state <> 1 OR p_version <> 1
                   OR p_created_at IS NULL OR p_updated_at <> p_created_at
                   OR pg_catalog.length(pg_catalog.btrim(p_idempotency_key)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_request_hash)) <> 64 THEN
                    RAISE EXCEPTION 'payout request creation violates immutable request rules'
                        USING ERRCODE = '23514';
                END IF;

                PERFORM 1
                FROM public.economy_wallets
                WHERE "Id" = p_wallet_id
                  AND "TenantId" = p_tenant_id
                  AND "OwnerId" = p_payee_id
                  AND "State" = 1;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout request wallet does not belong to the tenant payee'
                        USING ERRCODE = '42501';
                END IF;

                INSERT INTO public.economy_payout_requests (
                    "Id", "TenantId", "IdempotencyKey", "RequestHash", "PayeeId", "WalletId",
                    "AmountUnits", "State", "Version", "CreatedAt", "UpdatedAt",
                    "FirstApprovalActorId")
                VALUES (
                    p_id, p_tenant_id, pg_catalog.btrim(p_idempotency_key),
                    pg_catalog.btrim(p_request_hash), p_payee_id, p_wallet_id, p_amount_units,
                    p_state, p_version, p_created_at, p_updated_at, NULL);
            EXCEPTION WHEN unique_violation THEN
                RAISE EXCEPTION 'payout request overlaps an active wallet request or reuses an idempotency key'
                    USING ERRCODE = '23505';
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.transition_payout_request_v3(
                p_tenant_id uuid, p_id uuid, p_payee_id uuid, p_expected_version bigint,
                p_state integer, p_updated_at timestamptz)
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
                WHERE "TenantId" = p_tenant_id AND "Id" = p_id AND "PayeeId" = p_payee_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout request was not found for the tenant payee'
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
                WHERE "TenantId" = p_tenant_id AND "Id" = p_id;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_request_by_idempotency_v3(
                p_tenant_id uuid, p_payee_id uuid, p_idempotency_key text)
            RETURNS SETOF public.economy_payout_requests
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_requests
                WHERE "TenantId" = p_tenant_id AND "PayeeId" = p_payee_id
                  AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_request_by_id_for_payee_v3(
                p_tenant_id uuid, p_id uuid, p_payee_id uuid)
            RETURNS SETOF public.economy_payout_requests
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_payout_requests
                WHERE "TenantId" = p_tenant_id AND "Id" = p_id AND "PayeeId" = p_payee_id
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_payout_requests_by_payee_v3(
                p_tenant_id uuid, p_payee_id uuid, p_take integer)
            RETURNS SETOF public.economy_payout_requests
            LANGUAGE plpgsql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_tenant_id IS NULL OR p_payee_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                    RAISE EXCEPTION 'payout request payee query is invalid' USING ERRCODE = '22023';
                END IF;
                RETURN QUERY
                SELECT * FROM public.economy_payout_requests
                WHERE "TenantId" = p_tenant_id AND "PayeeId" = p_payee_id
                ORDER BY "CreatedAt" DESC, "Id" DESC
                LIMIT p_take;
            END
            $function$;
            """);
    }

    private static void RegisterPayoutPostingCapabilities(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO public.economy_registered_capabilities (
                "Id", "Name", "AllowedTemplateKinds", "IsEnabled", "CreatedAt", "RevokedAt")
            VALUES
                ('e1000000-0000-0000-0000-000000000009', 'payout-reservation', '[11]'::jsonb,
                    true, TIMESTAMPTZ '2026-08-26T00:00:00Z', NULL),
                ('e1000000-0000-0000-0000-000000000010', 'payout-provider-terminal', '[12,13]'::jsonb,
                    true, TIMESTAMPTZ '2026-08-26T00:00:00Z', NULL)
            ON CONFLICT ("Name") DO NOTHING;
            """);
    }

    private static void RestrictLegacyPayoutFunctions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER FUNCTION economy_private.create_payout_operation_v2(
                uuid,uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,
                bigint,bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid,uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid,integer)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid,uuid,integer)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid,text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.create_payout_request_v3(
                uuid,uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.transition_payout_request_v3(
                uuid,uuid,uuid,bigint,integer,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_request_by_idempotency_v3(uuid,uuid,text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_request_by_id_for_payee_v3(uuid,uuid,uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_payout_requests_by_payee_v3(uuid,uuid,integer)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE ALL ON FUNCTION economy_private.create_payout_operation_v1(
                uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,bigint,
                bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_idempotency_v1(text)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid,integer)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.create_payout_request_v2(
                uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.transition_payout_request_v1(
                uuid,uuid,bigint,integer,timestamptz)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.read_payout_request_by_idempotency_v1(uuid,text)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.read_payout_request_by_id_for_payee_v1(uuid,uuid)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.read_payout_requests_by_payee_v1(uuid,integer)
                FROM gameguild_economy_writer;

            REVOKE ALL ON FUNCTION economy_private.create_payout_operation_v2(
                uuid,uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,
                bigint,bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid,uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid,integer) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid,uuid,integer) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid,text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.create_payout_request_v3(
                uuid,uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.transition_payout_request_v3(
                uuid,uuid,uuid,bigint,integer,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_request_by_idempotency_v3(uuid,uuid,text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_request_by_id_for_payee_v3(uuid,uuid,uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_payout_requests_by_payee_v3(uuid,uuid,integer) FROM PUBLIC;

            GRANT EXECUTE ON FUNCTION economy_private.create_payout_operation_v2(
                uuid,uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,
                bigint,bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz),
                economy_private.read_payout_operation_for_tenant_v2(uuid,uuid),
                economy_private.read_payout_operations_for_tenant_v2(uuid,integer),
                economy_private.read_payout_operations_by_payee_v2(uuid,uuid,integer),
                economy_private.read_payout_operation_by_idempotency_v2(uuid,text),
                economy_private.create_payout_request_v3(
                    uuid,uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz),
                economy_private.transition_payout_request_v3(uuid,uuid,uuid,bigint,integer,timestamptz),
                economy_private.read_payout_request_by_idempotency_v3(uuid,uuid,text),
                economy_private.read_payout_request_by_id_for_payee_v3(uuid,uuid,uuid),
                economy_private.read_payout_requests_by_payee_v3(uuid,uuid,integer)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveTenantScopedPayoutRuntime(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM public.economy_registered_capabilities
            WHERE "Id" IN (
                'e1000000-0000-0000-0000-000000000009'::uuid,
                'e1000000-0000-0000-0000-000000000010'::uuid);

            DROP FUNCTION IF EXISTS economy_private.create_payout_operation_v2(
                uuid,uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,
                bigint,bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.read_payout_operation_for_tenant_v2(uuid,uuid);
            DROP FUNCTION IF EXISTS economy_private.read_payout_operations_for_tenant_v2(uuid,integer);
            DROP FUNCTION IF EXISTS economy_private.read_payout_operations_by_payee_v2(uuid,uuid,integer);
            DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_idempotency_v2(uuid,text);
            DROP FUNCTION IF EXISTS economy_private.create_payout_request_v3(
                uuid,uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.transition_payout_request_v3(
                uuid,uuid,uuid,bigint,integer,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.read_payout_request_by_idempotency_v3(uuid,uuid,text);
            DROP FUNCTION IF EXISTS economy_private.read_payout_request_by_id_for_payee_v3(uuid,uuid,uuid);
            DROP FUNCTION IF EXISTS economy_private.read_payout_requests_by_payee_v3(uuid,uuid,integer);
            DROP FUNCTION IF EXISTS economy_private.next_payout_fencing_token_v1();
            DROP SEQUENCE IF EXISTS economy_private.payout_fencing_sequence;

            GRANT EXECUTE ON FUNCTION economy_private.create_payout_operation_v1(
                uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,bigint,
                bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz),
                economy_private.read_payout_operation_by_idempotency_v1(text),
                economy_private.read_payout_operations_by_payee_v1(uuid,integer),
                economy_private.create_payout_request_v2(
                    uuid,text,text,uuid,uuid,bigint,integer,bigint,timestamptz,timestamptz),
                economy_private.transition_payout_request_v1(uuid,uuid,bigint,integer,timestamptz),
                economy_private.read_payout_request_by_idempotency_v1(uuid,text),
                economy_private.read_payout_request_by_id_for_payee_v1(uuid,uuid),
                economy_private.read_payout_requests_by_payee_v1(uuid,integer)
                TO gameguild_economy_writer;
            """);
    }
}
